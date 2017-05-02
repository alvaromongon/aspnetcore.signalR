﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Wtwd.Core.PublishSubscribe.Model;

namespace Wtwd.Core.PublishSubscribe.Proxy
{
    // https://github.com/aspnet/SignalR

    public class PublishSubscribeHubProxy : IPublishSubscribeHubProxy
    {
        private Uri _hubUrl;
        private readonly ILogger<PublishSubscribeHubProxy> _logger;

        private readonly HubConnection _hubConnection;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private static string _hubPath = "/PublishSubscribe";
        private static string _methodRecievedName = "Publish";
        private static string _methodSendMessageName = "SendMessage";
        private static string _methodSubscribeName = "SubscribeAsync";
        private static string _methodUnsubscribeName = "UnsubscribeAsync";

        public PublishSubscribeHubProxy(Uri serverUrl, ILogger<PublishSubscribeHubProxy> logger)
        {
            _hubUrl = new Uri(serverUrl, _hubPath);
            _logger = logger;

            // http://localhost:5000/hubs
            //var _hubConnection = new HubConnection(new Uri(baseUrl), new JsonNetInvocationAdapter(), loggerFactory);
            _hubConnection = new HubConnection(_hubUrl);
            _cancellationTokenSource = new CancellationTokenSource();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception trying to connect to {0}", _hubUrl);

                // TODO: Encapsulate in library exception
                throw ex;
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

        public async Task SendAsync<T>(string topic, T content)
        {            
            try
            {
                var message = new Model.Message()
                {
                    Topic = topic,
                    Content = Newtonsoft.Json.JsonConvert.SerializeObject(content)
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

        public async Task SubscribeAsync(string topic)
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

        public async Task UnSubscribeAsync(string topic)
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

        private HttpClient BuildHttpClient()
        {
            return new HttpClient() { BaseAddress = _hubUrl };
        }

        private void HandleRecievedMessage(string serializedMessage)
        {
            _logger.LogInformation("Message Recieved: {0}", serializedMessage);
        }
    }
}
