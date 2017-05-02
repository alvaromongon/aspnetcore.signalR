using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using Wtwd.Core.PublishSubscribe.Model;

namespace Wtwd.Core.PublishSubscribe.Service.Hubs
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

        public async Task SendMessage(Message message)
        {
            await Clients.Group(message.Topic).InvokeAsync("Publish", message.Content);
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
