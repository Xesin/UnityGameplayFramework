using System;
using UnityEngine;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework.Messages
{
    public partial class Message
    {
        public object Value { get; internal set; }
        public Type ValueType { get; internal set; }
        public string Text { get; internal set; }
        public bool HasValue => Value != null;

        public object MessageSender { get; protected internal set; }
        public Type SenderType { get; protected internal set; }

        public float Timestamp { get; protected internal set; }

        public MessageStream Stream { get; protected internal set; }

        public Message()
        {
            Reset();
        }

        internal Message(object messageSender, object Value, string Message)
        {
            Reset()
                .SetMessageSender(messageSender)
                .SetMessageValue(Value)
                .SetMessage(Message);
        }

        internal Message(object messageSender) : this(messageSender, null, null) { }
        internal Message(object messageSender, string message) : this(messageSender, null, message) { }

        public bool TryGetValue<T>(out T value)
        {
            if (ValueType == typeof(T))
            {
                value = (T)Value;
                return true;
            }
            value = default;
            return false;
        }

        public T GetValueUnsafe<T>() => (T)Value;


        public Message Reset()
        {
            Value = null;
            ValueType = null;
            Text = null;

            MessageSender = null;
            SenderType = null;

            Timestamp = Time.time;

            return this;
        }


        internal Message SetMessageSender(object messageSender)
        {
            MessageSender = messageSender;
            SenderType = messageSender != null ? messageSender.GetType() : null;
            return this;
        }

        internal Message SetMessageValue(object value)
        {
            Value = value;
            ValueType = value != null ? value.GetType() : null;

            return this;
        }

        internal Message SetTimestamp()
        {
            Timestamp = Time.time;

            return this;
        }

        internal Message SetMessage(string message)
        {
            Text = message;

            return this;
        }

        internal Message SetStream(MessageStream stream)
        {
            Stream = stream;
            return this;
        }

        public static void Send(GameplayTag tag, string message = "") =>
            MessageService.SendMessage(tag, message);

        public static void Send(GameplayTag tag, object sender, string message = "") =>
            MessageService.SendMessage(tag, sender, message);

        public static void Send<T>(GameplayTag tag, T messsageValue, string message = "") =>
            MessageService.SendMessage(tag, null, messsageValue, message);

        public static void Send<T>(GameplayTag tag, object sender, T messsageValue, string message = "") =>
            MessageService.SendMessage(tag, sender, messsageValue, message);
    }

}
