namespace Common.Domain.Documents;

public enum InventoryCounterKey
{
    Reception,
    Transfer
}

public enum SalesCounterKey
{
    Sale,
    CashClose
}

public static class CounterKeyNames
{
    public const string Reception = "Reception";
    public const string Transfer = "Transfer";
    public const string Sale = "Sale";
    public const string CashClose = "CashClose";

    public static string ToName(this InventoryCounterKey key) => key switch
    {
        InventoryCounterKey.Reception => Reception,
        InventoryCounterKey.Transfer => Transfer,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown inventory counter key")
    };

    public static string ToName(this SalesCounterKey key) => key switch
    {
        SalesCounterKey.Sale => Sale,
        SalesCounterKey.CashClose => CashClose,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown sales counter key")
    };
}