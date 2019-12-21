using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// REST service constants
    /// </summary>
    internal class SanteDBRestConstants
    {

        // Client claim header
        internal const string BasicHttpClientClaimHeaderName = "X-SanteDBClient-Claim";
        // Client auth header
        internal const string BasicHttpClientCredentialHeaderName = "X-SanteDBClient-Authorization";
        // Device authorization
        internal const string HttpDeviceCredentialHeaderName = "X-Device-Authorization";
    }
}
