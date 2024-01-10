using System.Collections.Generic;
using UnityEngine.Events;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework.Messages
{
    public interface IMessageReceiver
    {
        MessageStream Stream { get; }
        bool IsConnected { get; }

        void OnMessage(Message message);
        void Connect(GameplayTag tag);
        void Disconnect();
    }
    public class MessageStream
    {
        public GameplayTag MessageTag { get; private set; }
        public List<IMessageReceiver> Receivers { get; } = new List<IMessageReceiver>();
        public int MeceiversCount => Receivers.Count;

        public UnityAction<IMessageReceiver> OnReceiverConnected;

        public UnityAction<IMessageReceiver> OnReceiverDisconnected;

        public Message PreviousMessage { get; protected set; }
        public Message CurrentMessage { get; protected set; }

        public UnityAction<Message> OnMessage;


        public MessageStream(GameplayTag streamTag)
        {
            MessageTag = streamTag;
        }

        public virtual MessageStream ConnectReceiver(IMessageReceiver receiver)
        {
            if (receiver == null) return this;
            if (Receivers.Contains(receiver)) return this;
            Receivers.Add(receiver);
            OnReceiverConnected?.Invoke(receiver);
            return this;
        }

        public virtual void DisconnectReceiver(IMessageReceiver receiver)
        {
            if (receiver == null) return;
            if (!Receivers.Contains(receiver)) return;
            Receivers.Remove(receiver);
            if (receiver.Stream != this) return;
            receiver.Disconnect();
            OnReceiverDisconnected?.Invoke(receiver);
        }
        public virtual void DisconnectAllReceivers()
        {
            Receivers.Remove(null);
            foreach (IMessageReceiver receiver in Receivers.ToArray())
            {
                if (receiver == null)
                    continue;
                DisconnectReceiver(receiver);
            }
            Receivers.Clear();
        }

        public virtual void ClearCallbacks()
        {
            OnMessage = null;
        }

        public virtual void Close()
        {
            DisconnectAllReceivers();
            ClearCallbacks();
        }
        public virtual void SendMessage(object sender, string message = "") => SendMessage(sender, null, message);
        
        public virtual void SendMessage(object sender, object value, string message)
        {
            Message toSend = MessagePool.Get();
            toSend
                .SetMessageSender(sender)
                .SetMessageValue(value)
                .SetMessage(message);

            Send(toSend);
        }

        private void Send(Message message)
        {
            message.SetStream(this);
            if (PreviousMessage != null)
            {
                PreviousMessage.Release();
            }

            PreviousMessage = CurrentMessage;
            CurrentMessage = message;
            OnMessage?.Invoke(message);
            Receivers.Remove(null);
            for (int i = 0; i < Receivers.Count; i++)
            {
                Receivers[i].OnMessage(message);
            }
        }

    }

}
