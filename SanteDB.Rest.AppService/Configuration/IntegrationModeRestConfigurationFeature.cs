using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Integration mode rest configuration
    /// </summary>
    public class IntegrationModeRestConfigurationFeature : IClientConfigurationFeature
    {

        /// <summary>
        /// Get the mode of configuration
        /// </summary>
        public const string MODE_SETTING = "mode";

        // Integration patterns
        private IEnumerable<IUpstreamIntegrationPattern> m_integrationPatterns;
        private readonly ApplicationServiceContextConfigurationSection m_appConfiguration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public IntegrationModeRestConfigurationFeature(IServiceManager serviceManager, IConfigurationManager configurationManager)
        {
            this.m_integrationPatterns = serviceManager.CreateInjectedOfAll<IUpstreamIntegrationPattern>();
            this.m_appConfiguration = configurationManager.GetSection<ApplicationServiceContextConfigurationSection>();
        }

        /// <summary>
        /// The order of this configuration being applied
        /// </summary>
        public int Order => 0;

        /// <summary>
        /// Get the name
        /// </summary>
        public string Name => "integration";

        /// <summary>
        /// Gets the configuration
        /// </summary>
        public ConfigurationDictionary<string, object> Configuration => new ConfigurationDictionary<string, object>()
        {
            { MODE_SETTING, this.m_integrationPatterns.FirstOrDefault(p=>p.GetServices().All(s=>this.m_appConfiguration.ServiceProviders.Any(c=>c.Type == s)))?.Name }
        };

        /// <summary>
        /// Read cofiguration policy
        /// </summary>
        public string ReadPolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <summary>
        /// Write policy
        /// </summary>
        public string WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <summary>
        /// Configure the section
        /// </summary>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var appSetting = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            if(featureConfiguration.TryGetValue(MODE_SETTING, out var modeRaw))
            {
                // Remove old services
                var oldMode = this.m_integrationPatterns.FirstOrDefault(o => o.Name == this.Configuration[MODE_SETTING]?.ToString())?.GetServices();
                if (oldMode != null) {
                    appSetting.ServiceProviders.RemoveAll(s => oldMode.Contains(s.Type));
                }
                var newMode = this.m_integrationPatterns.First(o => o.Name == modeRaw.ToString())?.GetServices();
                if(newMode == null)
                {
                    throw new InvalidOperationException(String.Format(ErrorMessages.TYPE_NOT_FOUND, modeRaw));
                }
                else
                {
                    appSetting.ServiceProviders.AddRange(newMode.Select(o => new TypeReferenceConfiguration(o)));
                }
            }
            return true;
        }
    }
}
