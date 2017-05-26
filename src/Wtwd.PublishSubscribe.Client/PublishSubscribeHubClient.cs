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

namespace Wtwd.PublishSubscribe.Client
{
    /// <summary>
    /// Proxy class for the Pusblish Subscribe hub
    /// https://github.com/aspnet/SignalR
    /// </summary>
    public class PublishSubscribeHubClient : IPublishSubscribeHubClient
    {
        private readonly Uri _serviceUrl;

        // TODO: Use Serilog abstraction. 
        // At the moment the abstraction requiere full framework due to the reference to Serilog.Settings.AppSettings library.
        private readonly ILogger<PublishSubscribeHubClient> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<string, Tuple<Type, Action<object>>> _handlers;

        private HubConnection _hubConnection;
        private Uri _hubUrl;

        private static string _hubPath = "/PublishSubscribe";
        private static string _methodReceivedName = "Publish";
        private static string _methodSendMessageName = "SendMessageAsync";
        private static string _methodSubscribeName = "SubscribeAsync";
        private static string _methodUnsubscribeName = "UnsubscribeAsync";

        /// <summary>
        /// Constructor
        /// </summary>        
        /// <param name="serviceUrl">Publish Subscriber service url</param>
        /// <param name="logger">logger</param>
        public PublishSubscribeHubClient(Uri serviceUrl, ILogger<PublishSubscribeHubClient> logger)
        {
            _serviceUrl = serviceUrl;
            _logger = logger;

            _cancellationTokenSource = new CancellationTokenSource();
            _handlers = new ConcurrentDictionary<string, Tuple<Type, Action<object>>>();

            _hubUrl = new Uri(_serviceUrl, _hubPath);
            _hubConnection = new HubConnection(_hubUrl);
        }

        /// <summary>
        /// Connect method
        /// </summary>        
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            await ConnectAsync(BuildHttpClient());
        }

        /// <summary>
        /// Connect method that will be used the passed http client
        /// For testing porpouse
        /// </summary>
        /// <param name="httpClient">Http client</param>
        /// <returns></returns>
        public async Task ConnectAsync(HttpClient httpClient)
        {
            try
            {
                httpClient = httpClient ?? BuildHttpClient();

                // TODO hardcoded transport type, should be defined by the proxy client?
                await _hubConnection.StartAsync(TransportType.LongPolling, httpClient);

                // TODO: Handle Closed and Connected (Do we need it?) events.

                _logger.LogInformation("Connected to {0}", _hubUrl);

                // Set up handler
                _hubConnection.On(_methodReceivedName, new[] { typeof(Model.Message) }, message =>
                {
                    var receivedMessage = (Model.Message)message[0];
                    HandleReceivedMessage(receivedMessage);
                });
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                _logger.LogInformation("ConnectAsync, operation cancelled");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ConnectAsync, operation cancelled");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "ConnectAsync, connection already established to hub {0}. It cannot be re-established again", _hubUrl);

                // TODO: Encapsulate in library exception
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConnectAsync, Exception trying to connect to {0}", _hubUrl);

                // TODO: Encapsulate in library exception
                throw;
            }
        }

        /// <summary>
        /// Disconnect method
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Disconnecting from {0}", _hubUrl);

            _cancellationTokenSource.Cancel();

            // TODO: This is not thread safe, but for now setting the value to null in this way
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        /// <summary>
        /// Send a message with the indicated content to the indicated topic
        /// </summary>
        /// <typeparam name="T">Content type</typeparam>
        /// <param name="topic">Topic</param>
        /// <param name="content">Content</param>
        /// <returns></returns>
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
                _logger.LogInformation("SendAsync, operation cancelled for topic {0}", topic);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SendAsync, operation cancelled for topic {0}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendAsync, Unknown exception trying to send a message to {0}", _hubUrl);

                // TODO: Log and throw library expecific exception for known exception types, other, log and throw
                throw;
            }
        }

        /// <summary>
        /// Subscribe to the indicated topic
        /// </summary>
        /// <typeparam name="T">Received content type</typeparam>
        /// <param name="topic">Topic</param>
        /// <param name="handler">Method to handle messages about this topic</param>
        /// <returns></returns>        
        public async Task SubscribeAsync<T>(string topic, Action<T> handler)
        {
            try
            {
                // Always perform the action even if the key does not exist to ensure everything is in sync
                await _hubConnection.Invoke<object>(_methodSubscribeName, _cancellationTokenSource.Token, topic);

                Action<object> castedDelegate = (o) => handler((T)o);
                var tuple = new Tuple<Type, Action<object>>(typeof(T), castedDelegate);

                // Add handlers to the generic dictionary of objects
                _handlers.AddOrUpdate(topic, tuple, (key, value) => { return tuple; });
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                _logger.LogInformation("SubscribeAsync, operation cancelled for topic {0}", topic);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SubscribeAsync, operation cancelled for topic {0}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscribeAsync, Unknown exception trying to Subscribe on topic {0} to service {1}", topic, _hubUrl);

                // TODO: Log and throw library expecific exception for known exception types, other, log and throw
                throw;
            }
        }

        /// <summary>
        /// Unsubscribe to the indicated topic
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <returns></returns>
        public async Task UnSubscribeAsync(string topic)
        {
            try
            {
                // Always perform the action even if the key does not exist to ensure everything is in sync
                await _hubConnection.Invoke<object>(_methodUnsubscribeName, _cancellationTokenSource.Token, topic);

                _handlers.TryRemove(topic, out Tuple<Type, Action<object>> removedTuple);
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                _logger.LogInformation("UnSubscribeAsync, operation cancelled for topic {0}", topic);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UnSubscribeAsync, operation cancelled for topic {0}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnSubscribeAsync, Unknown exception trying to Unsubscribe on topic {0} to service {1}", topic, _hubUrl);

                // TODO: Log and throw library expecific exception for known exception types, other, log and throw
                throw;
            }
        }

        private HttpClient BuildHttpClient()
        {
            return new HttpClient() { BaseAddress = _hubUrl };
        }

        private void HandleReceivedMessage(Model.Message receivedMessage)
        {
            _logger.LogInformation("Message Received from topic '{0}', serialized content: {0}", receivedMessage.Topic, receivedMessage.Content);

            try
            {
                if (_handlers.TryGetValue(receivedMessage.Topic, out Tuple<Type, Action<object>> tuple))
                {
                    // TODO: Improve readability with super cool c#7 tuples
                    var content = JsonConvert.DeserializeObject(receivedMessage.Content, tuple.Item1);

                    tuple.Item2.Invoke(content);
                }
            }
            catch (JsonReaderException ex)
            {
                _logger.LogError(ex, "Exception trying to deserialize received content into handle method parameter on topic {0} from service {1}", receivedMessage.Topic, _hubUrl);

                // TODO: Log and throw library expecific exception for known exception types, other, log and throw
                //throw; Cannot do anything except log it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown exception trying to handle a received message on topic {0} from service {1}", receivedMessage.Topic, _hubUrl);

                // TODO: Log and throw library expecific exception for known exception types, other, log and throw
                //throw; Cannot do anything except log it
            }
        }
    }
}
