using System.Threading.Tasks;

namespace Wtwd.PublishSubscribe.Proxy
{
    public interface IPublishSubscribeHubProxy
    {
        Task ConnectAsync();

        Task DisconectAsync();

        Task SendAsync<T>(string topic, T messageContent);

        Task SubscribeAsync(string topic);

        Task UnSubscribeAsync(string topic);
    }
}
