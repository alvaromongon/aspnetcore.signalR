using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Wtwd.Core.PublishSubscribe.Proxy
{
    // https://github.com/aspnet/SignalR

    public class PublishSubscribeHubProxy : IPublishSubscribeHubProxy
    {
        private Uri _hubUrl;
        private readonly ILogger<PublishSubscribeHubProxy> _logger;

        private readonly HubConnection _hubConnection;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<string, Action<object>> _handlers;

        private static string _hubPath = "/PublishSubscribe";
        private static string _methodRecievedName = "Publish";
        private static string _methodSendMessageName = "SendMessage";
        private static string _methodSubscribeName = "SubscribeAsync";
        private static string _methodUnsubscribeName = "UnsubscribeAsync";

        public PublishSubscribeHubProxy(Uri serverUrl, ILogger<PublishSubscribeHubProxy> logger)
        {
            _hubUrl = new Uri(serverUrl, _hubPath);
            _logger = logger;

            //var _hubConnection = new HubConnection(new Uri(baseUrl), new JsonNetInvocationAdapter(), loggerFactory);
            _hubConnection = new HubConnection(_hubUrl);
            _cancellationTokenSource = new CancellationTokenSource();
            _handlers = new ConcurrentDictionary<string, Action<object>>();
        }

        public async Task ConnectAsync()
        {
            await ConnectAsync(BuildHttpClient());
        }

        public async Task ConnectAsync(HttpClient httpClient)
        {
            try
            {
                httpClient = httpClient ?? BuildHttpClient();
                await _hubConnection.StartAsync(TransportType.LongPolling, httpClient);
                _logger.LogInformation("Connected to {0}", _hubUrl);

                // Set up handler
                _hubConnection.On(_methodRecievedName, new[] { typeof(Model.Message) }, message =>
                {
                    var recievedMessage = (Model.Message)message[0];
                    HandleRecievedMessage(recievedMessage);
                });
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception trying to connect to {0}", _hubUrl);

                // TODO: Encapsulate in library exception
                throw ex;
            }
        }

        public async Task DisconectAsync()
        {
            _logger.LogInformation("Disconnecting from {0}", _hubUrl);

            _cancellationTokenSource.Cancel();

            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }

        public async Task SendAsync<T>(string topic, T content)
        {            
            try
            {
                var message = new Model.Message()
                {
                    Topic = topic,
                    Content = JsonConvert.SerializeObject(content)
                };

                await _hubConnection.Invoke<object>(_methodSendMessageName, _cancellationTokenSource.Token, message);
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception trying to send a message to {0}", _hubUrl);

                // TODO: Encapsulate in library exception
                throw ex;
            }
        }

        public async Task SubscribeAsync(string topic, Action<object> handler)
        {
            try
            {
                // Always perform the action even if the key does not exist to ensure everything is in sync
                await _hubConnection.Invoke<object>(_methodSubscribeName, _cancellationTokenSource.Token, topic);

                _handlers.AddOrUpdate(topic, handler, (key, value) => { return handler; });
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception trying to Subscribe on topic {0} to service {1}", topic, _hubUrl);

                // TODO: Encapsulate in library exception
                throw ex;
            }
    }

        public async Task UnSubscribeAsync(string topic)
        {
            try
            {
                // Always perform the action even if the key does not exist to ensure everything is in sync
                await _hubConnection.Invoke<object>(_methodUnsubscribeName, _cancellationTokenSource.Token, topic);

                Action<object> removedHandler;
                _handlers.TryRemove(topic, out removedHandler);
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception trying to Unsubscribe on topic {0} to service {1}", topic, _hubUrl);

                // TODO: Encapsulate in library exception
                throw ex;
            }
        }

        private HttpClient BuildHttpClient()
        {
            return new HttpClient() { BaseAddress = _hubUrl };
        }

        private void HandleRecievedMessage(Model.Message recievedMessage)
        {
            _logger.LogInformation("Message Recieved from topic '{0}', message: {0}", recievedMessage.Topic, recievedMessage.Content);

            if (_handlers.TryGetValue(recievedMessage.Topic, out Action<object> messageHandler))
            {
                messageHandler.Invoke(JsonConvert.DeserializeObject(recievedMessage.Content));                
            }
        }
    }
}
