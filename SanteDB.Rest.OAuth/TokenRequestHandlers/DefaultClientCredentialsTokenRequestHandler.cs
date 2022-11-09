using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.OAuth.Abstractions;
using SanteDB.Rest.OAuth.Model;
using System.Collections.Generic;

namespace SanteDB.Rest.OAuth.TokenRequestHandlers
{
    /// <summary>
    /// Handles client_credentials grants from the token endpoint. This will process requests for applications when a user is not present.
    /// </summary>
    public class DefaultClientCredentialsTokenRequestHandler : ITokenRequestHandler
    {
        readonly Tracer _Tracer;
        readonly IPolicyEnforcementService _PolicyEnforcementService;

        /// <summary>
        /// Creates a new instance of the handler.
        /// </summary>
        /// <param name="policyEnforcementService">The policy enforcement service to use for demanding permission grants.</param>
        public DefaultClientCredentialsTokenRequestHandler(IPolicyEnforcementService policyEnforcementService)
        {
            _Tracer = new Tracer(nameof(DefaultClientCredentialsTokenRequestHandler));
            _PolicyEnforcementService = policyEnforcementService;

            if (null == _PolicyEnforcementService)
            {
                _Tracer.TraceWarning("No policy enforcement service is defined and no policy validation will take place.");
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> SupportedGrantTypes => new[] { OAuthConstants.GrantNameClientCredentials };

        /// <inheritdoc />
        public string ServiceName => "Default Client Credentials Token Request Handler";

        /// <inheritdoc />
        public bool HandleRequest(OAuthTokenRequestContext context)
        {
            if (string.IsNullOrEmpty(context?.ClientId))
            {
                _Tracer.TraceInfo("{0}: Missing client id in Token request.", context.IncomingRequest.RequestTraceIdentifier);
                context.ErrorType = OAuthErrorType.invalid_grant;
                context.ErrorMessage = "invalid client_id";
                return false;
            }

            if (null == context.ApplicationPrincipal)
            {
                _Tracer.TraceInfo("{0}: Wrong or missing client secret in Token request.", context.IncomingRequest.RequestTraceIdentifier);
                context.ErrorType = OAuthErrorType.invalid_client;
                context.ErrorMessage = "invalid client_secret";
                return false;
            }

            _Tracer.TracePolicyDemand(context.IncomingRequest.RequestTraceIdentifier, OAuthConstants.OAuthClientCredentialFlowPolicy, context.ApplicationPrincipal);
            _PolicyEnforcementService?.Demand(OAuthConstants.OAuthClientCredentialFlowPolicy, context.ApplicationPrincipal);

            if (null != context.DevicePrincipal)
            {
                _Tracer.TracePolicyDemand(context.IncomingRequest.RequestTraceIdentifier, OAuthConstants.OAuthClientCredentialFlowPolicy, context.DevicePrincipal);
                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthClientCredentialFlowPolicy, context.DevicePrincipal);
            }
            else
            {
                _Tracer.TracePolicyDemand(context.IncomingRequest.RequestTraceIdentifier, OAuthConstants.OAuthClientCredentialFlowPolicyWithoutDevice, context.ApplicationPrincipal);
                _PolicyEnforcementService?.Demand(OAuthConstants.OAuthClientCredentialFlowPolicyWithoutDevice, context.ApplicationPrincipal);
            }

            if (null == context.DevicePrincipal && context.Configuration?.AllowClientOnlyGrant != true)
            {
                _Tracer.TraceError("{0} No device principal was authenticated and AllowClientOnlyGrant is not enabled.", context.IncomingRequest.RequestTraceIdentifier);
                context.ErrorType = OAuthErrorType.unauthorized_client;
                context.ErrorMessage = $"{OAuthConstants.GrantNameClientCredentials} grant type requires device authentication either using X509 or X-Device-Authorization or enabling the DeviceAuthorizationAccessBehavior in the configuration.";
                return false;
            }

            _Tracer.TraceVerbose("{0}: Will issue a token.");
            context.Session = null; //Setting this to null will let the OAuthTokenBehavior establish the session for us.
            return true;
        }
    }
}
