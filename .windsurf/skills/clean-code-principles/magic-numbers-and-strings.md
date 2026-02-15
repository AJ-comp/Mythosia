# Magic Numbers & Strings

Replace unexplained literal values with named constants or enums to make code self-documenting and easy to maintain.

## Core Principles

1. **Name Every Literal** — If a number or string has business meaning, give it a name
2. **Single Source of Truth** — Define a constant once, reference it everywhere
3. **Type Safety with Enums** — Use enums instead of string/int codes for fixed sets of values
4. **Searchability** — Named constants are easy to find; the number `7` is not

## Rules

- Any literal used in a condition or calculation that isn't immediately obvious should be a named constant
- String literals used as status codes, types, or categories should be enums or static constants
- Configuration values (timeouts, limits, thresholds) should be in a config or constants class
- `0`, `1`, `-1` are acceptable only when the context is universally clear (e.g., index start, not-found)
- Never duplicate the same magic value in multiple places

## Examples

### Example 1: Numeric Literals in Business Logic

#### Before (what does 30, 3, 100 mean?)

```csharp
public decimal CalculateShippingFee(Order order)
{
    if (order.TotalWeight > 30)
    {
        return order.TotalWeight * 3.5m;
    }

    if (order.TotalPrice >= 100)
    {
        return 0;
    }

    return 5.0m;
}
```

#### After (named constants)

```csharp
private const decimal HeavyWeightThresholdKg = 30m;
private const decimal HeavyWeightRatePerKg = 3.5m;
private const decimal FreeShippingMinimumPrice = 100m;
private const decimal StandardShippingFee = 5.0m;

public decimal CalculateShippingFee(Order order)
{
    if (order.TotalWeight > HeavyWeightThresholdKg)
    {
        return order.TotalWeight * HeavyWeightRatePerKg;
    }

    if (order.TotalPrice >= FreeShippingMinimumPrice)
    {
        return 0;
    }

    return StandardShippingFee;
}
```

### Example 2: String Codes as Status

#### Before (scattered string literals)

```csharp
public void ProcessOrder(Order order)
{
    if (order.Status == "P")
    {
        order.Status = "A";
        SendNotification(order, "approved");
    }
    else if (order.Status == "A")
    {
        order.Status = "S";
        SendNotification(order, "shipped");
    }
    else if (order.Status == "S")
    {
        order.Status = "D";
        SendNotification(order, "delivered");
    }
}
```

#### After (enum for type safety)

```csharp
public enum OrderStatus
{
    Pending,
    Approved,
    Shipped,
    Delivered
}

public void ProcessOrder(Order order)
{
    if (order.Status == OrderStatus.Pending)
    {
        order.Status = OrderStatus.Approved;
        SendNotification(order, OrderStatus.Approved);
    }
    else if (order.Status == OrderStatus.Approved)
    {
        order.Status = OrderStatus.Shipped;
        SendNotification(order, OrderStatus.Shipped);
    }
    else if (order.Status == OrderStatus.Shipped)
    {
        order.Status = OrderStatus.Delivered;
        SendNotification(order, OrderStatus.Delivered);
    }
}
```

### Example 3: Configuration Values Buried in Code

#### Before (hardcoded config)

```csharp
public async Task<string> CallExternalApi(string endpoint, string payload)
{
    var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("X-Api-Key", "sk-abc123xyz");

    var response = await client.PostAsync(
        "https://api.example.com/v2/" + endpoint,
        new StringContent(payload));

    if (response.StatusCode == (HttpStatusCode)429)
    {
        await Task.Delay(3000);
        response = await client.PostAsync(
            "https://api.example.com/v2/" + endpoint,
            new StringContent(payload));
    }

    return await response.Content.ReadAsStringAsync();
}
```

#### After (externalized configuration)

```csharp
public class ApiClientOptions
{
    public string BaseUrl { get; set; } = "https://api.example.com/v2/";
    public string ApiKey { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(3);
}

public async Task<string> CallExternalApi(string endpoint, string payload)
{
    _client.Timeout = _options.Timeout;
    _client.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);

    var url = _options.BaseUrl + endpoint;
    var response = await _client.PostAsync(url, new StringContent(payload));

    if (response.StatusCode == HttpStatusCode.TooManyRequests)
    {
        await Task.Delay(_options.RetryDelay);
        response = await _client.PostAsync(url, new StringContent(payload));
    }

    return await response.Content.ReadAsStringAsync();
}
```

### Example 4: Bitwise Flags and Permissions

#### Before (what does 1, 2, 4 mean?)

```csharp
public bool HasAccess(int userPermission, string action)
{
    if (action == "read" && (userPermission & 1) != 0) return true;
    if (action == "write" && (userPermission & 2) != 0) return true;
    if (action == "delete" && (userPermission & 4) != 0) return true;
    return false;
}
```

#### After (flags enum)

```csharp
[Flags]
public enum Permission
{
    None   = 0,
    Read   = 1,
    Write  = 2,
    Delete = 4,
    All    = Read | Write | Delete
}

public bool HasAccess(Permission userPermission, Permission requiredPermission)
{
    return userPermission.HasFlag(requiredPermission);
}
```

## When NOT to Apply

- **Universally obvious values** — `0` as default, `1` as increment, `""` as empty string
- **Math constants** — `2 * Math.PI * radius` doesn't need `Two` extracted
- **Test data** — Test methods often use inline literals for clarity
- **One-time comparisons** — `if (args.Length == 0)` is clear enough without a constant
