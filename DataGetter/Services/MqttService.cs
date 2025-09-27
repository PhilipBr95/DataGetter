using MQTTnet;
using System.Text.Json;

namespace DataGetter.Services
{
    internal class MqttService : IMqttService
    {
        private Settings _settings;
        private readonly ILogger<MqttService> _logger;

        public MqttService(Settings settings, ILogger<MqttService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task SendMqttAsync<T>(string state, T payload)
        {
            var payloadType = payload.GetType().Name.ToLower();
            _logger.LogInformation($"Sending {payloadType}... {state}");

            var mqttFactory = new MqttClientFactory();
            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_settings.Mqtt)
                .Build();

            var result = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            _logger.LogInformation($"Connected to MQTT broker {_settings.Mqtt}: {result.ResultCode}");

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic($"datagetter/{payloadType}/state")
                .WithPayload(state)
                .Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            var json = JsonSerializer.Serialize(payload);
            var applicationMessage2 = new MqttApplicationMessageBuilder()
                .WithTopic($"datagetter/{payloadType}/attributes")
                .WithPayload(json)
                .Build();

            await mqttClient.PublishAsync(applicationMessage2, CancellationToken.None);
            await mqttClient.DisconnectAsync();
        }

        public async Task SendMqttImageAsync(MediaFile mediaFile)
        {
            var payloadType = nameof(Media).ToLower();
            _logger.LogInformation($"Sending {payloadType}... ??");

            var mqttFactory = new MqttClientFactory();
            using var mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_settings.Mqtt)
                .Build();

            var result = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            _logger.LogInformation($"Connected to MQTT broker {_settings.Mqtt}: {result.ResultCode}");

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic($"datagetter/{payloadType}/state")
                .WithPayload(mediaFile.Data)
                .Build();

            await mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
            await mqttClient.DisconnectAsync();
        }
    }
}
