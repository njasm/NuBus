using System;
using NuBus.Util;

namespace NuBus.Adapter
{
    public class MessageReceivedArgs : EventArgs
    {
        public string MessageFullName { get; protected set; }
        public string MessageSerialized { get; protected set; }
        public Guid MessageID { get; protected set; }

        public MessageReceivedArgs(string messageFullName, string messageSerialized, Guid messageID)
        {
            MessageID = messageID;
            MessageFullName = messageFullName;
            MessageSerialized = messageSerialized;
        }
    }
}
