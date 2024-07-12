namespace MadoHomuAPIv2
{
    public class Utils
    {
        public static void Log(string txt)
        {
            File.AppendAllTextAsync(@".\data\log.txt", $"[{DateTime.Now}] {txt}\n");
        }

        public static void WriteFileFromBase64(string file, string base64)
        {
            File.WriteAllBytes(file, Convert.FromBase64String(base64));
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
}
