using System.Text.Json;
using System.Text.Json.Serialization;

namespace Module.Inventory.Application.UseCases.Liquidation;

/// <summary>
/// Serializa enums como snake_case (no_loss, release_cash) para que el contrato
/// de la API sea legible y no dependa del naming de C#.
/// </summary>
public sealed class SnakeCaseEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException($"Invalid {typeof(TEnum).Name} value: empty");

        var normalized = raw.Replace("_", string.Empty, StringComparison.Ordinal);
        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (string.Equals(
                    value.ToString().Replace("_", string.Empty, StringComparison.Ordinal),
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        throw new JsonException($"Invalid {typeof(TEnum).Name} value: '{raw}'");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ToSnakeCase(value.ToString()));
    }

    private static string ToSnakeCase(string name)
    {
        var result = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            // Break before an uppercase letter, or before a digit that follows a letter
            // ("LossUpTo10" -> "loss_up_to_10").
            var startsWord = char.IsUpper(c)
                || (char.IsDigit(c) && i > 0 && char.IsLetter(name[i - 1]));

            if (startsWord && i > 0)
                result.Append('_');

            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
