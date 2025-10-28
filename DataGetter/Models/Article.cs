namespace DataGetter.Models
{
    internal class Article
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string PublishedDate { get; set; }
        public string Description { get; set; }

        public string ImageUrl => ImageUrl_ExtraLarge;
        public string ImageUrl_Small { get; set; }
        public string ImageUrl_Medium { get; set; }
        public string ImageUrl_Large { get; set; }
        public string ImageUrl_Larger { get; set; }
        public string ImageUrl_ExtraLarge { get; set; }
        public string? ImageUrl_Smaller { get; internal set; }
    }
}