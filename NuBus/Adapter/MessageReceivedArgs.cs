using System;
using NuBus.Util;

namespace NuBus.Adapter
{
    public class MessageReceivedArgs : EventArgs
    {
        public string MessageKey { get; protected set; }
        public string SerializedMessage { get; protected set; }
        public Guid MessageID { get; protected set; }

        public MessageReceivedArgs(string messageKey, string serializedMessage, Guid messageID)
        {
            MessageID = messageID;
            MessageKey = messageKey;
            SerializedMessage = serializedMessage;
        }
    }
}
