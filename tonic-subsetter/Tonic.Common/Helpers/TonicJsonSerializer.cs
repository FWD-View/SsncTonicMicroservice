using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Tonic.Common.Helpers;

public static class TonicJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions();

    static TonicJsonSerializer()
    {
        DefaultOptions.UseTonicSerializerOptions();
    }

    public static void UseTonicSerializerOptions(this JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        options.PropertyNamingPolicy = new TonicJsonNamingPolicy();
    }

    public static TValue? Deserialize<TValue>(string json)
    {
        return JsonSerializer.Deserialize<TValue>(json, DefaultOptions);
    }

    public static object? Deserialize(string json, Type returnType)
    {
        return JsonSerializer.Deserialize(json, returnType, DefaultOptions);
    }

    public static string Serialize<TValue>(TValue value)
    {
        return JsonSerializer.Serialize(value, DefaultOptions);
    }

    public static void Serialize<TValue>(Utf8JsonWriter writer, TValue value)
    {
        JsonSerializer.Serialize(writer, value, DefaultOptions);
    }
}

public class TonicJsonNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0])) return name;

        return FixCasing(name);
    }

    public static string FixCasing(string str)
    {
        var chars = str.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i])) break;

            var hasNext = i + 1 < chars.Length;

            // Stop when next char is already lowercase.
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                // If the next char is a space, lowercase current char before exiting.
                if (chars[i + 1] == ' ') chars[i] = char.ToLowerInvariant(chars[i]);

                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }
}