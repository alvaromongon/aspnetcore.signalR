using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Wtwd.Core.PublishSubscribe.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Wtwd.Core.PublishSubscribe.Service.Hubs;
using Microsoft.AspNetCore.Sockets;
using Wtwd.Core.PublishSubscribe.Service;

namespace Wtwd.Core.PublishSubscribe.IntegrationTests
{
    // https://github.com/aspnet/SignalR/blob/dev/test/Microsoft.AspNetCore.SignalR.Client.FunctionalTests/HubConnectionTests.cs

    public class PublishSubscribeHubTests : IDisposable
    {
        private readonly TestServer _testServer;

        public PublishSubscribeHubTests()
        {
            var webHostBuilder = new WebHostBuilder().
                ConfigureServices(services =>
                {
                    services.AddSignalR();
                })
                .Configure(app =>
                {
                    app.UseSignalR(routes =>
                    {
                        routes.MapHub<PublishSubscribeHub>("/PublishSubscribe");
                    });
                });
            _testServer = new TestServer(webHostBuilder);
        }

        [Fact]
        public async Task WhenClientConnect_ThenCanSubscribeAndUnSubscribe()
        {
            // Arrange
            using (var httpClient = _testServer.CreateClient())
            {
                string topic = "anyTopic";
                Action<object> handler = (obj) => { };
                IPublishSubscribeHubProxy proxy = new PublishSubscribeHubProxy(new Uri("http://test/"), CreateLogger());

                // Act
                await proxy.ConnectAsync(httpClient);
                await proxy.SubscribeAsync(topic, handler);
                await proxy.UnSubscribeAsync(topic);
                await proxy.DisconectAsync();

                // Assert
            }
        }

        [Fact]
        public async Task WhenClientSubscribeToTopic_ThenCanRecievedMessages()
        {
            // Arrange
            using (var httpClient = _testServer.CreateClient())
            {
                bool messageRecieved = false;
                string contentRecieved = string.Empty;

                string topic = "anyTopic";
                Action<object> handler = (obj) => { messageRecieved = true; contentRecieved = (string)obj; };
                string content = "Hi, how do you do?";
                IPublishSubscribeHubProxy proxy = new PublishSubscribeHubProxy(new Uri("http://test/"), CreateLogger());

                // Act
                await proxy.ConnectAsync(httpClient);
                await proxy.SubscribeAsync(topic, handler);
                await proxy.SendAsync<string>(topic, content);
                await proxy.UnSubscribeAsync(topic);
                await proxy.DisconectAsync();

                // Assert
                Assert.True(messageRecieved);
                Assert.Equal(content, contentRecieved);
            }
        }


        public void Dispose()
        {
            if (_testServer != null)
            {
                _testServer.Dispose();
            }
        }

        private static ILogger<PublishSubscribeHubProxy> CreateLogger()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Trace);

            var logger = loggerFactory.CreateLogger<PublishSubscribeHubProxy>();

            return logger;
        }
    }
}
