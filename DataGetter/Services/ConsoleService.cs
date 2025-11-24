using DataGetter.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Xml;

namespace DataGetter.Services
{
    internal class ConsoleService : IHostedService
    {        
        private int _CurrentArticleIndex = 0;
        private bool _looping = true;

        private IEnumerable<Article> _Articles = new List<Article>();

        private Settings _settings = new Settings();
        private IMqttService _mqttService;

        private readonly ILogger<ConsoleService> _logger;

        public ConsoleService(IMqttService mqttService, ILogger<ConsoleService> logger)
        {
            _mqttService = mqttService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Console Service is starting.");
            _ = Task.Run(() => RunAsync());
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Console Service is stopping.");
            _looping = false;
        }

        async Task RunAsync()
        {
            try
            {
                //Allows a full refresh on load
                var isStarting = true;
                var counter = 0;
                var dayOfWeek = DateTime.Now.DayOfWeek;
                int downloadCount = 0;

                await _mqttService.RegisterDiscoveryAsync();

                //Run forever
                while (_looping)
                {
                    //Do we need to refresh the articles?
                    if (isStarting || counter >= _settings.RefreshArticlesEveryCycle)
                    {
                        if (downloadCount >= _settings.MaxDownloads)
                        {
                            counter = 0;

                            _logger.LogInformation("Max downloads reached, exiting...");
                            continue;
                        }

                        if (isStarting || IsSleeping(_settings) == false)
                        {
                            downloadCount++;
                            await DownloadArticlesAsync();
                        }
                        
                        isStarting = false;
                        counter = 0;

                        //Has the day changed?
                        if (dayOfWeek != DateTime.Now.DayOfWeek)
                        {
                            //Reset the count
                            downloadCount = 0;

                            dayOfWeek = DateTime.Now.DayOfWeek;
                            _logger.LogInformation($"Day changed to {dayOfWeek}");
                        }
                    }

                    await SendArticleAsync();

                    //Pause for the specified time
                    await Task.Delay(TimeSpan.FromSeconds(_settings.ChangeArticleEverySeconds));

                    counter++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the main loop.");
            }
        }

        private bool IsSleeping(Settings settings)
        {
            if (settings.SleepTimes == null || settings.SleepTimes.Count() == 0)
                return false;

            //Get todays SleepTimes
            var sleepTimes = settings.SleepTimes.Where(st => st.DayOfWeeks.Contains(DateTime.Now.DayOfWeek));
            var now = DateTime.Now.TimeOfDay;

            if (sleepTimes.Any(st =>
            {
                //Check if the current time is in any of the time ranges
                return st.TimeRanges != null && st.TimeRanges.Any(tr =>
                {
                    if (tr.Start < tr.End)
                        return now >= tr.Start && now <= tr.End;
                    else
                        return now >= tr.Start || now <= tr.End;
                });

            }))
            {
                _logger.LogInformation($"Sleeping @ {now}, not downloading articles...");
                return true;
            }

            return false;
        }
        
        private async Task DownloadArticlesAsync()
        {
            _logger.LogInformation("Refreshing articles...");
            var articles = new List<Article>();

            foreach (var url in _settings.Urls)
            {
                var client = new HttpClient();
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var doc = new XmlDocument();

                doc.LoadXml(content);
                var items = doc.GetElementsByTagName("item");

                foreach (XmlNode item in items)
                {
                    var article = new Article
                    {
                        Title = item["title"]?.InnerText.Trim(),
                        Link = item["link"]?.InnerText,
                        PublishedDate = item["pubDate"]?.InnerText,
                        Description = item["description"]?.InnerText,
                        ImageUrl_Smaller = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/400/"),
                        ImageUrl_Small = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/640/"),
                        ImageUrl_Medium = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/800/"),
                        ImageUrl_Large = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/1200/"),
                        ImageUrl_Larger = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/1920/"),
                        ImageUrl_ExtraLarge = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/2048/")
                    };

                    if (!IgnoreArticle(article))
                    {
                        articles.Add(article);
                        _logger.LogDebug($"Downloaded {article.Title}");
                    }
                }
            }

            _logger.LogInformation($"Downloaded {articles.Count} articles");
            _Articles = articles.OrderByDescending(a => a.PublishedDate)
                                .ToList();
        }

        private bool IgnoreArticle(Article article)
        {
            return _settings.IgnoredTitles.Any(ia =>                 
                article.Title.Contains(ia, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task SendArticleAsync()
        {
            if (_Articles.Count() == 0)
            {
                _logger.LogInformation("No articles available to send :-(");
                return;
            }

            _CurrentArticleIndex++;

            if (_CurrentArticleIndex >= _Articles.Count())
                _CurrentArticleIndex = 0;

            var article = _Articles.ElementAt(_CurrentArticleIndex);

            _logger.LogInformation("Sending {index}/{total} - {Message}", _CurrentArticleIndex, _Articles.Count(), article.Title);
            await _mqttService.SendMqttAsync(article.PublishedDate, article);
        }
    }
}