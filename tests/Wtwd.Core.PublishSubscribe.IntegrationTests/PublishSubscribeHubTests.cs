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
            //using (var httpClient = _testServer.CreateClient())
            //{
            //    var connection = new HubConnection(new Uri("http://test/PublishSubscribe"));
            //    try
            //    {
            //        await connection.StartAsync(TransportType.LongPolling, httpClient);

            //        var result = await connection.Invoke<string>("HelloWorld");

            //        Assert.Equal("Hello World!", result);
            //    }
            //    finally
            //    {
            //        await connection.DisposeAsync();
            //    }
            //}

            // Arrange
            using (var httpClient = _testServer.CreateClient())
            {
                string topic = "anyTopic";
                IPublishSubscribeHubProxy proxy = new PublishSubscribeHubProxy(new Uri("http://test/"), CreateLogger());

                // Act
                await proxy.ConnectAsync(httpClient);
                await proxy.SubscribeAsync(topic);
                await proxy.UnSubscribeAsync(topic);
                await proxy.DisconectAsync();

                // Assert
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
