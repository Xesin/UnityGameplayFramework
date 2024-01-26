using System.Collections.Generic;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework.Messages
{

    public static class MessageService
    {
        public static readonly Dictionary<GameplayTag, MessageStream> Streams = new Dictionary<GameplayTag, MessageStream>();

        public static void SendMessage(GameplayTag tag, string message = "")
        {
            SendMessage<object>(tag, null, null, message);
        }

        public static void SendMessage(GameplayTag tag, object sender, string message = "")
        {
            SendMessage<object>(tag, sender, null, message);
        }

        public static void SendMessage<T>(GameplayTag tag, object sender, T messsageValue, string message = "")
        {
            SendMessage(GetStream(tag), sender, messsageValue, message);
        }

        public static void SendMessage<T>(MessageStream stream, object sender, T messageValue, string message = "")
        {
            stream.SendMessage(sender, messageValue, message);
        }

        public static MessageStream GetStream(GameplayTag streamTag)
        {
            if (Streams.ContainsKey(streamTag))
                return Streams.GetValueOrDefault(streamTag);

            MessageStream messageStream = new MessageStream(streamTag);
            Streams.Add(streamTag, messageStream);
            return messageStream;
        }

        public static MessageStream GetStream(string streamTag)
        {
            return GetStream(GameplayTagsContainer.RequestGameplayTag(streamTag));
        }
    }

}
