using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Xml.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

/*
var DBconnection = new SqliteConnection(@"Data Source=data\main.db");
DBconnection.Open();
var DBcommand = DBconnection.CreateCommand();
*/

List<CommentToWrite> pendingComments = new();
var sqlWriterRunning = true;
Thread sqlWriter = new(() =>
{
    File.AppendAllText(@".\data\log.txt", $"\n[{DateTime.Now}] API started\n");
    var retryCount = 0;
    var maxRetryCount = 10;
    var criticalWarn = 0;

    while (sqlWriterRunning)
    {
        if (pendingComments.Count > 0)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            long elapsed_time = 0;

            long id = -99999;

            var DBconnection = new SqliteConnection(@"Data Source=data\main.db");
            DBconnection.Open();
            var DBcommand = DBconnection.CreateCommand();

            DBcommand.CommandText = $"SELECT max(id) FROM comments";
            using (var reader = DBcommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    id = (long)reader.GetValue(0) + 1;
                }
            }

            if (pendingComments[0].sender == "3112611479")
            {
                id = -99999;
            }

            //DBcommand.CommandText = $"INSERT INTO comments (id, time, sender, comment) VALUES ({id}, {pendingComments[0].unixTime}, '{pendingComments[0].sender}', '{pendingComments[0].comment}')";
            DBcommand.CommandText = $"INSERT INTO comments (id, time, sender, comment, image) VALUES ({id}, {pendingComments[0].unixTime}, @sender, @comment, @images)";
            DBcommand.Parameters.AddWithValue("@sender", pendingComments[0].sender);
            DBcommand.Parameters.AddWithValue("@comment", pendingComments[0].comment);
            DBcommand.Parameters.AddWithValue("@images", pendingComments[0].images ?? (object)DBNull.Value);
            try
            {
                var result = DBcommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                //app.Logger.LogInformation(e.Message);
                stopwatch.Stop();
                elapsed_time = stopwatch.ElapsedMilliseconds;

                DBconnection.Close();

                if (retryCount < maxRetryCount)
                {
                    retryCount++;
                    File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Failed to write to database after {elapsed_time}ms, retrying ({retryCount}/{maxRetryCount}). Pending to write: {pendingComments.Count}\nError: {e.Message}\n");
                }
                else
                {
                    File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Failed to write to database after {elapsed_time}ms, discarding this comment. Pending to write: {pendingComments.Count}\nError: {e.Message}\nSender: {pendingComments[0].sender}\nComment: {pendingComments[0].comment}\nImage: {pendingComments[0].images}\n");
                    
                    pendingComments.RemoveAt(0);
                    retryCount = 0;
                    criticalWarn++;

                    if (criticalWarn >= 3)
                    {
                        File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Too many comments have been discarded, force exiting ...\n");
                        Environment.Exit(-10000);
                    }
                }

                Thread.Sleep(1000);

                continue;
            }

            DBconnection.Close();

            pendingComments.RemoveAt(0);
            retryCount = 0;
            criticalWarn = 0;

            stopwatch.Stop();
            elapsed_time = stopwatch.ElapsedMilliseconds;
            File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Written comment #{id} in {elapsed_time}ms\n");
        }
        else
        {
            Thread.Sleep(200);
        }
    }
    File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] API stopped\n");
});
sqlWriter.Start();

app.Lifetime.ApplicationStopped.Register(() =>
{
    sqlWriterRunning = false;
});

/*
var totalPosts = 0;
var returned = 0;
Thread pendingChecker = new Thread(() =>
{
    while (true)
    {
        app.Logger.LogInformation($"Total: {totalPosts}  Returned: {returned}  Pending: {pendingComments.Count}");
        Thread.Sleep(500);
    }
});
pendingChecker.Start();
*/

app.MapGet("/comments", (int? from, int? count, int? time) =>
{
    List<Dictionary<string, dynamic>> comments = new();

    var DBconnection = new SqliteConnection(@"Data Source=data\main.db");
    DBconnection.Open();
    var DBcommand = DBconnection.CreateCommand();

    if (from != null)
    {
        DBcommand.CommandText = $"SELECT * FROM comments WHERE id BETWEEN {from - (count ?? 10) + 1} AND {from} ORDER BY id DESC";
    }
    else if (time != null)
    {
        DBcommand.CommandText = $"SELECT * FROM comments WHERE id BETWEEN (SELECT id FROM comments WHERE time >= {time} LIMIT 1) AND (SELECT id FROM comments WHERE time >= {time} LIMIT 1) + {count ?? 10} - 1 ORDER BY id DESC";
    }
    else
    {
        DBcommand.CommandText = $"SELECT * FROM comments ORDER BY id DESC LIMIT {count ?? 10}";
    }

    using (var reader = DBcommand.ExecuteReader())
    {
        while (reader.Read())
        {
            comments.Add(new Dictionary<string, dynamic>
            {
                {"id", reader.GetValue(0)},
                {"time", reader.GetValue(1)},
                {"sender", reader.GetValue(2)},
                {"comment", reader.GetValue(3)},
                {"image", reader.GetValue(4)},
                {"hidden", reader.GetValue(5)},
            });
        }
    }

    DBconnection.Close();

    if (comments.Count == 0)
    {
        comments.Add(new Dictionary<string, dynamic>
        {
            {"id", -999999},
            {"time", 0},
            {"sender", ""},
            {"comment", "你请求的留言不存在\n\n可能原因:\n\n你输入了错误的留言ID (最小-24849, 最大不超过现有留言数)\n\n你翻到了留言区底部\n\n网站的数据库出现了错误"},
            {"image", ""},
            {"hidden", 0},
        });
    }

    return comments;
});

app.MapGet("/comments/count", (long? time, int? utc) =>
{
    DateTimeOffset dto;
    if (time != null)
    {
        dto = DateTimeOffset.FromUnixTimeSeconds((long)time);
    } 
    else
    {
        dto = DateTimeOffset.UtcNow;
    }
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
});

app.MapPost("/post", (PostedComment commentData) =>
{
    if (commentData.sender == null || commentData.comment == null)
    {
        File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Ignoring a request with null sender/comment\n");
        return -1;
    }

    string? images = "";

    if (commentData.images == null)
    {
        images = null;
    }
    else
    {
        if (commentData.images.Count == 0)
        {
            images = null;
        }
        else
        {
            foreach (var image in commentData.images)
            {
                //app.Logger.LogInformation(commentData.images[0]);
                var filename = DateTime.UtcNow.Ticks.ToString();
                try
                {
                    File.WriteAllBytes(@$"data\images\posts\{filename}.jpg", Convert.FromBase64String(image));
                    images += filename + ',';
                }
                catch (Exception e)
                {
                    File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Failed to decode base64 image: {e.Message}\nThe base64 data is:\n{image}\n");
                }
            }
            images = images.TrimEnd(',');
        }
    }
    
    //app.Logger.LogInformation(images);

    //totalPosts++;
    DateTimeOffset dto = new(DateTime.UtcNow);
    long unixTime = dto.ToUnixTimeSeconds();

    pendingComments.Add(new CommentToWrite
    {
        unixTime = unixTime,
        sender = commentData.sender,
        comment = commentData.comment,
        images = images,
    });

    //returned++;
    return 1;
});

app.MapPost("/upload", (HttpRequest request) =>
{
    //app.Logger.LogInformation(request.Form.Files.Count.ToString());
    if (request.Form.Files.Count == 0)
    {
        return "Please select a file!";
    }

    //var ContentType = request.Form.Files[0].ContentType;
    var name = request.Form.Files[0].Name;
    var FileName = request.Form.Files[0].FileName;

    var invalidFileChars = "\\/:*?\"<>|";
    var validFileChars = "＼／：＊？＂＜＞｜";
    for ( int i = 0; i < invalidFileChars.Length; i++ )
    {
        name = name.Replace(invalidFileChars[i], validFileChars[i]);
    }

    if (name == "" || name == ".jpg" || name == "匿名用户.jpg" || name.Contains('/') || name.Contains('\\'))
    {
        return "invalid username";
    }

    using (var stream = new FileStream(@$"data\images\avatars\{name}", FileMode.Create))
    {
        request.Form.Files[0].CopyTo(stream);
    }

    File.AppendAllText(@".\data\log.txt", $"[{DateTime.Now}] Avatar {name} has been uploaded\n");

    return name;
});


app.Run();

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
