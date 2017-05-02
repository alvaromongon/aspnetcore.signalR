namespace Wtwd.PublishSubscribe.Model
{
    public class Message
    {
        /// <summary>
        /// Message topic
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Json serialized content
        /// </summary>
        public string Content { get; set; }
    }
}
