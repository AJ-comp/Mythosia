# DRY Principle

Eliminate duplicated logic by extracting shared behavior into reusable functions, base classes, or utilities. Every piece of knowledge should have a single, authoritative representation.

## Core Principles

1. **Single Source of Truth** — Define logic once, reference it everywhere
2. **Change in One Place** — When requirements change, you should only need to edit one location
3. **Extract, Don't Copy** — If you copy-paste code, stop and extract it instead
4. **DRY ≠ No Repetition** — Similar-looking code with different *reasons to change* is not duplication

## Rules

- If the same logic appears in 3+ places, extract it immediately (Rule of Three)
- If the same logic appears in 2 places and is non-trivial, consider extraction
- Prefer composition over inheritance for sharing behavior
- Don't DRY things that are only *coincidentally* similar — they may diverge later
- Shared logic should live at the appropriate level: utility, service, or base class
- Duplicated string templates, SQL fragments, and validation rules are all forms of duplication

## Examples

### Example 1: Copy-Pasted Validation

#### Before (same validation in multiple methods)

```csharp
public class OrderService
{
    public async Task<Result> CreateOrder(OrderRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerEmail)) return Result.Failure("Email required");
        if (!request.CustomerEmail.Contains("@")) return Result.Failure("Invalid email");
        if (request.Items == null || request.Items.Count == 0) return Result.Failure("No items");
        if (request.Items.Any(i => i.Quantity <= 0)) return Result.Failure("Invalid quantity");

        // ... create logic
    }

    public async Task<Result> UpdateOrder(string orderId, OrderRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerEmail)) return Result.Failure("Email required");
        if (!request.CustomerEmail.Contains("@")) return Result.Failure("Invalid email");
        if (request.Items == null || request.Items.Count == 0) return Result.Failure("No items");
        if (request.Items.Any(i => i.Quantity <= 0)) return Result.Failure("Invalid quantity");

        // ... update logic
    }

    public async Task<Result> CloneOrder(string sourceOrderId, OrderRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerEmail)) return Result.Failure("Email required");
        if (!request.CustomerEmail.Contains("@")) return Result.Failure("Invalid email");
        if (request.Items == null || request.Items.Count == 0) return Result.Failure("No items");
        if (request.Items.Any(i => i.Quantity <= 0)) return Result.Failure("Invalid quantity");

        // ... clone logic
    }
}
```

#### After (shared validation method)

```csharp
public class OrderService
{
    public async Task<Result> CreateOrder(OrderRequest request)
    {
        var validation = ValidateOrderRequest(request);
        if (!validation.IsSuccess) return validation;

        // ... create logic
    }

    public async Task<Result> UpdateOrder(string orderId, OrderRequest request)
    {
        var validation = ValidateOrderRequest(request);
        if (!validation.IsSuccess) return validation;

        // ... update logic
    }

    public async Task<Result> CloneOrder(string sourceOrderId, OrderRequest request)
    {
        var validation = ValidateOrderRequest(request);
        if (!validation.IsSuccess) return validation;

        // ... clone logic
    }

    private Result ValidateOrderRequest(OrderRequest request)
    {
        if (string.IsNullOrEmpty(request.CustomerEmail)) return Result.Failure("Email required");
        if (!request.CustomerEmail.Contains("@")) return Result.Failure("Invalid email");
        if (request.Items == null || request.Items.Count == 0) return Result.Failure("No items");
        if (request.Items.Any(i => i.Quantity <= 0)) return Result.Failure("Invalid quantity");

        return Result.Success();
    }
}
```

### Example 2: Duplicated Query Patterns

#### Before (repeated query + mapping)

```csharp
public async Task<UserDto> GetUserById(int id)
{
    var user = await _db.Users
        .Where(u => u.Id == id && u.IsActive)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.Name,
            JoinedAt = u.CreatedAt
        })
        .FirstOrDefaultAsync();
    return user;
}

public async Task<List<UserDto>> GetUsersByRole(string role)
{
    var users = await _db.Users
        .Where(u => u.Role.Name == role && u.IsActive)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.Name,
            JoinedAt = u.CreatedAt
        })
        .ToListAsync();
    return users;
}

public async Task<List<UserDto>> SearchUsers(string keyword)
{
    var users = await _db.Users
        .Where(u => u.Name.Contains(keyword) && u.IsActive)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.Name,
            JoinedAt = u.CreatedAt
        })
        .ToListAsync();
    return users;
}
```

#### After (shared base query + projection)

```csharp
private IQueryable<UserDto> ActiveUsersQuery()
{
    return _db.Users
        .Where(u => u.IsActive)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.Name,
            JoinedAt = u.CreatedAt
        });
}

public async Task<UserDto> GetUserById(int id)
{
    return await ActiveUsersQuery()
        .FirstOrDefaultAsync(u => u.Id == id);
}

public async Task<List<UserDto>> GetUsersByRole(string role)
{
    return await ActiveUsersQuery()
        .Where(u => u.Role == role)
        .ToListAsync();
}

public async Task<List<UserDto>> SearchUsers(string keyword)
{
    return await ActiveUsersQuery()
        .Where(u => u.Name.Contains(keyword))
        .ToListAsync();
}
```

### Example 3: Duplicated Response Formatting

#### Before (formatting logic repeated in every controller)

```csharp
public class OrderController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _service.GetOrderAsync(id);
        if (order == null)
        {
            return NotFound(new { success = false, message = "Not found", data = (object)null });
        }
        return Ok(new { success = true, message = "OK", data = order });
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(OrderRequest request)
    {
        var result = await _service.CreateOrderAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, message = result.Error, data = (object)null });
        }
        return Ok(new { success = true, message = "Created", data = result.Value });
    }
}
```

#### After (shared response helper)

```csharp
public static class ApiResponse
{
    public static object Success(object data, string message = "OK")
        => new { success = true, message, data };

    public static object Fail(string message)
        => new { success = false, message, data = (object)null };
}

public class OrderController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _service.GetOrderAsync(id);
        if (order == null) return NotFound(ApiResponse.Fail("Not found"));

        return Ok(ApiResponse.Success(order));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(OrderRequest request)
    {
        var result = await _service.CreateOrderAsync(request);
        if (!result.IsSuccess) return BadRequest(ApiResponse.Fail(result.Error));

        return Ok(ApiResponse.Success(result.Value, "Created"));
    }
}
```

## When NOT to Apply

- **Coincidental similarity** — Two functions look alike but serve different domains and will evolve independently
- **Test code** — Duplicated setup in tests is often more readable than shared fixtures
- **Premature abstraction** — Don't extract until you see actual duplication (Rule of Three)
- **Coupling risk** — Sharing code across bounded contexts can create unwanted coupling
