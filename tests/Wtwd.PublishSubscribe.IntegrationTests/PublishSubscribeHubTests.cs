using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Wtwd.PublishSubscribe.Proxy;
using Wtwd.PublishSubscribe.Service;
using Xunit;

namespace Wtwd.PublishSubscribe.IntegrationTests
{
    // https://github.com/aspnet/SignalR/blob/dev/test/Microsoft.AspNetCore.SignalR.Client.FunctionalTests/HubConnectionTests.cs

    public class PublishSubscribeHubTests : IDisposable
    {
        private readonly TestServer _testServer;

        public PublishSubscribeHubTests()
        {
            var host = new WebHostBuilder()
                //.UseKestrel()
                //.UseContentRoot(Directory.GetCurrentDirectory())
                //.UseIISIntegration()
                .UseStartup<Startup>();
                //.UseApplicationInsights()
                //.Build();

            _testServer = new TestServer((IWebHostBuilder)host);
        }

        [Fact]
        public async Task WhenClientConnect_ThenCanSubscribeAndUnSubscribe()
        {
            // Arrange
            string topic = "anyTopic";
            IPublishSubscribeHubProxy proxy = new PublishSubscribeHubProxy(new Uri("http://test"), CreateLogger());

            // Act
            await proxy.ConnectAsync();
            await proxy.SubscribeAsync(topic);
            await proxy.UnSubscribeAsync(topic);
            await proxy.DisconectAsync();

            // Assert
        }        

        public void Dispose()
        {
            _testServer.Dispose();
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
