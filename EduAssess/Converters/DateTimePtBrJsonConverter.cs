using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduAssess.Converters;

/// <summary>
/// Lê datas no formato dd/MM/yyyy (preferencial) ou ISO yyyy-MM-dd e escreve sempre dd/MM/yyyy.
/// </summary>
public class DateOnlyPtBrJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "dd/MM/yyyy";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();

        if (!string.IsNullOrWhiteSpace(dateString))
        {
            if (DateOnly.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
                return exact;

            if (DateOnly.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
                return iso;
        }

        throw new JsonException($"Formato de data inválido. Use {Format} ou yyyy-MM-dd.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
