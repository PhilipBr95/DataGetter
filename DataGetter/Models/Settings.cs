namespace DataGetter.Models
{
    internal class Settings
    {
        public string[] Urls =
        [
            "https://feeds.bbci.co.uk/news/rss.xml?edition=uk"
        ];

        public string[] IgnoredTitles = [
            "BBC News app",
            "Play now",
            "Weekly quiz",
            "Watch on iplayer",
        ];
        
        public int ChangeArticleEverySeconds = 30;
        public int RefreshArticlesEveryCycle = 60;
        public int MaxDownloads = 50;

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
