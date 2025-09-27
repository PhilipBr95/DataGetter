
namespace DataGetter.Services
{
    internal interface IMqttService
    {
        Task SendMqttAsync<T>(string state, T payload);
        Task SendMqttImageAsync(MediaFile mediaFile);
    }
}