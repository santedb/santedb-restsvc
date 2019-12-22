using RestSrvr;
using RestSrvr.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SanteDB.Rest.Common.Behaviors
{
    /// <summary>
    /// Implements the Content-Security-Policy header
    /// </summary>
    public class SecurityPolicyHeadersBehavior : IEndpointBehavior, IMessageInspector
    {

        /// <summary>
        /// Gets the NONCE for this instance of the policy behavior
        /// </summary>
        public String Nonce { get; }

        /// <summary>
        /// Policy behavior configuration
        /// </summary>
        public SecurityPolicyHeadersBehavior()
        {
            this.Nonce = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "");
        }

        /// <summary>
        /// After receiving request (not applicable)
        /// </summary>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
            ;
        }

        /// <summary>
        /// Applet the content security header
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(this);
        }

        /// <summary>
        /// Before sending a response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {

            //response.Headers.Add("Content-Security-Policy", $"script-src-elem 'nonce-{this.Nonce}'; script-src 'self'");
            response.Headers.Add("Content-Security-Policy", $"script-src-elem 'self' 'nonce-{this.Nonce}' 'strict-dynamic'; script-src 'self'");
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
            response.Headers.Add("X-Frame-Options", "deny");
            response.Headers.Add("Feature-Policy", "autoplay 'none'; camera 'none'; accelerometer 'none'; goelocation 'none'; payment 'none'");

        }
    }
}
