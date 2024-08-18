using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace MadoHomuAPIv2
{
    public static class Config
    {
        public class ConfigItems
        {
            public required string EncryptionKey { get; set; }
            public required EmailConfig Email { get; set; }
            public class EmailConfig
            {
                public required string SmtpServerAddr { get; set; }
                public required int SmtpServerPort { get; set; }
                public required string Username { get; set; }
                public required string Password { get; set; }
            }
            public bool? AllowUserNameChangeWithoutEmail { get; set; }
        }

        private static ConfigItems? _data = null;

        public static ConfigItems Data
        {
            get
            {
                if (_data == null) LoadConfig();
                if (_data == null) throw new Exception("Failed to read config");
                return _data;
            }
        }

        public static void LoadConfig()
        {
            _data = JsonSerializer.Deserialize<ConfigItems>(File.ReadAllText(File.ReadAllText(@"data\config_location.txt")));
        }
    }

    public static class Utils
    {
        public static string EncryptionKey => Config.Data.EncryptionKey;
        public static Config.ConfigItems.EmailConfig EmailConfig => Config.Data.Email;

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
            var invalidFileChars = "\\/:*?\"<>| ";
            var validFileChars = "＼／：＊？＂＜＞｜_";
            for (int i = 0; i < invalidFileChars.Length; i++)
            {
                name = name.Replace(invalidFileChars[i], validFileChars[i]);
            }
            return name;
        }

        public static byte[] SHA256HashWithSalt(string input, int bitsLength = SHA256.HashSizeInBits, string? salt = null)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input + (salt ?? EncryptionKey)));
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
            aes.Key = SHA256HashWithSalt(key ?? EncryptionKey, aes.KeySize);
            aes.IV = SHA256HashWithSalt(iv ?? EncryptionKey, aes.BlockSize);

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
                aes.Key = SHA256HashWithSalt(key ?? EncryptionKey, aes.KeySize);
                aes.IV = SHA256HashWithSalt(iv ?? EncryptionKey, aes.BlockSize);

                using MemoryStream memoryStream = new(Convert.FromBase64String(encryptedBase64));
                using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using StreamReader streamReader = new(cryptoStream, Encoding.UTF8);

                return streamReader.ReadToEnd();
            }
            catch (Exception e)
            {
                Log($"Failed to decrypt string '{encryptedBase64}', reason:\n{e}");
                return encryptedBase64;
            }
        }

        public static void SetEncryptedValue(ref string? target, string? rawValue)
        {
            if (target != null)
                throw new Exception($"Cannot set property for more than once because it already stores an encrypted value");
            if (rawValue != null) target = EncryptString(rawValue);
        }

        public static void SetDecryptedValue(ref string? target, string? encryptedValue)
        {
            if (target != null)
                throw new Exception($"Cannot set property for more than once because it already stores a decrypted value");
            if (encryptedValue != null) target = DecryptString(encryptedValue);
        }

        public static void SetHashedValue(ref string? target, string? rawValue)
        {
            if (target != null)
                throw new Exception($"Cannot set property for more than once because it already stores a hashed value");
            if (rawValue != null) target = Convert.ToBase64String(SHA256HashWithSalt(rawValue));
        }

        public static async Task<ResponseCode> SendEmail(string address, string subject, string body, bool isHtml = true)
        {
            var smtpClient = new SmtpClient(EmailConfig.SmtpServerAddr, EmailConfig.SmtpServerPort)
            {
                Credentials = new NetworkCredential(EmailConfig.Username, EmailConfig.Password),
                EnableSsl = true,
            };
            try
            {
                var mail = new MailMessage()
                {
                    From = new($"MadoHomu.love <{EmailConfig.Username}>"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml,
                };
                mail.To.Add(address);
                await smtpClient.SendMailAsync(mail);
            }
            catch (FormatException)
            {
                return ResponseCode.EmailNotValid;
            }
            catch (Exception e)
            {
                Log($"Failed to send email, reason:\n{e}");
                return ResponseCode.EmailSendFailed;
            }
            Log("Email sent successfully");
            return ResponseCode.Success;
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
