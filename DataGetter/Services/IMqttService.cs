
namespace DataGetter.Services
{
    public interface IMqttService
    {
        Task RegisterDiscoveryAsync();
        Task SendMqttAsync<T>(string state, T payload);     
    }
}