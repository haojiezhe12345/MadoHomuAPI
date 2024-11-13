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
        public async Task<List<CommentVO>> GetComments(int? from, int? count, long? time, string? user, int? uid, string? db, long? timeMin, long? timeMax)
        {
            var table = "comments";
            if (db != null)
            {
                if (db == "kami") table = "kami";
                else return [];
            }
            count ??= 10;

            List<CommentVO> comments = [];

            using (var DBcommand = HttpContext.DbConnection().CreateCommand())
            {
                if (user != null)
                {
                    DBcommand.CommandText = $"SELECT * FROM {table} WHERE sender = @sender ORDER BY id DESC LIMIT {count} OFFSET {from ?? 0};";
                    DBcommand.Parameters.AddWithValue("@sender", user);
                }
                else if (uid != null)
                {
                    DBcommand.CommandText = $"SELECT * FROM {table} WHERE uid = {uid} ORDER BY id DESC LIMIT {count} OFFSET {from ?? 0};";
                }
                else if (from != null)
                {
                    DBcommand.CommandText = count >= 0
                        ? $"SELECT * FROM {table} WHERE id <= {from} ORDER BY id DESC LIMIT {count}"
                        : $"SELECT * FROM (SELECT * FROM {table} WHERE id >= {from} ORDER BY id ASC LIMIT {0 - count}) ORDER BY id DESC";
                }
                else if (time != null)
                {
                    DBcommand.CommandText = count >= 0
                        ? $"SELECT * FROM {table} WHERE id <= (SELECT id FROM {table} WHERE time >= {time} ORDER by id ASC LIMIT 1) ORDER BY id DESC LIMIT {count}"
                        : $"SELECT * FROM (SELECT * FROM {table} WHERE id >= (SELECT id FROM {table} WHERE time >= {time} ORDER by id ASC LIMIT 1) ORDER BY id ASC LIMIT {0 - count}) ORDER BY id DESC";
                }
                else if (timeMin != null && timeMax != null)
                {
                    DBcommand.CommandText = $"SELECT * FROM {table} WHERE time BETWEEN {timeMin} AND {timeMax} ORDER BY id DESC";
                }
                else
                {
                    DBcommand.CommandText = $"SELECT * FROM {table} ORDER BY id DESC LIMIT {count}";
                }

                comments = DBcommand.ReadAsDTOList<CommentVO>();
            }

            foreach (var comment in comments)
            {
                if (table == "comments")
                {
                    if (comment.uid != null)
                    {
                        var commentUser = HttpContext.DbConnection().GetUser("id", comment.uid);
                        if (commentUser != null)
                        {
                            comment.sender = commentUser.name;
                            comment.avatar = commentUser.avatar;
                        }
                    }

                    comment.likes = Convert.ToInt32(await HttpContext.DbConnection().ReadOneValueAsync($"SELECT count(*) FROM comment_likes WHERE commentId = {comment.id}"));
                    if (HttpContext.UserLoggedIn())
                    {
                        comment.liked = Convert.ToInt32(await HttpContext.DbConnection().ReadOneValueAsync($"SELECT count(*) FROM comment_likes WHERE commentId = {comment.id} AND userId = {HttpContext.User().id}")) > 0;
                    }
                }
                //comment.avatar ??= "default.png";
                comment.source = table;
            }

            if (from != null && count != null && count >= 0)
            {
                if (from - count >= comments.Max(c => c.id)) return [];
            }

            return comments;
        }

        [HttpGet("count")]
        public async Task<long> GetCommentCount(long? time, int? utc)
        {
            DateTimeOffset dto = time == null ? DateTimeOffset.UtcNow : DateTimeOffset.FromUnixTimeSeconds((long)time);
            dto = dto.AddHours((double)(utc ?? 8));
            long timeMin = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 0, 0, 0, new TimeSpan(utc ?? 8, 0, 0)).ToUnixTimeSeconds();
            long timeMax = new DateTimeOffset(dto.Year, dto.Month, dto.Day, 23, 59, 59, new TimeSpan(utc ?? 8, 0, 0)).ToUnixTimeSeconds();

            return (long?)await HttpContext.DbConnection().ReadOneValueAsync($"SELECT count(*) FROM comments WHERE time BETWEEN {timeMin} AND {timeMax}") ?? 0;
        }

        [HttpPost("post")]
        [HttpPost("/post")]
        public async Task<int> Post(PostedComment CommentData)
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
                    testResult = await HttpContext.DbConnection().ExecuteAsync($"INSERT INTO comments (id) VALUES (-1)");
                    await HttpContext.DbConnection().ExecuteAsync("DELETE FROM comments WHERE id = -1");
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
                    replyid = CommentData.replyid,
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

        [HttpPost("like")]
        [UserLoginRequired]
        public async Task<int> AddLike(int commentId)
        {
            return await HttpContext.DbConnection().ExecuteAsync($"INSERT OR IGNORE INTO comment_likes (commentId, userId) VALUES ({commentId}, {HttpContext.User().id})");
        }

        [HttpDelete("like")]
        [UserLoginRequired]
        public async Task<int> RemoveLike(int commentId)
        {
            return await HttpContext.DbConnection().ExecuteAsync($"DELETE FROM comment_likes WHERE commentId = {commentId} AND userId = {HttpContext.User().id}");
        }
    }
}
