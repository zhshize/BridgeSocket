using MessagePack;

namespace BridgeSocket.Core.Message
{
    [MessagePackObject]
    public class EventMessage
    {
        [Key(MessageField.EventName)]
        public string Name { get; set; }

        [Key(MessageField.EventData)]
        public byte[] Data { get; set; }

        [Key(MessageField.CallbackId)]
        public string CallBackId { get; set; }

        public byte[] ToMessagePack()
        {
            return MessagePackSerializer.Serialize(this);
        }
    }
}