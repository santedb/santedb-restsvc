/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 */
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Configuration;
using SanteDB.Rest.Common.Attributes;
using System;

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
                if (parameter.TryGet(RealmConfigurationFeature.REALM_NAME, out string host))
                {
                    uriBuilder.Host = host;
                }
                if (parameter.TryGet(RealmConfigurationFeature.USE_TLS, out bool useTls))
                {
                    uriBuilder.Scheme = useTls ? "https" : "http";
                }
                else
                {
                    uriBuilder.Scheme = "http";
                }
                if (parameter.TryGet(RealmConfigurationFeature.PORT_NUMBER, out int port))
                {
                    uriBuilder.Port = port;
                }
                this.Realm = uriBuilder.Uri;

                if (parameter.TryGet(RealmConfigurationFeature.DEVICE_NAME, out string deviceName))
                {
                    this.LocalDeviceName = deviceName;
                }
                else
                {
                    throw new ArgumentNullException(RealmConfigurationFeature.DEVICE_NAME);
                }

                if (parameter.TryGet(RealmConfigurationFeature.CLIENT_NAME, out string clientName))
                {
                    this.LocalClientName = clientName;
                }
                else
                {
                    throw new ArgumentNullException(RealmConfigurationFeature.CLIENT_NAME);
                }

                if (parameter.TryGet(RealmConfigurationFeature.CLIENT_SECRET, out string clientSecret))
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

            /// <inheritdoc/>
            public Guid LocalDeviceSid => Guid.Empty;
        }

        /// <inheritdoc/>
        public ParameterCollection JoinRealm(ParameterCollection parameters)
        {
            try
            {
                if (this.m_upstreamManagementService.IsConfigured())
                {
                    this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration);
                }

                var defaultClientSettings = this.m_configurationManager.GetSection<UpstreamConfigurationSection>().Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Application);
                var settings = new ParameterRealmSettings(parameters, defaultClientSettings.CredentialSecret);

                _ = parameters.TryGet(RealmConfigurationFeature.OVERRIDE_NAME, out bool overwrite);
                this.m_upstreamManagementService.Join(settings, overwrite, out var welcomeMessage);
                parameters.Set("joined", true);
                parameters.Set("welcome", welcomeMessage);
                return parameters;
            }
            catch (UpstreamIntegrationException e)
            {
                this.m_tracer.TraceError("{0}: {1}", e.Message, e.InnerException?.Message);
                throw;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error joining realm: {0}", e.Message);
                throw new UpstreamIntegrationException(this.m_localizationService.GetString(ErrorMessageStrings.UPSTREAM_JOIN_ERR), e);
            }

        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public ParameterCollection UnJoinRealm(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public ParameterCollection PerformUpdate(ParameterCollection parameters)
        {
            _ = parameters.TryGet("_apply", out bool apply);
            this.m_updateManager.Update(apply);
            return null;
        }
    }
}
