using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Moq;
using Wtwd.PublishSubscribe.Service.Hubs;
using Xunit;

namespace Wtwd.PublishSubscribe.UnitTests.Hubs
{
    public class PublishSubscribeHubTests
    {
        public class SendMessageAsync
        {
            [Fact]
            public void WhenSubscribeAsync_ThenInvokeAsyncMethodGetsCalled()
            {
                var topic = "topic";
                var message = new Model.Message() { Topic = topic, Content = "content message" };

                // Arrange
                var loggerMock = new Mock<ILogger<PublishSubscribeHub>>();

                var clientProxyMock = new Mock<IClientProxy>();
                clientProxyMock.Setup(m => m.InvokeAsync("Publish", It.IsAny<object[]>())).Returns(Task.CompletedTask);

                var clientsMock = new Mock<IHubConnectionContext<IClientProxy>>();
                clientsMock.Setup(m => m.Group(topic)).Returns(clientProxyMock.Object);

                var hub = new PublishSubscribeHub(loggerMock.Object);

                hub.Clients = clientsMock.Object;

                // Act
                hub.SendMessageAsync(message).Wait();

                // Assert
                clientsMock.VerifyAll();
                clientProxyMock.VerifyAll();
            }
        }

        public class SubscribeAsync
        {
            [Fact]
            public void WhenSubscribeAsync_ThenAddAsyncMethodGetsCalled()
            {
                var topic = "topic";

                // Arrange
                var loggerMock = new Mock<ILogger<PublishSubscribeHub>>();
                var hub = new PublishSubscribeHub(loggerMock.Object);

                var groupManagerMock = new Mock<IGroupManager>();
                groupManagerMock.Setup(m => m.AddAsync(topic)).Returns(Task.CompletedTask);
                hub.Groups = groupManagerMock.Object;

                hub.Context = new HubCallerContextFake();

                // Act
                hub.SubscribeAsync(topic).Wait();

                // Assert
                loggerMock.VerifyAll();
                groupManagerMock.Verify();
            }
        }

        public class UnsubscribeAsync
        {
            [Fact]
            public async Task WhenUnSubscribeAsync_ThenRemoveMethodGetsCalled()
            {
                var topic = "topic";

                // Arrange
                var loggerMock = new Mock<ILogger<PublishSubscribeHub>>();
                var hub = new PublishSubscribeHub(loggerMock.Object);

                var groupManagerMock = new Mock<IGroupManager>();
                groupManagerMock.Setup(m => m.RemoveAsync(topic)).Returns(Task.CompletedTask);
                hub.Groups = groupManagerMock.Object;

                hub.Context = new HubCallerContextFake();

                // Act
                await hub.UnsubscribeAsync(topic);

                // Assert
                //loggerMock.VerifyAll();
                groupManagerMock.VerifyAll();
            }
        }

        private class HubCallerContextFake : HubCallerContext
        {
            public HubCallerContextFake() : base(new Connection("connectionId", null))
            {
            }

            public HubCallerContextFake(Connection connection) : base(connection)
            {

            }
        }
    }
}
