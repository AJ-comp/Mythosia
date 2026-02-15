# Boolean & Flag Parameters

Replace boolean parameters with separate methods, enums, or option objects to make call sites readable and intentions clear.

## Core Principles

1. **Call Site Readability** — `Render(true, false)` tells the reader nothing; `Render(Page.Full, Cache.Bypass)` does
2. **One Function, One Behavior** — A boolean flag often means the function does two different things
3. **Enums Over Bools** — Named values are self-documenting at both definition and call site
4. **Option Objects for Many Flags** — When 3+ booleans creep in, bundle them into a configuration object

## Rules

- If a boolean parameter changes the function's core behavior, split into two separate functions
- If a boolean parameter tweaks a detail, replace it with an enum
- Never pass a raw `true`/`false` literal to a function — at minimum, use a named constant or comment
- Avoid functions with 2+ boolean parameters — the call site becomes unreadable
- Use `[Flags] enum` when options can be combined

## Examples

### Example 1: Boolean That Splits Core Behavior

#### Before (one flag, two different functions)

```csharp
public async Task<List<UserDto>> GetUsers(bool includeInactive)
{
    if (includeInactive)
    {
        return await _db.Users
            .Select(u => new UserDto { Id = u.Id, Name = u.Name, IsActive = u.IsActive })
            .ToListAsync();
    }
    else
    {
        return await _db.Users
            .Where(u => u.IsActive)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name, IsActive = u.IsActive })
            .ToListAsync();
    }
}

// Call site — what does 'true' mean?
var users = await GetUsers(true);
```

#### After (two descriptive methods)

```csharp
public async Task<List<UserDto>> GetActiveUsers()
{
    return await QueryUsers(activeOnly: true);
}

public async Task<List<UserDto>> GetAllUsers()
{
    return await QueryUsers(activeOnly: false);
}

private async Task<List<UserDto>> QueryUsers(bool activeOnly)
{
    var query = _db.Users.AsQueryable();
    if (activeOnly) query = query.Where(u => u.IsActive);

    return await query
        .Select(u => new UserDto { Id = u.Id, Name = u.Name, IsActive = u.IsActive })
        .ToListAsync();
}

// Call site — self-explanatory
var users = await GetAllUsers();
```

### Example 2: Multiple Boolean Parameters

#### Before (unreadable call site)

```csharp
public string FormatReport(Report report, bool includeHeader, bool includeFooter, bool landscape, bool colored)
{
    ...
}

// What do these booleans mean?
var output = FormatReport(report, true, false, true, false);
```

#### After (options object)

```csharp
public class ReportFormatOptions
{
    public bool IncludeHeader { get; set; } = true;
    public bool IncludeFooter { get; set; } = true;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public ColorMode Color { get; set; } = ColorMode.Grayscale;
}

public enum PageOrientation { Portrait, Landscape }
public enum ColorMode { Grayscale, Color }

public string FormatReport(Report report, ReportFormatOptions options)
{
    ...
}

// Call site — every choice is named
var output = FormatReport(report, new ReportFormatOptions
{
    IncludeHeader = true,
    IncludeFooter = false,
    Orientation = PageOrientation.Landscape,
    Color = ColorMode.Grayscale
});
```

### Example 3: Boolean as Type Discriminator

#### Before (flag switches type behavior)

```csharp
public decimal CalculateShipping(Order order, bool isExpress)
{
    if (isExpress)
    {
        return order.Weight * 5.0m + 15.0m;
    }
    else
    {
        return order.Weight * 2.0m + 5.0m;
    }
}

// Call site
var fee = CalculateShipping(order, true);
```

#### After (enum makes intent clear)

```csharp
public enum ShippingMethod { Standard, Express }

public decimal CalculateShipping(Order order, ShippingMethod method)
{
    return method switch
    {
        ShippingMethod.Express  => order.Weight * 5.0m + 15.0m,
        ShippingMethod.Standard => order.Weight * 2.0m + 5.0m,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}

// Call site — no ambiguity
var fee = CalculateShipping(order, ShippingMethod.Express);
```

### Example 4: Flag for Soft Delete

#### Before (delete with undo flag)

```csharp
public async Task DeleteUser(int userId, bool permanent)
{
    if (permanent)
    {
        var user = await _db.Users.FindAsync(userId);
        _db.Users.Remove(user);
    }
    else
    {
        var user = await _db.Users.FindAsync(userId);
        user.IsDeleted = true;
    }

    await _db.SaveChangesAsync();
}

// Call site — does 'false' mean "don't delete"?
await DeleteUser(userId, false);
```

#### After (separate methods with clear names)

```csharp
public async Task SoftDeleteUser(int userId)
{
    var user = await _db.Users.FindAsync(userId);
    if (user is null) return;

    user.IsDeleted = true;
    await _db.SaveChangesAsync();
}

public async Task PermanentlyDeleteUser(int userId)
{
    var user = await _db.Users.FindAsync(userId);
    if (user is null) return;

    _db.Users.Remove(user);
    await _db.SaveChangesAsync();
}

// Call site — unmistakable
await SoftDeleteUser(userId);
```

## When NOT to Apply

- **Well-known conventions** — `ascending: true` in sort functions is universally understood
- **Optional feature toggles** — `verbose: true` in logging utilities is clear enough
- **Private helpers** — A single boolean in an internal method with one caller can be fine
- **Framework requirements** — Some APIs require boolean parameters by design
