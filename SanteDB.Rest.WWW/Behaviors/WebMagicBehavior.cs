using RestSrvr;
using RestSrvr.Exceptions;
using RestSrvr.Message;
using SanteDB.Core.Diagnostics;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.WWW.Behaviors
{
    /// <summary>
    /// The web-magic behavior is used to ensure that the X-SanteDB-Magic header matches the magic number 
    /// generated on startup. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is used on protected contexts (Android Client, Windows Client, Linux Client) where only the host process (such as a chrome web view, 
    /// or CEF view) can access the UI. The application process injects the header into all HTTP requests and this is used to ensure that only 
    /// that process is accessing data</para>
    /// <para>The data is passed in the HTTP header <c>X-SanteDB-Magic</c> or in the UserAgent of <c>SanteDB-MAGIC</c></para>
    /// </remarks>
    public class WebMagicBehavior : IServiceBehavior, IServicePolicy
    {

        // Web magic behavior
        private Tracer m_tracer = Tracer.GetTracer(typeof(WebMagicBehavior));

        /// <summary>
        /// Get the magic of this service
        /// </summary>
        public static byte[] Magic { get; private set; }

        /// <summary>
        /// Set the magic for this class
        /// </summary>
        static WebMagicBehavior()
        {
            Magic = Guid.NewGuid().ToByteArray();
        }

        /// <inheritdoc cref="IServicePolicy.Apply(RestRequestMessage)"/>
        public void Apply(RestRequestMessage request)
        {
            if(!request.Headers.TryGetValue(ExtendedHttpHeaderNames.ClientMagicNumberHeaderName, out var magicHeader) && magicHeader.Contains(Magic.HexEncode()) ||
                $"SanteDB-{Magic.HexEncode()}".Equals(request.UserAgent))
            {
                
            }
            else
            {
                RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
                throw new FaultException<Stream>(System.Net.HttpStatusCode.Forbidden, new GZipStream(typeof(WebMagicBehavior).Assembly.GetManifestResourceStream("SanteDB.Rest.WWW.Resources.antihaxor"), CompressionMode.Decompress));
            }
        }

        /// <inheritdoc cref="IServiceBehavior.ApplyServiceBehavior(RestService, ServiceDispatcher)"/>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.AddServiceDispatcherPolicy(this);
        }
    }
}
