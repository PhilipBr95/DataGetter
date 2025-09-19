using System.Diagnostics.Metrics;
using System.Xml;

namespace DataGetter
{
    internal class Program
    {
        private static int _DownloadCount = 0;
        private static int _CurrentArticleIndex = 0;
        private static List<Article> _Articles = new List<Article>();

        async static Task Main(string[] args)
        {
            var settings = new Settings();
            var counter = int.MaxValue;

            //Run forever
            while (true)
            {
                //Do we need to refresh the articles?
                if (counter >= settings.RefreshArticlesEveryCycle)
                {
                    if (_DownloadCount >= settings.MaxDownloads)
                    {
                        Console.WriteLine("Max downloads reached, exiting...");
                        return;
                    }

                    Console.WriteLine("Refreshing articles...");

                    foreach (var url in settings.Urls)
                    {
                        var client = new HttpClient();
                        var response = await client.GetAsync(url);
                        var content = await response.Content.ReadAsStringAsync();
                        var doc = new XmlDocument();
                        doc.LoadXml(content);
                        var items = doc.GetElementsByTagName("item");

                        _Articles.Clear();

                        foreach (XmlNode item in items)
                        {
                            var article = new Article
                            {
                                Title = item["title"]?.InnerText,
                                Link = item["link"]?.InnerText,
                                PublishedDate = item["pubDate"]?.InnerText,
                                Description = item["description"]?.InnerText,
                                ImageUrl = item["media:thumbnail"]?.Attributes["url"]?.Value
                            };

                            _Articles.Add(article);
                            Console.WriteLine(article.Title);
                        }
                    }

                    _DownloadCount++;
                    counter = 0;
                } 

                SendArticle();

                //Pause for the specified time
                await Task.Delay(TimeSpan.FromSeconds(settings.ChangeArticleEverySeconds));
                counter++;
            }
        }

        private static void SendArticle()
        {
            _CurrentArticleIndex++;

            if (_CurrentArticleIndex >= _Articles.Count)
                _CurrentArticleIndex = 0;

            var article = _Articles[_CurrentArticleIndex];

            Console.WriteLine($"Sending article... {article.Title}");
        }
    }
}
