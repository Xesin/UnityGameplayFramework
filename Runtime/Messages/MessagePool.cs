using System.Collections.Generic;

namespace Xesin.GameplayFramework.Messages
{
    internal static class MessagePool
    {
        private static HashSet<Message> pool { get; set; }

        private static void Initialize()
        {
            if (pool != null) return;
            pool = new HashSet<Message>();
        }

        public static Message Get()
        {
            Initialize();
            pool.Remove(null);
            Message signal = null;
            foreach (Message pooledItem in pool)
            {
                signal = pooledItem;
                pool.Remove(pooledItem);
                break;
            }

            signal ??= new Message();
            return signal;
        }

        public static void Release(this Message message)
        {
            Initialize();
            message.Reset();
            pool.Add(message);
        }
    }

}
