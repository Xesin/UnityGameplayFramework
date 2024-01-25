using UnityEngine;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework.Messages
{
    public class MessageSender : MonoBehaviour
    {
        [SerializeField]
        private GameplayTag triggerTag;

        public MessageStream Stream { get; private set; }

        public void TriggerMessage()
        {
            Message.Send(triggerTag, sender: this);
        }
    }

}
