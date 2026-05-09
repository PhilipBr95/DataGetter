namespace DataGetter.Models
{
    public class Article
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string PublishedDate { get; set; }
        public string Description { get; set; }

        public string ImageUrl { get; set; }
        public string[]? Categories { get; set; }
        public string Source { get; set; }
    }
}