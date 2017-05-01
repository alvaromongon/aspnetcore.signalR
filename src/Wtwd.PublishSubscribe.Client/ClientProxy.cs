using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Wtwd.PublishSubscribe.Model;

namespace Wtwd.PublishSubscribe.Client
{
    // https://github.com/aspnet/SignalR

    public class ClientProxy
    {
        private Uri _hubUrl;
        private readonly ILogger<ClientProxy> _logger;

        private readonly HubConnection _hubConnection;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private static string _hubPath = "/PublishSubscribe";
        private static string _methodRecievedName = "Publish";
        private static string _methodSendMessageName = "SendMessage";
        private static string _methodSubscribeName = "SubscribeAsync";
        private static string _methodUnsubscribeName = "UnsubscribeAsync";

        public ClientProxy(Uri serverUrl, ILogger<ClientProxy> logger)
        {
            _hubUrl = new Uri(serverUrl, _hubPath);
            _logger = logger;

            // http://localhost:5000/hubs
            //var _hubConnection = new HubConnection(new Uri(baseUrl), new JsonNetInvocationAdapter(), loggerFactory);
            _hubConnection = new HubConnection(_hubUrl);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Connect()
        {            
            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("Connected to {0}", _hubUrl);

                // Set up handler
                _hubConnection.On(_methodRecievedName, new[] { typeof(string) }, a =>
                {
                    var serializedMessage = (string)a[0];                
                    HandleRecievedMessage(serializedMessage);
                });
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await _hubConnection.DisposeAsync();
            }
        }

        public async Task DisconectAsync()
        {
            _logger.LogInformation("Disconnecting from {0}", _hubUrl);

            _cancellationTokenSource.Cancel();

            await _hubConnection.DisposeAsync();
        }

        public async Task Send<T>(string topic, T messageContent)
        {            
            try
            {
                var message = new MessageWithTopic()
                {
                    Topic = topic,
                    Message = Newtonsoft.Json.JsonConvert.SerializeObject(messageContent)
                };

                await _hubConnection.Invoke<object>(_methodSendMessageName, _cancellationTokenSource.Token, message);
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await _hubConnection.DisposeAsync();
            }
        }

        public async Task Subscribe(string topic)
        {
            try
            {
                await _hubConnection.Invoke<object>(_methodSubscribeName, _cancellationTokenSource.Token, topic);
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await _hubConnection.DisposeAsync();
            }            
        }

        public async Task UnSubscribe(string topic)
        {
            try
            {
                await _hubConnection.Invoke<object>(_methodUnsubscribeName, _cancellationTokenSource.Token, topic);
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await _hubConnection.DisposeAsync();
            }
        }

        private void HandleRecievedMessage(string serializedMessage)
        {
            _logger.LogInformation("Message Recieved: {0}", serializedMessage);
        }
    }
}
