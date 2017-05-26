using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wtwd.PublishSubscribe.Client;
using Wtwd.PublishSubscribe.Service;
using Wtwd.PublishSubscribe.Service.Hubs;
using Xunit;

namespace Wtwd.PublishSubscribe.IntegrationTests
{
    public class PublishSubscribeHubTests : IDisposable
    {
        private readonly TestServer _testServer;

        public PublishSubscribeHubTests()
        {
            var webHostBuilder = new WebHostBuilder().UseStartup(typeof(Startup));
                //ConfigureServices(services =>
                //{
                //    services.AddSignalR();
                //})
                //.Configure(app =>
                //{
                //    app.UseSignalR(routes =>
                //    {
                //        routes.MapHub<PublishSubscribeHub>("/PublishSubscribe");
                //    });
                //});
            _testServer = new TestServer(webHostBuilder);
        }

        [Fact]
        public async Task WhenConnectedTwice_ThenInvalidOperationExceptionIsThrown()
        {
            // Arrange  
            IPublishSubscribeHubClient client = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());

            using (var httpClient = _testServer.CreateClient())
            {
                // Act
                await client.ConnectAsync(httpClient);

                // Assert
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.ConnectAsync(httpClient));
            }
        }

        [Fact]
        public async Task WhenDisconectAndNeverConnectedBefore_ThenItDoesNotFail()
        {
            // Arrange  
            IPublishSubscribeHubClient client = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());

            using (var httpClient = _testServer.CreateClient())
            {
                // Act
                await client.DisconnectAsync();

                // Assert
            }
        }

        [Fact]
        public async Task WhenConnectedAndDisconectTwice_ThenItDoesNotFail()
        {
            // Arrange  
            IPublishSubscribeHubClient client = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());

            using (var httpClient = _testServer.CreateClient())
            {
                // Act
                await client.ConnectAsync(httpClient);
                await client.DisconnectAsync();
                await client.DisconnectAsync();

                // Assert
            }
        }

        [Fact]
        public async Task WhenClientSubscribeToTopic_ThenCanRecieveSimpleTypeMessages()
        {
            // Arrange
            string topic = "anyTopic";
            string content = "Hi, how do you do?";

            bool messageReceived = false;
            string contentReceived = string.Empty;

            Action<string> handler = (param) => { messageReceived = true; contentReceived = param; };

            IPublishSubscribeHubClient client = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());

            using (var httpClient = _testServer.CreateClient())
            {
                // Act
                await client.ConnectAsync(httpClient);
                await client.SubscribeAsync(topic, handler);
                await client.SendAsync(topic, content);

                // Not good but needed...
                await Task.Delay(50);

                await client.UnSubscribeAsync(topic);
                await client.DisconnectAsync();

                // Assert
                Assert.True(messageReceived);
                Assert.Equal(content, contentReceived);
            }
        }

        [Fact]
        public async Task WhenClientSubscribeToTopic_ThenCanRecieveComplexTypeMessages()
        {
            // Arrange
            string topic = "anyTopic";
            DateTime content = DateTime.MaxValue;

            bool messageReceived = false;
            DateTime contentReceived = DateTime.MinValue;

            Action<DateTime> handler = (param) => { messageReceived = true; contentReceived = param; };

            IPublishSubscribeHubClient client = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());

            using (var httpClient = _testServer.CreateClient())
            {
                // Act
                await client.ConnectAsync(httpClient);
                await client.SubscribeAsync(topic, handler);
                await client.SendAsync(topic, content);

                // Not good but needed...
                await Task.Delay(50);

                await client.UnSubscribeAsync(topic);
                await client.DisconnectAsync();

                // Assert
                Assert.True(messageReceived);
                Assert.Equal(content, contentReceived);
            }
        }

        [Fact]
        public async Task WhenClientSubscribeAnsUnsubscribeToTopic_ThenDoNotRecieveMessages()
        {
            // Arrange
            string topic = "anyTopic";
            string content = "Hi, how do you do?";

            bool messageReceived = false;
            string contentReceived = string.Empty;

            Action<string> handler = (obj) => { messageReceived = true; contentReceived = obj; };

            IPublishSubscribeHubClient client = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());

            using (var httpClient = _testServer.CreateClient())
            {
                // Act
                await client.ConnectAsync(httpClient);
                await client.SubscribeAsync(topic, handler);

                // Not good but needed...
                await Task.Delay(50);

                await client.UnSubscribeAsync(topic);
                await client.SendAsync(topic, content);
                await client.DisconnectAsync();

                // Assert
                Assert.False(messageReceived);
                Assert.Equal(string.Empty, contentReceived);
            }
        }

        [Fact]
        public async Task WhenTwoClientsSubscribeExpectingDifferentTypes_HandlerWithWrongTypeIsNotExecuted()
        {
            // Arrange
            string topic = "anyTopic";
            TimeSpan content_1 = new TimeSpan(12345);

            var handler_1_executed = false;
            var handler_2_executed = false;

            Action<TimeSpan> handler_1 = (obj) => { handler_1_executed = true; };
            Action<int> handler_2 = (obj) => { handler_1_executed = true; };

            IPublishSubscribeHubClient client_1 = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());
            IPublishSubscribeHubClient client_2 = new PublishSubscribeHubClient(new Uri("http://test/"), CreateLogger());

            using (var httpClient_1 = _testServer.CreateClient())
            {
                await client_1.ConnectAsync(httpClient_1);
                await client_1.SubscribeAsync(topic, handler_1);

                using (var httpClient_2 = _testServer.CreateClient())
                {
                    await client_2.ConnectAsync(httpClient_2);
                    await client_2.SubscribeAsync(topic, handler_2);

                    await client_1.SendAsync(topic, content_1);

                    // Not good but needed...
                    await Task.Delay(50);

                    await client_2.UnSubscribeAsync(topic);
                    await client_2.DisconnectAsync();
                }

                await client_1.UnSubscribeAsync(topic);
                await client_1.DisconnectAsync();

                // Assert
                Assert.True(handler_1_executed);
                Assert.False(handler_2_executed);
            }
        }

        public void Dispose()
        {
            if (_testServer != null)
            {
                _testServer.Dispose();
            }
        }

        private static ILogger<PublishSubscribeHubClient> CreateLogger()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Trace);

            var logger = loggerFactory.CreateLogger<PublishSubscribeHubClient>();

            return logger;
        }
    }
}
