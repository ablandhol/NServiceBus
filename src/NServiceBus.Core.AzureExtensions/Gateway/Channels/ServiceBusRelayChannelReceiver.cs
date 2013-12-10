
namespace NServiceBus.Gateway.Channels.AzureRelay
{
    using System;
    using System.ServiceModel;
    using Logging;
    using Microsoft.ServiceBus;

    public class ServiceBusRelayChannelReceiver : IChannelReceiver
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ServiceBusRelayChannelReceiver));

        ServiceHost serviceHost;

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
            }
        }

        public event EventHandler<DataReceivedOnChannelArgs> DataReceived;

        public void Start(string address, int numberOfWorkerThreads)
        {
            var serviceInstance = new ReceiveMessagesService(DataReceived);
            serviceHost = new ServiceHost(serviceInstance);

            var servicePath = address.Replace("azure://", "");
            serviceHost.AddServiceEndpoint(
                typeof(IReceiveMessages),
                new NetTcpRelayBinding(),
                ServiceBusEnvironment.CreateServiceUri("sb", "bidreceiver", servicePath))
                .Behaviors.Add(new TransportClientEndpointBehavior
                {
                    TokenProvider = TokenProvider.CreateSharedSecretTokenProvider("owner", "VBHfkHHhMXTN3YojT3jX5KFLc23XV8SpOBpCesfk4e8=")
                });

            // http://stackoverflow.com/questions/5372222/specify-a-singleton-service-in-a-wcf-self-hosted-service
            var behavior = serviceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            behavior.InstanceContextMode = InstanceContextMode.Single;

            Logger.InfoFormat("Azure Service Bus Channel Receiver started with address {0}", address);

            try
            {
                serviceHost.Open();
            }
            catch (Exception ex)
            {
                var message = string.Format("Failed to start serviceHost for {0} make sure that you have admin privileges", address);
                throw new Exception(message, ex);
            }
        }
    }
}

