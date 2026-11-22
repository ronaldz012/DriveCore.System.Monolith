# Domain Design Guidelines

> **Scope:** All modules
> **When to use:** Writing domain entities, value objects, or business rules

The Domain is the heart of the system. It must model business rules, invariants, and entities without being contaminated by infrastructure details, databases, or application control flow (such as the `Result` pattern).

## 1. Domain Purity Principle
- **Zero external dependencies:** The domain must not reference application projects, infrastructure, or control flow utilities like `Result` or `Error`.
- **Primitive types or Value Objects:** Domain methods receive and return standard C# types or domain-specific entities/Value Objects.

## 1.b Multi-Tenant Unique Indexes

Every **unique index** on a multi-tenant entity (`IMustHaveTenant`) MUST start with `TenantId`, even when the other columns are already globally unique (e.g. GUID ids). GUIDs make cross-tenant collisions theoretically impossible — the rule is about consistency: every tenant-scoped uniqueness reads the same way in the model, the migrations and the DB.

```csharp
// ✔ Always tenant-first
entity.HasIndex(e => new { e.TenantId, e.Number }).IsUnique();
entity.HasIndex(e => new { e.TenantId, e.SaleId, e.Method }).IsUnique();

// ✘ Never tenant-less
entity.HasIndex(e => new { e.SaleId, e.Method }).IsUnique();
```

Non-unique indexes (lookups, FKs) don't require it — only **unique** ones.

## 2. The Tester-Doer Pattern

To avoid exceptions in normal business flow and maintain server performance, we separate state-changing operations into two parts:

1. **The Tester:** A method that returns a `bool`. It validates whether the operation is logically viable based on the current state. Makes no changes.
2. **The Doer:** A `void` method that performs the actual state change (modifies properties, adds records to internal collections).

### Example Implementation in an Entity:

```csharp
public class ProductVariant 
{
    public string Sku { get; private set; }
    public List<BranchInventory> BranchInventories { get; private set; } = new();
    public List<StockMovement> StockMovements { get; private set; } = new();

    // 1. TESTER: Only answers Yes or No. Pure logic.
    public bool HasSufficientStock(int quantity, Guid branchId)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        return branchInventory != null && branchInventory.Stock >= quantity;
    }

    // 2. DOER: Performs the action.
    // Assumes the application layer already validated. If there's a programming bug, throws a last-resort exception.
    public void SellStock(int quantity, Guid branchId, Guid userId, Guid referenceId, string? notes = null)
    {
        var branchInventory = BranchInventories.FirstOrDefault(bi => bi.BranchId == branchId);
        if (branchInventory == null)
            throw new InvalidOperationException($"No inventory record found for branch {branchId}");

        if (branchInventory.Stock < quantity)
            throw new InvalidOperationException($"Insufficient stock for {Sku}."); // Emergency safeguard (dev bug)

        branchInventory.Stock -= quantity;
        StockMovements.Add(StockMovement.CreateSale(branchId, Id, userId, quantity, referenceId, notes));
    }
}
```
