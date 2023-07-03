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
            DBcommand.CommandText = $"INSERT INTO comments (id, time, sender, comment) VALUES ({id}, {pendingComments[0].unixTime}, @sender, @comment)";
            DBcommand.Parameters.AddWithValue("@sender", pendingComments[0].sender);
            DBcommand.Parameters.AddWithValue("@comment", pendingComments[0].comment);
            try
            {
                var result = DBcommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                elapsed_time = stopwatch.ElapsedMilliseconds;

                var errlog = $"[{DateTime.Now}] Failed to write to database after {elapsed_time}ms, retrying. Pending to write: {pendingComments.Count}\n{e.Message}\n";
                //app.Logger.LogInformation(errlog);
                //app.Logger.LogInformation(e.Message);
                File.AppendAllTextAsync(@".\data\log.txt", errlog);
                DBconnection.Close();
                continue;
            }

            DBconnection.Close();

            pendingComments.RemoveAt(0);

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

app.MapGet("/comments", (int? from, int? count) =>
{
    List<Dictionary<string, dynamic>> comments = new();

    var DBconnection = new SqliteConnection(@"Data Source=data\main.db");
    DBconnection.Open();
    var DBcommand = DBconnection.CreateCommand();

    if (from != null)
    {
        DBcommand.CommandText = $"SELECT * FROM comments WHERE id BETWEEN {from - (count ?? 10) + 1} AND {from} ORDER BY id DESC";
    }
    else {
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

app.MapPost("/post", (PostedComment commentData) =>
{
    //totalPosts++;
    DateTimeOffset dto = new(DateTime.UtcNow);
    long unixTime = dto.ToUnixTimeSeconds();

    pendingComments.Add(new CommentToWrite
    {
        unixTime = unixTime,
        sender = commentData.sender,
        comment = commentData.comment,
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

    if (name == "" || name.Substring(0, 4) == ".jpg" || name.Contains('/') || name.Contains('\\'))
    {
        return "invalid username";
    }

    using (var stream = new FileStream(@$"data\images\avatars\{name}", FileMode.Create))
    {
        request.Form.Files[0].CopyTo(stream);
    }

    return name;
});


app.Run();

public class PostedComment
{
    public string? sender { get; set; }
    public string? comment { get; set; }
}

public class CommentToWrite
{
    public long unixTime { get; set; }
    public string? sender { get; set; }
    public string? comment { get; set; }
}
