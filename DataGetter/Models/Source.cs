namespace DataGetter.Models
{
    public class Source
    {
        public required string SourceName { get; set; }
        public required string Url { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
