using DataGetter.Models;
using System.Diagnostics.Metrics;
using System.Runtime;
using System.Xml;

namespace DataGetter.Services
{
    public class ConsoleService : IHostedService, IConsoleService
    {       
        private bool _looping = true;

        private static int _CurrentArticleIndex = 0;
        private static IEnumerable<Article> _Articles = new List<Article>();

        private Settings _settings = new Settings();
        private IMqttService _mqttService;

        private readonly ILogger<ConsoleService> _logger;

        public ConsoleService(IMqttService mqttService, Settings settings, ILogger<ConsoleService> logger)
        {
            _mqttService = mqttService;
            _settings = settings;
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

        private int _counter = 0;
        private int _downloadCount = 0;

        async Task RunAsync()
        {
            try
            {
                //Allows a full refresh on load
                var isStarting = true;                
                var dayOfWeek = DateTime.Now.DayOfWeek;                

                if(_settings.UseMqtt)
                    await _mqttService.RegisterDiscoveryAsync();

                //Run forever
                while (_looping)
                {
                    _logger.LogDebug($"Looping... Counter: {_counter}, DownloadCount: {_downloadCount}, DayOfWeek: {dayOfWeek}");

                    //Do we need to refresh the articles?
                    if (isStarting || _counter >= _settings.RefreshArticlesEveryCycle)
                    {
                        if (_downloadCount >= _settings.MaxDownloads)
                        {
                            _counter = 0;

                            _logger.LogInformation("Max downloads reached, exiting...");
                            continue;
                        }

                        if (isStarting || IsSleeping(_settings) == false)
                        {
                            _downloadCount++;
                            await DownloadArticlesAsync();
                        }

                        isStarting = false;
                        _counter = 0;

                        //Has the day changed?
                        if (dayOfWeek != DateTime.Now.DayOfWeek)
                        {
                            //Reset the count
                            _downloadCount = 0;

                            dayOfWeek = DateTime.Now.DayOfWeek;
                            _logger.LogInformation($"Day changed to {dayOfWeek}");
                        }
                    }

                    if(_settings.UseMqtt)
                        await SendArticleAsync();

                    //Pause for the specified time
                    await Task.Delay(TimeSpan.FromSeconds(_settings.ChangeArticleEverySeconds));

                    _counter++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the main loop.");
            }
        }
        private void ResetNumbers()
        {
            _counter = 0;
            _downloadCount = 0;
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

            var sources = _settings.Sources.Where(s => s.Enabled);
            foreach (var source in sources)
            {
                var sourceArticles = new List<Article>();
                var client = new HttpClient();
                var response = await client.GetAsync(source.Url);
                var content = await response.Content.ReadAsStringAsync();
                var doc = new XmlDocument();

                doc.LoadXml(content);
                var items = doc.GetElementsByTagName("item");

                _logger.LogInformation($"Source: {source.SourceName}");

                foreach (XmlNode item in items)
                {
                    try
                    {

                        var pubDate = DateTime.Parse(item["pubDate"]?.InnerText);
                        var title = $"{pubDate.ToString("ddd")}: {item["title"]?.InnerText.Trim()}";

                        var article = new Article
                        {
                            Source = source.SourceName,
                            Title = title,
                            Link = item["link"]?.InnerText,
                            PublishedDate = pubDate,
                            Description = item["description"]?.InnerText,
                            //ImageUrl_Smaller = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/400/"),
                            ImageUrl = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/640/") ??
                                       item["media:content"]?.Attributes["url"]?.Value.Replace("/240/", "/640/"),
                            //ImageUrl_Medium = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/800/"),
                            //ImageUrl_Large = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/1200/"),
                            //ImageUrl_Larger = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/1920/"),
                            //ImageUrl_ExtraLarge = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/2048/"),
                            Categories = (item as XmlElement)?.GetElementsByTagName("category")
                                                             ?.Cast<XmlNode>()
                                                             .Select(s => s.InnerText)
                                                             .ToArray()
                        };

                        if (article.ImageUrl is null)
                        {
                            var html = item["content:encoded"]?.InnerText;
                            var start = html?.IndexOf("<img") ?? 0;
                            if (start > 0)
                            {
                                var end = html!.IndexOf(">", start);
                                if (end > 0)
                                {
                                    var imageHtml = html.Substring(start, (end - start) + 1);
                                    article.ImageUrl = imageHtml.Split("src=\"")[1].Split("\"")[0];
                                }
                                else
                                {
                                    Console.WriteLine("No img");
                                }
                            }
                        }

                        if (!IgnoreArticle(source, article))
                        {
                            sourceArticles.Add(article);
                            _logger.LogDebug($"    Downloaded {article.Title}");
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, item["title"]?.InnerText.Trim());
                    }
                }

                articles.AddRange(sourceArticles);
                _logger.LogInformation($"Downloaded {sourceArticles.Count} articles from {source.SourceName}");
            }

            _logger.LogInformation($"Total cached articles: {articles.Count}");
            _Articles = articles.OrderByDescending(a => a.PublishedDate)
                                .ToList();
        }

        private bool IgnoreArticle(Source source, Article article)
        {            
            var ignore = _settings.IgnoredTitles
                                  .Where(w => w.SourceName is null || w.SourceName == source.SourceName)
                                  .Any(ia => article.Title.Contains(ia.Value, StringComparison.InvariantCultureIgnoreCase));

            if (ignore)
            {
                _logger.LogInformation($"    Ignoring article(Title): {article.Title}");
                return ignore;
            }

            ignore = _settings.IgnoredLinks
                              .Where(w => w.SourceName is null || w.SourceName == source.SourceName)
                              .Any(ia => article.Link.Contains(ia.Value, StringComparison.InvariantCultureIgnoreCase));

            if (ignore)
            {
                _logger.LogInformation($"    Ignoring article(Link): {article.Title}");
                return ignore;
            }

            if (article.Categories == null || article.Categories.Length == 0)
                return false;

            ignore = _settings.IgnoredCategories
                              .Where(w => w.SourceName is null || w.SourceName == source.SourceName)
                              .Any(ia => article.Categories
                              .Any(a => a.Contains(ia.Value, StringComparison.InvariantCultureIgnoreCase)));

            if (ignore)
            {
                _logger.LogInformation($"    Ignoring article(Category): {article.Title}");
                return ignore;
            }

            return false;
        }

        public async Task SendArticleAsync()
        {
            var article = await GetArticleAsync();

            if (article == null)
                return;

            _logger.LogInformation("Sending {index}/{total} - {Message}", _CurrentArticleIndex, _Articles.Count(), article.Title);
            await _mqttService.SendMqttAsync(article.PublishedDate.ToString(), article);
        }

        public async Task<Article?> GetArticleAsync()
        {
            if (_Articles?.Count() == 0)
            {
                _logger.LogInformation("No articles available to send :-(");
                return null;
            }

            //Check everything looks good
            if(_Articles.All(a => DateTime.Now - a.PublishedDate > TimeSpan.FromDays(1)))
            {
                _logger.LogWarning("All articles are older than 1 day.");

                ResetNumbers();
            }

            _CurrentArticleIndex++;

            if (_CurrentArticleIndex >= _Articles!.Count())
                _CurrentArticleIndex = 0;

            var article = _Articles!.ElementAt(_CurrentArticleIndex);
            return article;
        }

    }
}