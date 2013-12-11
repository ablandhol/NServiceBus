namespace NServiceBus.Gateway.Channels.AzureRelay
{
    using System;
    using System.ServiceModel;
    using System.Runtime.Serialization;

    [ServiceContract]
    public interface IReceiveMessages
    {
        [OperationContract]
        void ReceiveMessage(RelayedMessage relayedMessage);
    }

    // http://social.msdn.microsoft.com/Forums/vstudio/en-US/f1402d44-d1ec-451f-9b8a-c1e59fd6b85f/streaming-in-wcf-with-datacontract?forum=wcf
    [DataContract]
    public class RelayedMessage
    {
        // http://stackoverflow.com/questions/679050/how-to-return-generic-dictionary-in-a-webservice
        // http://msdn.microsoft.com/en-us/magazine/cc164135.aspx
        [DataMember(IsRequired = true)]
        public MessageHeader Header { get; set; }

        [DataMember]
        public String Data { get; set; }
    }

    [DataContract]
    public class MessageHeader
    {
        [DataMember]
        public string[] HeaderKeys { get; set; }

        [DataMember]
        public string[] HeaderValues { get; set; }
    }

    public interface IReceiveMessagesChannel: IReceiveMessages, IClientChannel
    {
        
    }
}

