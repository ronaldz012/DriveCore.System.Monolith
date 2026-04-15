namespace Inventory.Infrastructure.CodeGenerator;

public static class CodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private static readonly Random Random = new();

    public static string GenerateProductCode()
    {
        var chars = new char[6];
        for (int i = 0; i < 6; i++)
        {
            chars[i] = Alphabet[Random.Next(Alphabet.Length)];
        }
        return $"P-{new string(chars)}";
    }

    public static string GenerateVariantSku(string productInternalCode)
    {
        var chars = new char[3];
        for (int i = 0; i < 3; i++)
        {
            chars[i] = Alphabet[Random.Next(Alphabet.Length)];
        }
        return $"{productInternalCode}-{new string(chars)}";
    }
}