# Guard Clause Pattern

Write flat, readable code by handling error/edge cases first with early returns, keeping the happy path at the lowest indentation level.

## Core Principles

1. **Fail Fast** — Validate inputs and edge cases at the top of the function, return or throw immediately
2. **Flatten Nesting** — Never nest more than 2 levels deep; extract or invert conditions instead
3. **Happy Path Last** — The main success logic should be at the outermost indentation level
4. **One Reason to Indent** — Only indent for loops and the single primary condition, not for validation

## Rules

- Replace nested `if/else` chains with sequential guard clauses
- Each guard clause should handle exactly one concern
- Prefer `return` over `else` after a condition block
- Avoid `else` when the `if` block already returns/throws
- Keep functions short — if a function needs many guards, consider splitting it
- Apply the same pattern to `switch`/`match` expressions where applicable

## Examples

### Example 1: Basic Null/Validation Check

#### Before (deeply nested)

```csharp
public async Task<Result> ProcessOrder(Order order)
{
    if (order != null)
    {
        if (order.IsValid)
        {
            if (order.Items.Count > 0)
            {
                var total = order.Items.Sum(i => i.Price);
                await _repository.SaveAsync(order);
                return Result.Success(total);
            }
            else
            {
                return Result.Failure("No items");
            }
        }
        else
        {
            return Result.Failure("Invalid order");
        }
    }
    else
    {
        return Result.Failure("Order is null");
    }
}
```

#### After (guard clauses)

```csharp
public async Task<Result> ProcessOrder(Order order)
{
    if (order is null) return Result.Failure("Order is null");
    if (!order.IsValid) return Result.Failure("Invalid order");
    if (order.Items.Count == 0) return Result.Failure("No items");

    var total = order.Items.Sum(i => i.Price);
    await _repository.SaveAsync(order);
    return Result.Success(total);
}
```

### Example 2: Async Method with Multiple Conditions

#### Before (arrow code)

```csharp
public async Task DeleteStoreAsync(ulong storeId, User user)
{
    if (user != null)
    {
        var store = await _db.Stores.FindAsync(storeId);
        if (store != null)
        {
            if (store.IsActive == "Y")
            {
                var isOwner = await _db.StoreUsers
                    .AnyAsync(su => su.StoreId == storeId
                        && su.UserId == user.Id
                        && su.Role == "owner");
                if (isOwner)
                {
                    store.IsActive = "N";
                    await _db.SaveChangesAsync();
                }
            }
        }
    }
}
```

#### After (guard clauses)

```csharp
public async Task DeleteStoreAsync(ulong storeId, User user)
{
    if (user is null) return;

    var store = await _db.Stores.FindAsync(storeId);
    if (store is null) return;
    if (store.IsActive != "Y") return;

    var isOwner = await _db.StoreUsers
        .AnyAsync(su => su.StoreId == storeId
            && su.UserId == user.Id
            && su.Role == "owner");
    if (!isOwner) return;

    store.IsActive = "N";
    await _db.SaveChangesAsync();
}
```

### Example 3: Loop with Continue

#### Before (nested loop body)

```csharp
foreach (var item in items)
{
    if (item.IsEnabled)
    {
        if (item.Quantity > 0)
        {
            if (item.Price > 0)
            {
                total += item.Quantity * item.Price;
                count++;
            }
        }
    }
}
```

#### After (continue as guard)

```csharp
foreach (var item in items)
{
    if (!item.IsEnabled) continue;
    if (item.Quantity <= 0) continue;
    if (item.Price <= 0) continue;

    total += item.Quantity * item.Price;
    count++;
}
```

### Example 4: Try-Catch Flattening

#### Before (try wrapping everything)

```csharp
public async Task<Result> ChargeAsync(string billingKey, decimal amount)
{
    try
    {
        if (!string.IsNullOrEmpty(billingKey))
        {
            if (amount > 0)
            {
                var response = await _paymentApi.ChargeAsync(billingKey, amount);
                if (response.IsSuccess)
                {
                    return Result.Success(response.PaymentKey);
                }
                else
                {
                    return Result.Failure(response.ErrorMessage);
                }
            }
            else
            {
                return Result.Failure("Invalid amount");
            }
        }
        else
        {
            return Result.Failure("Missing billing key");
        }
    }
    catch (Exception ex)
    {
        return Result.Failure(ex.Message);
    }
}
```

#### After (guards + narrow try)

```csharp
public async Task<Result> ChargeAsync(string billingKey, decimal amount)
{
    if (string.IsNullOrEmpty(billingKey)) return Result.Failure("Missing billing key");
    if (amount <= 0) return Result.Failure("Invalid amount");

    try
    {
        var response = await _paymentApi.ChargeAsync(billingKey, amount);
        if (!response.IsSuccess) return Result.Failure(response.ErrorMessage);

        return Result.Success(response.PaymentKey);
    }
    catch (Exception ex)
    {
        return Result.Failure(ex.Message);
    }
}
```

## When NOT to Apply

- **Single simple if/else** — `if (x) return a; else return b;` is already flat enough
- **Pattern matching** — `switch` expressions with pattern matching are already declarative
- **Performance-critical hot paths** — where early return may prevent necessary setup/cleanup (rare)
