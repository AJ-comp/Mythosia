# Function Decomposition

Break large, complex functions into small, focused units that each do one thing well. Each function should operate at a single level of abstraction.

## Core Principles

1. **Single Responsibility** — A function should do exactly one thing and do it completely
2. **One Level of Abstraction** — Don't mix high-level orchestration with low-level details
3. **Small Functions** — If a function exceeds ~20 lines, it probably does too much
4. **Descriptive Extraction** — Extract a block of code into a named function that describes *what* it does

## Rules

- If you need a comment to explain *what* a code block does, extract it into a well-named function
- A function should have no more than 3–4 parameters; group related parameters into an object
- Avoid "and" in function names — it signals multiple responsibilities (`validateAndSave`, `fetchAndTransform`)
- Each function should have one clear return type and purpose
- Keep side effects explicit and isolated — separate queries from commands
- Private helper methods are cheap; don't hesitate to create them

## Examples

### Example 1: Monolithic Function

#### Before (one function does everything)

```csharp
public async Task<Result> RegisterUser(RegisterRequest request)
{
    // validate
    if (string.IsNullOrEmpty(request.Email)) return Result.Failure("Email required");
    if (!request.Email.Contains("@")) return Result.Failure("Invalid email");
    if (string.IsNullOrEmpty(request.Password)) return Result.Failure("Password required");
    if (request.Password.Length < 8) return Result.Failure("Password too short");
    if (!request.Password.Any(char.IsUpper)) return Result.Failure("Need uppercase");
    if (!request.Password.Any(char.IsDigit)) return Result.Failure("Need digit");

    // check duplicate
    var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (existing != null) return Result.Failure("Email taken");

    // create user
    var salt = GenerateSalt();
    var hash = HashPassword(request.Password, salt);
    var user = new User
    {
        Email = request.Email,
        PasswordHash = hash,
        Salt = salt,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };
    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    // send welcome email
    var template = await _templateService.GetAsync("welcome");
    var body = template.Body.Replace("{{name}}", request.Name);
    await _emailService.SendAsync(request.Email, "Welcome!", body);

    return Result.Success(user.Id);
}
```

#### After (decomposed into focused functions)

```csharp
public async Task<Result> RegisterUser(RegisterRequest request)
{
    var validation = ValidateRegistration(request);
    if (!validation.IsSuccess) return validation;

    if (await IsEmailTaken(request.Email)) return Result.Failure("Email taken");

    var user = await CreateUser(request);
    await SendWelcomeEmail(user.Email, request.Name);

    return Result.Success(user.Id);
}

private Result ValidateRegistration(RegisterRequest request)
{
    if (string.IsNullOrEmpty(request.Email)) return Result.Failure("Email required");
    if (!request.Email.Contains("@")) return Result.Failure("Invalid email");
    if (string.IsNullOrEmpty(request.Password)) return Result.Failure("Password required");
    if (request.Password.Length < 8) return Result.Failure("Password too short");
    if (!request.Password.Any(char.IsUpper)) return Result.Failure("Need uppercase");
    if (!request.Password.Any(char.IsDigit)) return Result.Failure("Need digit");

    return Result.Success();
}

private async Task<bool> IsEmailTaken(string email)
{
    return await _db.Users.AnyAsync(u => u.Email == email);
}

private async Task<User> CreateUser(RegisterRequest request)
{
    var salt = GenerateSalt();
    var hash = HashPassword(request.Password, salt);
    var user = new User
    {
        Email = request.Email,
        PasswordHash = hash,
        Salt = salt,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };
    _db.Users.Add(user);
    await _db.SaveChangesAsync();
    return user;
}

private async Task SendWelcomeEmail(string email, string name)
{
    var template = await _templateService.GetAsync("welcome");
    var body = template.Body.Replace("{{name}}", name);
    await _emailService.SendAsync(email, "Welcome!", body);
}
```

### Example 2: Mixed Levels of Abstraction

#### Before (high-level and low-level mixed)

```csharp
public async Task<Report> GenerateMonthlyReport(int year, int month)
{
    var startDate = new DateTime(year, month, 1);
    var endDate = startDate.AddMonths(1).AddDays(-1);

    var orders = await _db.Orders
        .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
        .Include(o => o.Items)
        .ToListAsync();

    var totalRevenue = 0m;
    var totalOrders = orders.Count;
    var categoryBreakdown = new Dictionary<string, decimal>();

    foreach (var order in orders)
    {
        foreach (var item in order.Items)
        {
            totalRevenue += item.Price * item.Quantity;
            if (!categoryBreakdown.ContainsKey(item.Category))
                categoryBreakdown[item.Category] = 0;
            categoryBreakdown[item.Category] += item.Price * item.Quantity;
        }
    }

    var report = new Report
    {
        Period = $"{year}-{month:D2}",
        TotalRevenue = totalRevenue,
        TotalOrders = totalOrders,
        CategoryBreakdown = categoryBreakdown
    };

    var html = $"<h1>Report {report.Period}</h1><p>Revenue: {report.TotalRevenue}</p>";
    await _emailService.SendAsync("admin@example.com", "Monthly Report", html);

    return report;
}
```

#### After (each function at one abstraction level)

```csharp
public async Task<Report> GenerateMonthlyReport(int year, int month)
{
    var dateRange = GetMonthDateRange(year, month);
    var orders = await GetOrdersInRange(dateRange);
    var report = BuildReport(year, month, orders);

    await NotifyAdmin(report);

    return report;
}

private (DateTime Start, DateTime End) GetMonthDateRange(int year, int month)
{
    var start = new DateTime(year, month, 1);
    return (start, start.AddMonths(1).AddDays(-1));
}

private async Task<List<Order>> GetOrdersInRange((DateTime Start, DateTime End) range)
{
    return await _db.Orders
        .Where(o => o.CreatedAt >= range.Start && o.CreatedAt <= range.End)
        .Include(o => o.Items)
        .ToListAsync();
}

private Report BuildReport(int year, int month, List<Order> orders)
{
    var categoryBreakdown = CalculateCategoryBreakdown(orders);
    var totalRevenue = categoryBreakdown.Values.Sum();

    return new Report
    {
        Period = $"{year}-{month:D2}",
        TotalRevenue = totalRevenue,
        TotalOrders = orders.Count,
        CategoryBreakdown = categoryBreakdown
    };
}

private Dictionary<string, decimal> CalculateCategoryBreakdown(List<Order> orders)
{
    return orders
        .SelectMany(o => o.Items)
        .GroupBy(i => i.Category)
        .ToDictionary(g => g.Key, g => g.Sum(i => i.Price * i.Quantity));
}

private async Task NotifyAdmin(Report report)
{
    var html = $"<h1>Report {report.Period}</h1><p>Revenue: {report.TotalRevenue}</p>";
    await _emailService.SendAsync("admin@example.com", "Monthly Report", html);
}
```

### Example 3: Too Many Parameters

#### Before (parameter overload)

```csharp
public async Task CreateOrder(
    string customerName,
    string customerEmail,
    string customerPhone,
    string shippingStreet,
    string shippingCity,
    string shippingZip,
    string productId,
    int quantity,
    string couponCode)
{
    ...
}
```

#### After (group into meaningful objects)

```csharp
public async Task CreateOrder(
    CustomerInfo customer,
    ShippingAddress address,
    OrderLineItem item,
    string couponCode = null)
{
    ...
}

public record CustomerInfo(string Name, string Email, string Phone);
public record ShippingAddress(string Street, string City, string Zip);
public record OrderLineItem(string ProductId, int Quantity);
```

## When NOT to Apply

- **Simple CRUD methods** — A 10-line function that reads, maps, and returns data is fine as-is
- **One-time scripts** — Throwaway code doesn't need the same rigor
- **Over-extraction** — Creating a function for 1–2 lines that's called exactly once adds indirection without value
- **Performance-sensitive code** — Sometimes inlining is necessary for hot paths
