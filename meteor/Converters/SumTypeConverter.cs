using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

public class SumTypeConverter<T1, T2> : JsonConverter<SumType<T1, T2>>
{
    public override SumType<T1, T2> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var value = JsonSerializer.Deserialize<T1>(ref reader, options);
            return new SumType<T1, T2>(value);
        }
        else
        {
            var value = JsonSerializer.Deserialize<T2>(ref reader, options);
            return new SumType<T1, T2>(value);
        }
    }

    public override void Write(Utf8JsonWriter writer, SumType<T1, T2> value, JsonSerializerOptions options)
    {
        if (value.Value is T1 t1)
            JsonSerializer.Serialize(writer, t1, options);
        else if (value.Value is T2 t2) JsonSerializer.Serialize(writer, t2, options);
    }
}