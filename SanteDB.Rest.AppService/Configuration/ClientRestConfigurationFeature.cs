using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Http;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Client connectivity configuration feature
    /// </summary>
    public class ClientRestConfigurationFeature : IRestConfigurationFeature
    {
        public const string REST_CLIENT_SETTING = "clients";
        public const string REST_CLIENT_OPTIMIZE_SETTING = "optimize";
        public const string REST_CLIENT_OPTIMIZE_REQ_SETTING = "optimizeReq";
        public const string REST_CLIENT_CERT_SETTING = "clientCertificate";
        private readonly RestClientConfigurationSection m_configuration;
        
        /// <summary>
        /// Rest constructor
        /// </summary>
        public ClientRestConfigurationFeature(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<RestClientConfigurationSection>();
        }

        /// <inheritdoc/>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "client";

        /// <inheritdoc/>
        public RestConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <summary>
        /// Regenerate or refresh the configuration
        /// </summary>
        private RestConfigurationDictionary<string, object> GetConfiguration() => new RestConfigurationDictionary<string, object>()
        {
            { REST_CLIENT_SETTING, this.m_configuration?.Client?.Select(c => new Dictionary<String, Object>() {
                { REST_CLIENT_SETTING,  c.Name },
                { REST_CLIENT_OPTIMIZE_SETTING, c.Binding.OptimizationMethod },
                { REST_CLIENT_OPTIMIZE_REQ_SETTING, c.Binding.CompressRequests },
                { REST_CLIENT_CERT_SETTING, c.Binding.Security.ClientCertificate }
            }).ToArray() },
            { REST_CLIENT_OPTIMIZE_SETTING, this.m_configuration.Client.Any() ? this.m_configuration.Client.Max(o=>o.Binding.OptimizationMethod) : Core.Http.Description.HttpCompressionAlgorithm.None },
            { REST_CLIENT_CERT_SETTING, this.m_configuration.Client?.Select(o=>o.Binding.Security.ClientCertificate).FirstOrDefault() }
        };

        /// <inheritdoc/>
        public string ReadPolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public string WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            throw new NotImplementedException();
        }
    }
}
