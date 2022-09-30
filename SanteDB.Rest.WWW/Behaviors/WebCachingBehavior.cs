using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Rest.WWW.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SanteDB.Rest.WWW.Behaviors
{
    /// <summary>
    /// A service behavior that dictates browser caching controls
    /// </summary>
    public class WebCachingBehavior : IEndpointBehavior, IMessageInspector
    {
        // Extensions which may be cached
        private readonly string[] m_cacheExtensions;
        private readonly WwwServiceConfigurationSection m_configuration;

        /// <summary>
        /// Default ctor with no configuration
        /// </summary>
        public WebCachingBehavior() : this(null)
        {

        }

        /// <summary>
        /// Ctor with configuration
        /// </summary>
        /// <remarks>Allows the configuraiton subsystem to control caching on specified resources</remarks>
        /// <param name="configurationElement">The configuration element</param>
        public WebCachingBehavior(XElement configurationElement)
        {
            if (configurationElement == null)
            {
                this.m_cacheExtensions = new string[] { ".css", ".js", ".json", ".png", ".jpg", ".woff2", ".ttf" };
            }
            else
            {
                this.m_cacheExtensions = configurationElement.Elements((XNamespace)"http://santedb.org/configuration" + "extension").Select(o => o.Value).ToArray();
            }

            this.m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<WwwServiceConfigurationSection>();
        }

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

            var extension = Path.GetExtension(RestOperationContext.Current.IncomingRequest.Url.AbsolutePath);
            if (this.m_cacheExtensions.Contains(extension) && this.m_configuration?.AllowClientCaching != false)
            {
                response.Headers.Add("Cache-Control", "public, max-age=120, must-revalidate"); // TODO: make this configurable
                response.Headers.Add("Expires", DateTime.UtcNow.AddHours(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'"));


            }
            else
            {
                response.Headers.Add("Cache-Control", "no-cache");
            }

        }
    }
}
