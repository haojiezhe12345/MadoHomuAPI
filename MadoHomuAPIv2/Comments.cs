using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace MadoHomuAPIv2
{
    public class Comments
    {
        public class Comment : Dictionary<string, object>;

        public class PostedComment
        {
            public string? sender { get; set; }
            public string? comment { get; set; }
            public List<string>? images { get; set; }
        }

        public class CommentToWrite
        {
            public long unixTime { get; set; }
            public string? sender { get; set; }
            public string? comment { get; set; }
            public string? images { get; set; }
        }

        public static int WriteComment(CommentToWrite Comment)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var DBconnection = new SqliteConnection(@"Data Source=data\main.db");
            DBconnection.Open();
            var DBcommand = DBconnection.CreateCommand();

            if (Comment.sender == "3112611479")
            {
                return -1;
            }

            DBcommand.CommandText = $"INSERT INTO comments (time, sender, comment, image) VALUES (@time, @sender, @comment, @images)";
            DBcommand.Parameters.AddWithValue("@time", Comment.unixTime);
            DBcommand.Parameters.AddWithValue("@sender", Comment.sender);
            DBcommand.Parameters.AddWithValue("@comment", Comment.comment);
            DBcommand.Parameters.AddWithValue("@images", Comment.images ?? (object)DBNull.Value);

            var result = DBcommand.ExecuteNonQuery();

            DBconnection.Close();

            stopwatch.Stop();
            File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Written comment in {stopwatch.ElapsedMilliseconds}ms\n");

            return result;
        }
    }
}
