using System.Diagnostics;

namespace MadoHomuAPIv2
{
    public static class Comments
    {
        public class CommentDTO
        {
            public int? id { get; set; }
            public long? time { get; set; }
            public string? sender { get; set; }
            public int? uid { get; set; }
            public string? comment { get; set; }
            public string? image { get; set; }
            public int? hidden { get; set; }
            public string? avatar { get; set; }
            public string? source { get; set; }
        }

        public class PostedComment
        {
            public string? sender { get; set; }
            public string? comment { get; set; }
            public List<string>? images { get; set; }
        }

        public class CommentToWrite
        {
            public long? time { get; set; }
            public string? sender { get; set; }
            public int? uid { get; set; }
            public string? comment { get; set; }
            public string? image { get; set; }
        }

        public static int WriteComment(CommentToWrite Comment)
        {
            if (Comment.sender == "3112611479") return -1;

            int result;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var DBconnection = Database.OpenNewConnection())
            {
                result = DBconnection.InsertDTO("comments", Comment);
            }

            stopwatch.Stop();
            Logging.Log($"Written comment in {stopwatch.ElapsedMilliseconds}ms");

            return result;
        }
    }
}
