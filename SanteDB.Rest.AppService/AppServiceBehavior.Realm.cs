using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Configuration;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for realm functions
    /// </summary>
    public partial class AppServiceBehavior
    {

        /// <summary>
        /// Realm settings provided by the REST API
        /// </summary>
        private class ParameterRealmSettings : IUpstreamRealmSettings
        {

            /// <summary>
            /// Constructor
            /// </summary>
            public ParameterRealmSettings(ParameterCollection parameter, String defaultClientSecret)
            {
                var uriBuilder = new UriBuilder();
                if(parameter.TryGet(RealmConfigurationFeature.REALM_NAME, out string host))
                {
                    uriBuilder.Host = host;
                }
                if(parameter.TryGet(RealmConfigurationFeature.USE_TLS, out bool useTls))
                {
                    uriBuilder.Scheme = useTls ? "https" : "http";
                }
                else
                {
                    uriBuilder.Scheme = "http";
                }
                if(parameter.TryGet(RealmConfigurationFeature.PORT_NUMBER, out int port))
                {
                    uriBuilder.Port = port;
                }
                this.Realm = uriBuilder.Uri;

                if(parameter.TryGet(RealmConfigurationFeature.DEVICE_NAME, out string deviceName))
                {
                    this.LocalDeviceName = deviceName;
                }
                else
                {
                    throw new ArgumentNullException(RealmConfigurationFeature.DEVICE_NAME);
                }

                if(parameter.TryGet(RealmConfigurationFeature.CLIENT_NAME, out string clientName))
                {
                    this.LocalClientName = clientName;
                }
                else
                {
                    throw new ArgumentNullException(RealmConfigurationFeature.CLIENT_NAME);
                }

                if(parameter.TryGet(RealmConfigurationFeature.CLIENT_SECRET, out string clientSecret))
                {
                    this.LocalClientSecret = clientSecret;
                }
                else
                {
                    this.LocalClientSecret = defaultClientSecret;
                }
            }
            /// <inheritdoc/>
            public Uri Realm { get; }

            /// <inheritdoc/>
            public string LocalDeviceName { get; }

            /// <inheritdoc/>
            public string LocalClientName { get; }

            /// <inheritdoc/>
            public string LocalClientSecret { get; }
        }

        /// <inheritdoc/>
        public ParameterCollection JoinRealm(ParameterCollection parameters)
        {
            try
            {
                if(this.m_upstreamManagementService.IsConfigured())
                {
                    this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction);
                }

                var defaultClientSettings = this.m_configurationManager.GetSection<UpstreamConfigurationSection>().Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Application);
                var settings = new ParameterRealmSettings(parameters, defaultClientSettings.CredentialSecret);

                _ = parameters.TryGet(RealmConfigurationFeature.OVERRIDE_NAME, out bool overwrite);
                this.m_upstreamManagementService.Join(settings, overwrite, out var welcomeMessage);
                parameters.Set("joined", true);
                parameters.Set("welcome", welcomeMessage);
                return parameters;
            }
            catch(UpstreamIntegrationException e)
            {
                this.m_tracer.TraceError("Error joining realm: {0}", e);
                throw;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error joining realm: {0}", e);
                throw new UpstreamIntegrationException(this.m_localizationService.GetString(ErrorMessageStrings.UPSTREAM_JOIN_ERR), e);
            }

        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction)]
        public ParameterCollection UnJoinRealm(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction)]
        public ParameterCollection PerformUpdate(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }
    }
}
