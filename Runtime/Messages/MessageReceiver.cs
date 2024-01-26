using PlasticGui.Configuration.CloudEdition.Welcome;
using System;
using UnityEngine;
using UnityEngine.Events;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework.Messages
{
    [Serializable]
    public class MessageReceiver : IMessageReceiver
    {
        public MessageStream Stream { get; private set; }
        public UnityAction<Message> OnSignal { get; set; }

        public bool IsConnected { get; private set; }
        public bool IsDisconnecting { get; private set; }

        public MessageReceiver()
        {
            Reset();
        }

        public IMessageReceiver Reset()
        {
            Stream = null;
            IsConnected = false;
            IsDisconnecting = false;

            return this;
        }

        public void Connect(GameplayTag tag)
        {
            if (!Application.isPlaying)
            {
                Disconnect();
                return;
            }

            if (IsConnected)
                return;

            Stream = MessageService.GetStream(tag);

            if(Stream == null)
            {
                return;
            }

            Stream.ConnectReceiver(this);
            IsConnected = true;
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            Stream.DisconnectReceiver(this);
            Stream = null;
            IsConnected = false;
        }

        public void OnMessage(Message message)
        {
            OnSignal?.Invoke(message);
        }

        
    }

    public static class MessageReceiverExtensions
    {
        public static T SetOnMessageCallback<T>(this T target, UnityAction<Message> callback) where T : MessageReceiver
        {
            target.OnSignal = callback;
            return target;
        }
    }

}
