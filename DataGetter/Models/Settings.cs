namespace DataGetter.Models
{

    public class Settings
    {
        public Source[] Sources =
        [
            new Source { SourceName = "Independent", Url = "https://www.independent.co.uk/news/uk/rss" },
            new Source { SourceName = "BBC", Url = "https://feeds.bbci.co.uk/news/rss.xml?edition=uk" },
            new Source { SourceName = "Sky", Url = "https://feeds.skynews.com/feeds/rss/uk.xml" },         
        ];

        public IgnoredSourceItem[] IgnoredTitles = [
            new IgnoredSourceItem { SourceName = "BBC", Value = "BBC News app" },
            new IgnoredSourceItem { SourceName = "BBC", Value = "Play now" },
            new IgnoredSourceItem { SourceName = "BBC", Value = "Weekly quiz" },
            new IgnoredSourceItem { SourceName = "BBC", Value = "Watch on iplayer" },
            new IgnoredSourceItem { Value = "rape" },
        ];

        public IgnoredSourceItem[] IgnoredCategories = [
            new IgnoredSourceItem { SourceName = "Metro", Value = "Entertainment" },
            new IgnoredSourceItem { SourceName = "Metro", Value = "Soap" },
            new IgnoredSourceItem { SourceName = "Metro", Value = "Sexual health" },
            new IgnoredSourceItem { SourceName = "Metro", Value = "Lifestyle" },
        ];

        public IgnoredSourceItem[] IgnoredLinks = [
            new IgnoredSourceItem{ SourceName = "BBC", Value = "https://www.bbc.co.uk/iplayer" },
            new IgnoredSourceItem{ SourceName = "BBC", Value ="https://www.bbc.co.uk/sounds" }
        ];

        public int ChangeArticleEverySeconds = 30;
        public int RefreshArticlesEveryCycle = 60;
        public int MaxDownloads = 50;
        public bool UseMqtt = false;

        public IEnumerable<SleepTime> SleepTimes =
        [
            new()
            {
                DayOfWeeks = [DayOfWeek.Monday, 
                                DayOfWeek.Tuesday, 
                                DayOfWeek.Wednesday, 
                                DayOfWeek.Thursday,
                                DayOfWeek.Friday],
                TimeRanges = [
                    new TimeRange()
                    {
                        Start = new TimeSpan(21, 0, 0),
                        End = new TimeSpan(7, 0, 0)
                    },
                    new TimeRange()
                    {
                        Start = new TimeSpan(9, 0, 0),
                        End = new TimeSpan(15, 30, 0)
                    }
                ]
            },
            new()
            {
                DayOfWeeks = [DayOfWeek.Saturday,
                                DayOfWeek.Sunday],
                TimeRanges = [
                    new TimeRange()
        
                    {
                        Start = new TimeSpan(1, 0, 0),
                        End = new TimeSpan(9, 0, 0)
                    }
                ]
            }
        ];
        public string Mqtt = "192.168.1.116";
    }
}
