namespace Wtwd.PublishSubscribe.Model
{
    public class MessageWithTopic
    {
        /// <summary>
        /// Message topic
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Json serialized message
        /// </summary>
        public string Message { get; set; }
    }
}
