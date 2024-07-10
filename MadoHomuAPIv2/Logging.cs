namespace MadoHomuAPIv2
{
    public class Logging
    {
        public static void Log(string txt) {
            File.AppendAllTextAsync(@".\data\log.txt", $"[{DateTime.Now}] {txt}\n");
        }
    }
}
