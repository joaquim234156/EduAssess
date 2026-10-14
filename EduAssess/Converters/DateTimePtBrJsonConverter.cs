using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduAssess.Converters;

public class DateOnlyPtBrJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "dd/MM/yyyy";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();

        if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return DateOnly.FromDateTime(DateTime.SpecifyKind(date, DateTimeKind.Utc));
        }

        if (DateTime.TryParse(dateString, out var fallbackDate))
        {
            return DateOnly.FromDateTime(DateTime.SpecifyKind(fallbackDate, DateTimeKind.Utc));
        }

        throw new JsonException($"Formato de data inválido. Use {Format}.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}