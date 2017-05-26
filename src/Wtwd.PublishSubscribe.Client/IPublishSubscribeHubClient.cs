using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Wtwd.PublishSubscribe.Client
{
    public interface IPublishSubscribeHubClient
    {
        /// <summary>
        /// Connect method
        /// </summary>
        /// <returns></returns>
        Task ConnectAsync();

        /// <summary>
        /// Connect method that will be used the passed http client
        /// For testing porpouse
        /// </summary>
        /// <param name="httpClient">Http client</param>
        /// <returns></returns>
        Task ConnectAsync(HttpClient httpClient);

        /// <summary>
        /// Disconnect method
        /// </summary>
        /// <returns></returns>
        Task DisconnectAsync();

        /// <summary>
        /// Send a message with the indicated content to the indicated topic
        /// </summary>
        /// <typeparam name="T">Content type</typeparam>
        /// <param name="topic">Topic</param>
        /// <param name="content">Content</param>
        /// <returns></returns>
        Task SendAsync<T>(string topic, T messageContent);

        /// <summary>
        /// Subscribe to the indicated topic
        /// </summary>
        /// <typeparam name="T">Received content type</typeparam>
        /// <param name="topic">Topic</param>
        /// <param name="handler">Method to handle messages about this topic</param>
        /// <returns></returns>  
        Task SubscribeAsync<T>(string topic, Action<T> handler);

        /// <summary>
        /// Unsubscribe to the indicated topic
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <returns></returns>
        Task UnSubscribeAsync(string topic);
    }
}
