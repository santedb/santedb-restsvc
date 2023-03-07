using Newtonsoft.Json.Linq;
using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Http;
using SanteDB.Core.Http;
using SanteDB.Core.Http.Description;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Client connectivity configuration feature
    /// </summary>
    public class ClientRestConfigurationFeature : IClientConfigurationFeature
    {
        public const string REST_CLIENT_SETTING = "clients";
        public const string REST_CLIENT_OPTIMIZE_SETTING = "optimize";
        public const string REST_CLIENT_OPTIMIZE_REQ_SETTING = "optimizeReq";
        public const string REST_CLIENT_CERT_SETTING = "clientCertificate";
        public const string REST_CLIENT_PROXY_SETTING = "proxyAddress";
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
        public ConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <summary>
        /// Regenerate or refresh the configuration
        /// </summary>
        private ConfigurationDictionary<string, object> GetConfiguration() => new ConfigurationDictionary<string, object>()
        {
            { REST_CLIENT_SETTING, this.m_configuration?.Client?.Select(c => new Dictionary<String, Object>() {
                { REST_CLIENT_SETTING,  c.Name },
                { REST_CLIENT_OPTIMIZE_SETTING, c.Binding.OptimizationMethod },
                { REST_CLIENT_OPTIMIZE_REQ_SETTING, c.Binding.CompressRequests },
                { REST_CLIENT_CERT_SETTING, c.Binding.Security.ClientCertificate }
            }).ToArray() },
            { REST_CLIENT_PROXY_SETTING, this.m_configuration?.ProxyAddress },
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
            var section = configuration.GetSection<RestClientConfigurationSection>();
            if(section == null)
            {
                section = new RestClientConfigurationSection()
                {
                    RestClientType = new TypeReferenceConfiguration( typeof(RestClient))
                };
            }

            section.ProxyAddress = featureConfiguration[REST_CLIENT_PROXY_SETTING]?.ToString() ?? section.ProxyAddress;
            foreach(var client in (JArray)featureConfiguration[REST_CLIENT_SETTING])
            {
                var localClient = section.Client.Find(o => o.Name == client[REST_CLIENT_SETTING].ToString());
                if(localClient == null) { continue; }
                if (Enum.TryParse<HttpCompressionAlgorithm>(client[REST_CLIENT_OPTIMIZE_SETTING].ToString(), out var optimize))
                {
                    localClient.Binding.OptimizationMethod = optimize;
                    localClient.Binding.CompressRequests = (bool)client[REST_CLIENT_OPTIMIZE_REQ_SETTING];
                }
                if (!String.IsNullOrEmpty(client[REST_CLIENT_CERT_SETTING]?.ToString()))
                {
                    localClient.Binding.Security.ClientCertificate = new Core.Security.Configuration.X509ConfigurationElement(StoreLocation.CurrentUser, StoreName.My, X509FindType.FindByThumbprint, client[REST_CLIENT_CERT_SETTING].ToString());
                }
                else
                {
                    localClient.Binding.Security.ClientCertificate = null;
                }
            }
            return true;
        }
    }
}
