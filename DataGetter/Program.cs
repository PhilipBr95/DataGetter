using MQTTnet;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace DataGetter
{
    internal class Program
    {
        private static int _DownloadCount = 0;
        private static int _CurrentArticleIndex = 0;
        private static List<Article> _Articles = new List<Article>();
        private static Settings _settings = new Settings();

        async static Task Main(string[] args)
        {
            //Allows a full refresh on load
            var isStarting = true;
            var counter = 0;

            var mqttFactory = new MqttClientFactory();

            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_settings.Mqtt)
                .Build();

            var result = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            Log($"Connected to MQTT broker {_settings.Mqtt}: {result.ResultCode}");

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("homeassistant/device/datagetter/datagetter/config")
                .WithPayload(File.ReadAllText("./json/Discovery.json"))
                .Build();
            
            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            //Run forever
            while (true)
            {
                //Do we need to refresh the articles?
                if (isStarting || counter >= _settings.RefreshArticlesEveryCycle)
                {
                    if (_DownloadCount >= _settings.MaxDownloads)
                    {
                        Log("Max downloads reached, exiting...");
                        return;
                    }

                    if (isStarting || IsSleeping(_settings) == false)
                    {
                        _DownloadCount++;
                        await DownloadArticles();
                    }
                    else
                        _DownloadCount = 0;

                    counter = 0;
                }

                await SendArticleAsync();

                //Pause for the specified time
                await Task.Delay(TimeSpan.FromSeconds(_settings.ChangeArticleEverySeconds));
                counter++;
            }

            await mqttClient.DisconnectAsync();
        }

        private static bool IsSleeping(Settings settings)
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
                Log($"Sleeping @ {now}, not downloading articles...");
                return true;
            }

            return false;
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now}: {message}");
        }

        private static async Task DownloadArticles()
        {
            Log("Refreshing articles...");
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
                        Title = item["title"]?.InnerText,
                        Link = item["link"]?.InnerText,
                        PublishedDate = item["pubDate"]?.InnerText,
                        Description = item["description"]?.InnerText,
                        ImageUrl = item["media:thumbnail"]?.Attributes["url"]?.Value.Replace("/240/", "/400/")
                    };

                    articles.Add(article);
                    Log($"Downloaded {article.Title}");
                }
            }

            _Articles = articles.OrderByDescending(a => a.PublishedDate)
                                .ToList();
        }

        private static async Task SendArticleAsync()
        {
            if(_Articles.Count == 0)
            {
                Log("No articals available to send :-(");
                return;
            }

            _CurrentArticleIndex++;

            if (_CurrentArticleIndex >= _Articles.Count)
                _CurrentArticleIndex = 0;

            var article = _Articles[_CurrentArticleIndex];

            Log($"Sending article... {article.Title}");

            var mqttFactory = new MqttClientFactory();
            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_settings.Mqtt)
                .Build();

            var result = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            Log($"Connected to MQTT broker {_settings.Mqtt}: {result.ResultCode}");

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("datagetter2/state")
                .WithPayload(article.PublishedDate)
                .Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            var json = JsonSerializer.Serialize(article);
            var applicationMessage2 = new MqttApplicationMessageBuilder()
                .WithTopic("datagetter2/attributes")
                .WithPayload(json)
                .Build();

            await mqttClient.PublishAsync(applicationMessage2, CancellationToken.None);

            await mqttClient.DisconnectAsync();
        }
    }
}
