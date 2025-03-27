using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tonic.Common.Helpers;

namespace Tonic.Common.JsonConverters;

/// <summary>
/// Encrypts string properties during Serialize and decrypts during Deserialize
/// </summary>
public class EncryptionConverter : JsonConverter<string>
{
    private static readonly byte[] _key = Convert.FromBase64String(EncryptionHelper.DEFAULT_SECRET);

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var encryptedString = JsonSerializer.Deserialize<string>(ref reader, options);

        if (!string.IsNullOrEmpty(encryptedString))
        {
            var decryptedString = EncryptionHelper.Decrypt(encryptedString, _key);
            return decryptedString;
        }

        return encryptedString;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        var encryptedString = EncryptionHelper.Encrypt(value, key: _key);

        JsonSerializer.Serialize(writer, encryptedString, options);
    }
}