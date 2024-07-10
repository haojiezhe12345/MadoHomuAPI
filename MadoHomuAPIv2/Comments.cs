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
            public long? timestamp { get; set; }
            public string? sender { get; set; }
            public string? comment { get; set; }
            public string? images { get; set; }
        }

        public static int WriteComment(CommentToWrite Comment)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var DBconnection = Database.OpenNewConnection();
            DBconnection.Open();
            var DBcommand = DBconnection.CreateCommand();

            if (Comment.sender == "3112611479")
            {
                return -1;
            }

            DBcommand.CommandText = $"INSERT INTO comments (time, sender, comment, image) VALUES (@time, @sender, @comment, @images)";
            DBcommand.Parameters.AddWithValue("@time", Comment.timestamp);
            DBcommand.Parameters.AddWithValue("@sender", Comment.sender);
            DBcommand.Parameters.AddWithValue("@comment", Comment.comment);
            DBcommand.Parameters.AddWithValue("@images", Comment.images ?? (object)DBNull.Value);

            var result = DBcommand.ExecuteNonQuery();

            DBconnection.Close();

            stopwatch.Stop();
            Logging.Log($"Written comment in {stopwatch.ElapsedMilliseconds}ms");

            return result;
        }
    }
}
