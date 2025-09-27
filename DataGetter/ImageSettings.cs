namespace DataGetter
{
    public class ImageSettings
    {
        public string Host { get; set; }
        public string? Password { get; set; }
        public string Username { get; set; }
        public IEnumerable<string> Paths { get; set; }
    }
}