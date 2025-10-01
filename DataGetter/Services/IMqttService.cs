
namespace DataGetter.Services
{
    internal interface IMqttService
    {
        Task RegisterDiscoveryAsync();
        Task SendMqttAsync<T>(string state, T payload);     
    }
}