using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tonic.Common.Enums;

namespace Tonic.Common.Helpers
{
    public static class EncryptionHelper
    {
        internal const string DEFAULT_SECRET = "REDACTED";
        private static readonly byte[] _aes128BitKey = GetKey();

        private static Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = _aes128BitKey;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        public static async Task CompressThenEncrypt(Stream streamSource, Stream streamDestination)
        {
            using var aesAlg = CreateAes();
            var encryptTransform = aesAlg.CreateEncryptor(aesAlg.Key, GetKey());
            await using var encryptor = new CryptoStream(streamDestination, encryptTransform, CryptoStreamMode.Write, true);
            await using var compressor = new GZipStream(encryptor, CompressionMode.Compress, true);
            await streamSource.CopyToAsync(compressor).ConfigureAwait(false);
        }

        public static Stream DecryptThenDecompress(Stream streamSource)
        {
            using var aesAlg = CreateAes();
            var decryptTransform = aesAlg.CreateDecryptor(aesAlg.Key, GetKey());
            var decryptor = new CryptoStream(streamSource, decryptTransform, CryptoStreamMode.Read);
            return new GZipStream(decryptor, CompressionMode.Decompress);
        }

        public static Stream GetEncryptCompressStream(this Stream streamDestination)
        {
            using var aesAlg = CreateAes();
            var encryptTransform = aesAlg.CreateEncryptor(aesAlg.Key, GetKey());
            var encryptor = new CryptoStream(streamDestination, encryptTransform, CryptoStreamMode.Write);
            var compressor = new GZipStream(encryptor, CompressionMode.Compress);
            return compressor;
        }

        public static string Encrypt(string plainText, bool useInitializationVector = true, byte[]? key = null)
        {
            using (var aesAlg = CreateAes())
            {
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, useInitializationVector ? aesAlg.IV : key ?? GetKey());
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        var encrypted = msEncrypt.ToArray();
                        return Convert.ToBase64String(useInitializationVector ? aesAlg.IV : Array.Empty<byte>()) + "|" + Convert.ToBase64String(encrypted);
                    }
                }
            }
        }

        public static string Decrypt(string base64EncodedIVAndCipherText, byte[]? key = null)
        {
            var ivAndCipher = base64EncodedIVAndCipherText.Split('|');
            var iv = Convert.FromBase64String(ivAndCipher[0]);
            var cipherText = Convert.FromBase64String(ivAndCipher[1]);
            using (var aesAlg = CreateAes())
            {
                aesAlg.IV = iv.Length > 0 ? iv : key ?? GetKey();

                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        private static byte[] GetKey()
        {
            var secret = TonicEnvironmentVariable.TONIC_SECRET.Get();
            return secret is null ? Convert.FromBase64String(DEFAULT_SECRET) : SHA256.HashData(Encoding.UTF8.GetBytes(secret)).Take(16).ToArray();
        }
    }
}