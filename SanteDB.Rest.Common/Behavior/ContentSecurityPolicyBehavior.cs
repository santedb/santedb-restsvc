using RestSrvr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common.Behaviors
{
    /// <summary>
    /// Implements the Content-Security-Policy header
    /// </summary>
    public class ContentSecurityPolicyBehavior : IEndpointBehavior
    {
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            throw new NotImplementedException();
        }
    }
}
