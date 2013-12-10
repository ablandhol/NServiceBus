namespace NServiceBus.Gateway.Channels.AzureRelay
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.ServiceModel;
    using Logging;
    using Microsoft.ServiceBus;

    [ChannelType("servicebusrelay")]
    public class ServiceBusRelayChannelSender : IChannelSender
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ServiceBusRelayChannelSender));

        public void Send(string remoteAddress, IDictionary<string, string> headers, Stream data)
        {
            Logger.InfoFormat("Azure Relay Sending to address {0}", remoteAddress);

            var servicePath = remoteAddress.Replace("azure://", "");

            var cf = new ChannelFactory<IReceiveMessagesChannel>(
                new NetTcpRelayBinding(),
                new EndpointAddress(ServiceBusEnvironment.CreateServiceUri("sb", "bidreceiver", servicePath)));

            cf.Endpoint.Behaviors.Add(new TransportClientEndpointBehavior
            {
                TokenProvider = TokenProvider.CreateSharedSecretTokenProvider("owner", "VBHfkHHhMXTN3YojT3jX5KFLc23XV8SpOBpCesfk4e8=")
            });

            using (var channel = cf.CreateChannel())
            {
                try
                {
                    var keys = new string[headers.Count];
                    headers.Keys.CopyTo(keys, 0);

                    var values = new string[headers.Count];
                    headers.Values.CopyTo(values, 0);

                    var reader = new StreamReader(data);
                    var bodyString = reader.ReadToEnd();
                    var message = new RelayedMessage
                    {
                        Data = bodyString,
                        Header = new MessageHeader
                        {
                            HeaderKeys = keys,
                            HeaderValues = values
                        }
                    };

                    channel.ReceiveMessage(message);
                }
                catch (Exception ex)
                {
                    Logger.Error("Azure Relay: Failed to send: ", ex);
                    throw;
                }
            }
        }
    }
}
