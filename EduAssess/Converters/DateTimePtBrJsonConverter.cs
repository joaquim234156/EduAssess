using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduAssess.Converters;

public class DateTimePtBrJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "dd/MM/yyyy";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();

        if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        if (DateTime.TryParse(dateString, out var fallbackDate))
        {
            return DateTime.SpecifyKind(fallbackDate, DateTimeKind.Utc);
        }

        throw new JsonException($"Formato de data inválido. Use {Format}.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}