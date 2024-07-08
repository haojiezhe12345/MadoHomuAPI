using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MadoHomuAPIv2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        // GET: api/<ValuesController>
        [HttpGet]
        public IEnumerable<Dictionary<string, dynamic>> Get(int? from, int? count, int? time, string? user, string? db, int? timeMin, int? timeMax)
        {
            List<Dictionary<string, dynamic>> comments = new();

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
                while (reader.Read())
                {
                    comments.Add(new Dictionary<string, dynamic>
            {
                {"id", reader["id"]},
                {"time", reader["time"]},
                {"sender", reader["sender"]},
                {"uid", reader["uid"]},
                {"comment", reader["comment"]},
                {"image", reader["image"]},
                {"hidden", reader["hidden"]},
            });
                }
            }

            DBconnection.Close();

            return comments;
        }

        // GET api/<ValuesController>/5
        [HttpGet("count")]
        public long Get(long? time, int? utc)
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
        }

        // POST api/<ValuesController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
