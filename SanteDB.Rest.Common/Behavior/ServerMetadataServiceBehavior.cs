using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using System.Reflection;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// Server metadata behavior adds server information to the outbound http headers
    /// </summary>
    public class ServerMetadataServiceBehavior : IEndpointBehavior, IMessageInspector
    {
        /// <inheritdoc cref="IMessageInspector.AfterReceiveRequest(RestRequestMessage)"/>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
        }

        /// <inheritdoc cref="IEndpointBehavior.ApplyEndpointBehavior(ServiceEndpoint, EndpointDispatcher)"/>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
        }

        /// <inheritdoc cref="IMessageInspector.BeforeSendResponse(RestResponseMessage)"/>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            var assembly = Assembly.GetEntryAssembly();
            var assemblyname = assembly?.GetName();


            response.Headers.Add("X-SanteDB-Application", ApplicationServiceContext.Current.ApplicationName);
            response.Headers.Add("X-Powered-By", string.Format("{0} v{1} ({2})", assemblyname?.Name, assemblyname?.Version, assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));
        }
    }
}
