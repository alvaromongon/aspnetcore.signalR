using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Wtwd.Core.PublishSubscribe.Model;

namespace Wtwd.Core.PublishSubscribe.Service.Hubs
{
    /// <summary>
    /// Publish subscribe hub
    /// https://github.com/aspnet/SignalR
    /// </summary>
    public class PublishSubscribeHub : Hub
    {
        // TODO: Use Serilog abstraction. 
        // At the moment the abstraction requiere full framework due to the reference to Serilog.Settings.AppSettings library.
        private readonly ILogger<PublishSubscribeHub> _logger;

        public PublishSubscribeHub(ILogger<PublishSubscribeHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Server side method called from client to send a message to subscriber client to the specified topic
        /// </summary>
        /// <param name="message">Message with content and topic</param>
        /// <returns></returns>
        public async Task SendMessageAsync(Message message)
        {
            // TODO: Do not send messages back to sender (Clients.OthersInGroup), note: this will brake integration tests
            await Clients.Group(message.Topic).InvokeAsync("Publish", message);

            _logger.LogInformation("Sent message '{0}' to topic '{1}'", message.Topic, message.Content);
        }

        /// <summary>
        /// Server side method called from client to subscribe to message labeled with the indicated label
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <returns></returns>
        public async Task SubscribeAsync(string topic)
        {
            await Groups.AddAsync(topic);

            _logger.LogInformation("Client '{0}' subscribed to topic '{1}'", Context.ConnectionId, topic);
        }

        /// <summary>
        /// Server side method called from client to unsubscribe to message labeled with the indicated label
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <returns></returns>
        public async Task UnsubscribeAsync(string topic)
        {
            await Groups.RemoveAsync(topic);

            _logger.LogInformation("Client '{0}' unsubscribed to topic '{1}'", Context.ConnectionId, topic);
        }
    }
}
