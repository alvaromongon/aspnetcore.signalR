using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Wtwd.Core.PublishSubscribe.Proxy
{
    public interface IPublishSubscribeHubProxy
    {
        Task ConnectAsync();

        Task ConnectAsync(HttpClient httpClient);

        Task DisconectAsync();

        Task SendAsync<T>(string topic, T messageContent);

        Task SubscribeAsync(string topic, Action<object> handler);

        Task UnSubscribeAsync(string topic);
    }
}
