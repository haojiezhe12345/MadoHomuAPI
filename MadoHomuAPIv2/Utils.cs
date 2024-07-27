using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace MadoHomuAPIv2
{
    public class Utils
    {
        private static string? _encryptionKey = null;

        public static string EncryptionKey
        {
            get
            {
                if (_encryptionKey == null)
                {
                    var location = File.ReadAllText(@"data\secret_location.txt");
                    _encryptionKey = File.ReadAllText(location);
                }
                return _encryptionKey;
            }
        }

        public static void Log(string txt)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] {txt}\n");
                    break;
                }
                catch { }
            }
        }

        public static void WriteFileFromBase64(string file, string base64)
        {
            File.WriteAllBytes(file, Convert.FromBase64String(base64));
        }

        public static string WriteFileFromBase64WithRandomName(string path, string base64)
        {
            var filename = DateTime.UtcNow.Ticks.ToString();
            WriteFileFromBase64(string.Format(path, filename), base64);
            return filename;
        }

        public static string EscapeFilename(string name)
        {
            var invalidFileChars = "\\/:*?\"<>|";
            var validFileChars = "＼／：＊？＂＜＞｜";
            for (int i = 0; i < invalidFileChars.Length; i++)
            {
                name = name.Replace(invalidFileChars[i], validFileChars[i]);
            }
            return name;
        }

        public static byte[] SHA256Hash(string input, int bitsLength = SHA256.HashSizeInBits, string? salt = null)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input + salt ?? EncryptionKey));
            if (bitsLength > hash.Length * 8)
                throw new Exception($"The maximum hash length is {hash.Length * 8}, but {bitsLength} requested");
            else if (bitsLength == hash.Length * 8)
                return hash;
            else
            {
                byte[] key = new byte[bitsLength / 8];
                Array.Copy(hash, key, bitsLength / 8);
                return key;
            }
        }

        public static string EncryptString(string txt, string? key = null, string? iv = null)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256Hash(key ?? EncryptionKey, aes.KeySize);
            aes.IV = SHA256Hash(iv ?? EncryptionKey, aes.BlockSize);

            using MemoryStream memoryStream = new();
            using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(Encoding.UTF8.GetBytes(txt));
            cryptoStream.FlushFinalBlock();

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static string DecryptString(string encryptedBase64, string? key = null, string? iv = null)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = SHA256Hash(key ?? EncryptionKey, aes.KeySize);
                aes.IV = SHA256Hash(iv ?? EncryptionKey, aes.BlockSize);

                using MemoryStream memoryStream = new(Convert.FromBase64String(encryptedBase64));
                using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using StreamReader streamReader = new(cryptoStream, Encoding.UTF8);

                return streamReader.ReadToEnd();
            }
            catch (Exception e)
            {
                Log($"Failed to decode string '{encryptedBase64}', reason:\n{e}");
                return encryptedBase64;
            }
        }
    }

    public class ErrorLoggingMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                Utils.Log(e.ToString());
                throw;
            }
        }
    }

    public class PerformanceMeasureMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var st = new Stopwatch();
            st.Start();
            await next(context);
            st.Stop();
            Utils.Log($"Request took {st.ElapsedTicks} ticks");
        }
    }
}
