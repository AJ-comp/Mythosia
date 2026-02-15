# Conditional Simplification

Reduce complex boolean expressions and branching logic into clear, readable conditions that reveal business intent.

## Core Principles

1. **Extract to Explain** — Complex conditions should be extracted into well-named boolean variables or methods
2. **Reduce Branching** — Fewer branches means fewer paths to understand and test
3. **Positive Conditions First** — Prefer positive checks over double negatives
4. **Polymorphism Over Switch** — Replace type-checking conditionals with polymorphic behavior when appropriate

## Rules

- If a condition has more than 2 clauses joined by `&&`/`||`, extract it into a named method or variable
- Eliminate double negatives: `!isNotActive` → `isActive`
- Replace `if/else` chains on the same variable with `switch`, dictionary lookup, or polymorphism
- Avoid nested ternary expressions — one level is the maximum
- Merge duplicate branches: if two `if` blocks lead to the same outcome, combine their conditions
- Use De Morgan's laws to simplify: `!(a && b)` → `!a || !b` (whichever reads better)

## Examples

### Example 1: Complex Boolean Expression

#### Before (cryptic condition)

```csharp
public decimal GetDiscount(User user, Order order)
{
    if (user.Type == "premium" && order.Total > 100
        && (order.CreatedAt.DayOfWeek == DayOfWeek.Saturday
            || order.CreatedAt.DayOfWeek == DayOfWeek.Sunday)
        && !user.HasUsedWeekendDiscount
        && order.Items.All(i => i.Category != "sale"))
    {
        return order.Total * 0.15m;
    }

    return 0;
}
```

#### After (extracted into named conditions)

```csharp
public decimal GetDiscount(User user, Order order)
{
    var isPremiumUser = user.Type == "premium";
    var meetsMinimumTotal = order.Total > 100;
    var isWeekendOrder = order.CreatedAt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    var hasNotUsedWeekendDiscount = !user.HasUsedWeekendDiscount;
    var hasNoSaleItems = order.Items.All(i => i.Category != "sale");

    var isEligibleForWeekendDiscount =
        isPremiumUser && meetsMinimumTotal && isWeekendOrder
        && hasNotUsedWeekendDiscount && hasNoSaleItems;

    return isEligibleForWeekendDiscount ? order.Total * 0.15m : 0;
}
```

### Example 2: if/else Chain on Type

#### Before (long if/else chain)

```csharp
public decimal CalculateTax(string region, decimal amount)
{
    if (region == "US-CA")
    {
        return amount * 0.0725m;
    }
    else if (region == "US-NY")
    {
        return amount * 0.08m;
    }
    else if (region == "US-TX")
    {
        return amount * 0.0625m;
    }
    else if (region == "US-OR")
    {
        return 0;
    }
    else if (region == "KR")
    {
        return amount * 0.1m;
    }
    else if (region == "JP")
    {
        return amount * 0.1m;
    }
    else
    {
        return amount * 0.05m;
    }
}
```

#### After (dictionary lookup)

```csharp
private static readonly Dictionary<string, decimal> TaxRates = new()
{
    ["US-CA"] = 0.0725m,
    ["US-NY"] = 0.08m,
    ["US-TX"] = 0.0625m,
    ["US-OR"] = 0m,
    ["KR"]    = 0.1m,
    ["JP"]    = 0.1m,
};

private const decimal DefaultTaxRate = 0.05m;

public decimal CalculateTax(string region, decimal amount)
{
    var rate = TaxRates.GetValueOrDefault(region, DefaultTaxRate);
    return amount * rate;
}
```

### Example 3: Double Negatives and Confusing Logic

#### Before (hard to parse negations)

```csharp
public bool ShouldSendNotification(User user, Notification notification)
{
    if (!user.IsNotActive)
    {
        if (!notification.IsNotUrgent || !user.HasNotOptedOut)
        {
            if (!(user.DoNotDisturb && !notification.IsNotUrgent))
            {
                return true;
            }
        }
    }
    return false;
}
```

#### After (positive, flat logic)

```csharp
public bool ShouldSendNotification(User user, Notification notification)
{
    if (!user.IsActive) return false;
    if (user.HasOptedOut && !notification.IsUrgent) return false;
    if (user.DoNotDisturb && !notification.IsUrgent) return false;

    return true;
}
```

### Example 4: Nested Ternary

#### Before (nested ternary maze)

```csharp
public string GetPriceLabel(Product product)
{
    return product.Stock == 0
        ? "Out of Stock"
        : product.Discount > 0
            ? product.Discount >= 50
                ? $"SALE! ${product.Price * (1 - product.Discount / 100m):F2}"
                : $"${product.Price * (1 - product.Discount / 100m):F2} (Save {product.Discount}%)"
            : product.IsNew
                ? $"NEW ${product.Price:F2}"
                : $"${product.Price:F2}";
}
```

#### After (early return with clear intent)

```csharp
public string GetPriceLabel(Product product)
{
    if (product.Stock == 0)
        return "Out of Stock";

    var finalPrice = product.Price * (1 - product.Discount / 100m);

    if (product.Discount >= 50)
        return $"SALE! ${finalPrice:F2}";

    if (product.Discount > 0)
        return $"${finalPrice:F2} (Save {product.Discount}%)";

    if (product.IsNew)
        return $"NEW ${product.Price:F2}";

    return $"${product.Price:F2}";
}
```

### Example 5: Repeated Null Checks

#### Before (null checks scattered everywhere)

```csharp
public string GetDisplayAddress(User user)
{
    if (user != null)
    {
        if (user.Address != null)
        {
            if (user.Address.City != null && user.Address.Street != null)
            {
                return $"{user.Address.Street}, {user.Address.City}";
            }
        }
    }
    return "No address";
}
```

#### After (null-conditional + null-coalescing)

```csharp
public string GetDisplayAddress(User user)
{
    var street = user?.Address?.Street;
    var city = user?.Address?.City;

    if (street is null || city is null)
        return "No address";

    return $"{street}, {city}";
}
```

## When NOT to Apply

- **Simple if/else** — `if (x) return a; else return b;` is already clear
- **2-3 case switch** — A small switch is more readable than a dictionary for trivial cases
- **Performance-critical paths** — Dictionary lookups have overhead vs. direct comparisons
- **Readability trade-off** — If extraction makes you scroll more without gaining clarity, keep it inline
