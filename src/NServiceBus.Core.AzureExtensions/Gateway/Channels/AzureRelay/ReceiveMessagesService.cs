
namespace NServiceBus.Gateway.Channels.AzureRelay
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Logging;

    public class ReceiveMessagesService : IReceiveMessages
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ReceiveMessagesService));

        event EventHandler<DataReceivedOnChannelArgs> DataReceived;

        public ReceiveMessagesService(EventHandler<DataReceivedOnChannelArgs> dataReceived)
        {
            DataReceived += dataReceived;
        }

        // http://stackoverflow.com/questions/1879395/how-to-generate-a-stream-from-a-string
        Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void ReceiveMessage(RelayedMessage relayedMessage)
        {
            Logger.Info("Processing incoming message.");
            if (DataReceived == null)
            {
                Logger.Warn("Noone listening for DataReceived");
                return;
            }
            try
            {
                var headers = new Dictionary<string, string>();
                for (var i = 0; i < relayedMessage.Header.HeaderKeys.GetUpperBound(0); i++)
                {
                    headers.Add(relayedMessage.Header.HeaderKeys[i], relayedMessage.Header.HeaderValues[i]);
                }

                using (var dataStream = GenerateStreamFromString(relayedMessage.Data))
                {
                    DataReceived(this, new DataReceivedOnChannelArgs
                        {
                            Headers = headers,
                            Data = dataStream
                        });
                }
                Logger.Debug("Request processing complete.");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed", ex);
                // todo: throw

            }
        }

    }
}
