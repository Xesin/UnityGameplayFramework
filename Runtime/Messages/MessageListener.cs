using UnityEngine;
using UnityEngine.Events;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework.Messages
{
    public class MessageListener : MonoBehaviour
    {
        [SerializeField]
        private GameplayTag triggerTag;
        [SerializeField]
        private UnityEvent<Message> OnMessage;

        public MessageReceiver Receiver { get; private set; }
        public bool IsConnected { get; private set; }
        public MessageStream Stream { get; private set; }

        private void Awake()
        {
            Receiver = new MessageReceiver().SetOnMessageCallback(OnMessageReceived);
            IsConnected = false;
        }

        private void OnEnable()
        {
            ConnectReceiver();
        }

        private void OnDisable()
        {
            DisconnectReceiver();
        }

        protected void ConnectReceiver() =>
            Stream = MessageService.GetStream(triggerTag).ConnectReceiver(Receiver);

        protected void DisconnectReceiver() =>
            Stream.DisconnectReceiver(Receiver);

        private void OnMessageReceived(Message message)
        {
            OnMessage?.Invoke(message);
        }
    }

}
