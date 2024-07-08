namespace MadoHomuAPIv2
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
}
