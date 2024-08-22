using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using static MadoHomuAPIv2.Comments;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MadoHomuAPIv2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        [HttpGet]
        public List<CommentVO> GetComments(int? from, int? count, long? time, string? user, int? uid, string? db, long? timeMin, long? timeMax)
        {
            var table = "comments";
            if (db != null)
            {
                if (db == "kami") table = "kami";
                else return [];
            }

            List<CommentVO> comments = [];

            using (var DBcommand = HttpContext.DbConnection().CreateCommand())
            {
                if (user != null)
                {
                    DBcommand.CommandText = $"SELECT * FROM {table} WHERE sender = @sender ORDER BY id DESC LIMIT {count ?? 10} OFFSET {from ?? 0};";
                    DBcommand.Parameters.AddWithValue("@sender", user);
                }
                else if (uid != null)
                {
                    DBcommand.CommandText = $"SELECT * FROM {table} WHERE uid = {uid} ORDER BY id DESC LIMIT {count ?? 10} OFFSET {from ?? 0};";
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

                comments = DBcommand.ReadAsDTOList<CommentVO>();
            }

            comments.ForEach(comment =>
            {
                if (table == "comments" && comment.uid != null)
                {
                    var user = HttpContext.DbConnection().GetUser("id", comment.uid);
                    if (user != null)
                    {
                        comment.sender = user.name;
                        comment.avatar = user.avatar;
                    }
                }
                //comment.avatar ??= "default.png";
                comment.source = table;
            });

            return comments;
        }

        [HttpGet("count")]
        public long GetCommentCount(long? time, int? utc)
        {
            DateTimeOffset dto = time == null ? DateTimeOffset.UtcNow : DateTimeOffset.FromUnixTimeSeconds((long)time);
            dto = dto.AddHours((double)(utc ?? 8));
            long timeMin = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 0, 0, 0, new TimeSpan(utc ?? 8, 0, 0)).ToUnixTimeSeconds();
            long timeMax = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 23, 59, 59, new TimeSpan(utc ?? 8, 0, 0)).ToUnixTimeSeconds();

            return (long?)HttpContext.DbConnection().ReadOneValue($"SELECT count(*) FROM comments WHERE time BETWEEN {timeMin} AND {timeMax}") ?? 0;
        }

        [HttpPost("post")]
        [HttpPost("/post")]
        public int Post(PostedComment CommentData)
        {
            var user = HttpContext.UserLoggedIn() ? HttpContext.User() : null;

            if ((user == null && CommentData.sender == null) || CommentData.comment == null)
            {
                Utils.Log($"Ignoring a request with null sender/comment");
                return -1;
            }

            string? realSender = user == null ? CommentData.sender : user.name;

            string? images = null;

            if (CommentData.images != null && CommentData.images.Count != 0)
            {
                images = "";
                foreach (var image in CommentData.images)
                {
                    //app.Logger.LogInformation(commentData.images[0]);
                    try
                    {
                        images += Utils.WriteFileFromBase64WithRandomName("data/images/posts/{0}.jpg", image) + ',';
                    }
                    catch (Exception e)
                    {
                        Utils.Log($"Failed to decode base64 image: {e.Message}\nThe base64 data is:\n{image}");
                    }
                }
                images = images.TrimEnd(',');
            }

            //app.Logger.LogInformation(images);

            if (realSender == "3112611479")
            {
                int testResult;
                try
                {
                    testResult = HttpContext.DbConnection().Execute($"INSERT INTO comments (id, time, sender, uid, comment, image, hidden) VALUES (-1, -1, 'sender', -1, 'comment', 'image', 1)");
                    HttpContext.DbConnection().Execute("DELETE FROM comments WHERE id = -1");
                }
                catch
                {
                    Utils.Log($"Comment write test FAILED. Reason:");
                    throw;
                }
                Utils.Log($"Comment write test {(testResult == 1 ? "succeeded" : "FAILED. Reason: Updated rows does not equal to 1")}");
                return -1;
            }

            int result;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                result = HttpContext.DbConnection().InsertDTO("comments", new CommentToWrite
                {
                    sender = realSender,
                    uid = user?.id,
                    comment = CommentData.comment,
                    image = images,
                });
            }
            catch
            {
                Utils.Log($"Failed to write comment. Reason:");
                throw;
            }

            stopwatch.Stop();
            Utils.Log($"Written comment from '{realSender}' (id={user?.id}) in {stopwatch.ElapsedMilliseconds}ms");

            return result;
        }
    }
}
