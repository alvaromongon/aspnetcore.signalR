using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using Wtwd.PublishSubscribe.Model;

namespace Wtwd.PublishSubscribe.Service.Hubs
{
    public class PublishSubscribeHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception ex)
        {
            return Task.CompletedTask;
        }

        public Task SendMessage(MessageWithTopic messageWithTopic)
        {
            return Clients.Group(messageWithTopic.Topic).InvokeAsync("Publish", messageWithTopic.Message);
        }

        /// <summary>
        /// Server side method called from client to subscribe to message labeled with the indicated label
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <returns></returns>
        public async Task SubscribeAsync(string topic)
        {
            await Groups.AddAsync(topic);
        }

        /// <summary>
        /// Server side method called from client to unsubscribe to message labeled with the indicated label
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <returns></returns>
        public async Task UnsubscribeAsync(string topic)
        {
            await Groups.RemoveAsync(topic);
        }
    }

}
