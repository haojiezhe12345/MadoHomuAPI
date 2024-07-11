namespace MadoHomuAPIv2
{
    public class Utils
    {
        public static void Log(string txt) {
            File.AppendAllTextAsync(@".\data\log.txt", $"[{DateTime.Now}] {txt}\n");
        }

        public static void WriteFileFromBase64(string file, string base64)
        {
            File.WriteAllBytes(file, Convert.FromBase64String(base64));
        }
    }
}
