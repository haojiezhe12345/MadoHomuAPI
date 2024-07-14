namespace MadoHomuAPIv2
{
    public static class Comments
    {
        public class CommentPO
        {
            public int? id { get; set; }
            public long? time { get; set; }
            public string? sender { get; set; }
            public int? uid { get; set; }
            public string? comment { get; set; }
            public string? image { get; set; }
            public int? hidden { get; set; }
        }

        public class CommentVO : CommentPO
        {
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
    }
}
