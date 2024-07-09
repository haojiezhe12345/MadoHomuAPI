using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using static MadoHomuAPIv2.Comments;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MadoHomuAPIv2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        [HttpGet]
        public List<Comment> GetComments(int? from, int? count, int? time, string? user, string? db, int? timeMin, int? timeMax)
        {
            List<Comment> comments = [];

            var table = "comments";
            if (db != null)
            {
                if (db == "kami") table = "kami";
                else return comments;
            }

            var DBconnection = new SqliteConnection(@"Data Source=data\main.db");
            DBconnection.Open();
            var DBcommand = DBconnection.CreateCommand();

            if (user != null)
            {
                DBcommand.CommandText = $"SELECT * FROM {table} WHERE sender=@sender ORDER BY id DESC LIMIT {count ?? 10} OFFSET {from ?? 0};";
                DBcommand.Parameters.AddWithValue("@sender", user);
            }
            else if (from != null)
            {
                DBcommand.CommandText = $"SELECT * FROM {table} WHERE id BETWEEN {from - (count ?? 10) + 1} AND {from} ORDER BY id DESC";
            }
            else if (time != null)
            {
                DBcommand.CommandText = $"SELECT * FROM {table} WHERE id BETWEEN (SELECT id FROM {table} WHERE time >= {time} ORDER by id ASC LIMIT 1) AND (SELECT id FROM {table} WHERE time >= {time} ORDER by id ASC LIMIT 1) + {count ?? 10} - 1 ORDER BY id DESC";
            }
            else if (timeMin != null && timeMax != null)
            {
                DBcommand.CommandText = $"SELECT * FROM {table} WHERE time BETWEEN {timeMin} AND {timeMax} ORDER BY id DESC";
            }
            else
            {
                DBcommand.CommandText = $"SELECT * FROM {table} ORDER BY id DESC LIMIT {count ?? 10}";
            }

            using (var reader = DBcommand.ExecuteReader())
            {
                List<string> columns = [];
                for (int i = 0; i < reader.FieldCount; i++) columns.Add(reader.GetName(i));

                while (reader.Read())
                {
                    Comment comment = [];
                    columns.ForEach(column => comment[column] = reader[column]);
                    comments.Add(comment);
                }
            }

            DBconnection.Close();

            return comments;
        }

        [HttpGet("count")]
        public long GetCommentCount(long? time, int? utc)
        {
            DateTimeOffset dto = time == null ? DateTimeOffset.UtcNow : DateTimeOffset.FromUnixTimeSeconds((long)time);
            dto = dto.AddHours((double)(utc ?? 8));
            long timeMin = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 0, 0, 0, new TimeSpan(utc ?? 8, 0, 0)).ToUnixTimeSeconds();
            long timeMax = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 23, 59, 59, new TimeSpan(utc ?? 8, 0, 0)).ToUnixTimeSeconds();

            long count = 0;

            var DBconnection = new SqliteConnection(@"Data Source=data\main.db");
            DBconnection.Open();
            var DBcommand = DBconnection.CreateCommand();

            DBcommand.CommandText = $"SELECT count(*) FROM comments WHERE time BETWEEN {timeMin} AND {timeMax}";
            using (var reader = DBcommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    count = (long)reader.GetValue(0);
                }
            }

            DBconnection.Close();

            //return new List<string> { dto.ToString(), timeMin.ToString(), dto.ToUnixTimeSeconds().ToString(), timeMax.ToString() };
            return count;
        }

        [HttpPost("")]
        [HttpPost("/post")]
        public int Post(PostedComment CommentData)
        {
            if (CommentData.sender == null || CommentData.comment == null)
            {
                System.IO.File.AppendAllTextAsync(@".\data\log.txt", $"[{DateTime.Now}] Ignoring a request with null sender/comment\n");
                return -1;
            }

            string? images = null;

            if (CommentData.images != null && CommentData.images.Count != 0)
            {
                images = "";
                foreach (var image in CommentData.images)
                {
                    //app.Logger.LogInformation(commentData.images[0]);
                    var filename = DateTime.UtcNow.Ticks.ToString();
                    try
                    {
                        System.IO.File.WriteAllBytes(@$"data\images\posts\{filename}.jpg", Convert.FromBase64String(image));
                        images += filename + ',';
                    }
                    catch (Exception e)
                    {
                        System.IO.File.AppendAllTextAsync(@".\data\log.txt", $"[{DateTime.Now}] Failed to decode base64 image: {e.Message}\nThe base64 data is:\n{image}\n");
                    }
                }
                images = images.TrimEnd(',');
            }

            //app.Logger.LogInformation(images);

            DateTimeOffset dto = new(DateTime.UtcNow);
            long TimeStamp = dto.ToUnixTimeSeconds();

            return WriteComment(new CommentToWrite
            {
                unixTime = TimeStamp,
                sender = CommentData.sender,
                comment = CommentData.comment,
                images = images,
            });
        }
    }
}
