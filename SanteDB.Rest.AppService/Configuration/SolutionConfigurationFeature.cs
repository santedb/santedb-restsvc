using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Solution configuration feature
    /// </summary>
    public class SolutionConfigurationFeature : IClientConfigurationFeature
    {

        /// <summary>
        /// Solution name configuration setting
        /// </summary>
        public const string SOLUTION_NAME_CONFIG = "solution";
        /// <summary>
        /// Auto update configuration
        /// </summary>
        public const string AUTO_UPDATE_CONFG = "autoUpdate";
        private readonly ClientConfigurationSection m_configuration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public SolutionConfigurationFeature(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<ClientConfigurationSection>();
        }

        /// <summary>
        /// Gets the order in the configuration
        /// </summary>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "applet";

        /// <inheritdoc/>
        public ConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <summary>
        /// Get configuration
        /// </summary>
        private ConfigurationDictionary<string, object> GetConfiguration() => new ConfigurationDictionary<string, object>()
            {
                { SOLUTION_NAME_CONFIG, this.m_configuration?.UiSolution },
                { AUTO_UPDATE_CONFG, this.m_configuration?.AutoUpdateApplets }
            };


        /// <inheritdoc/>
        public String ReadPolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public String WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<ClientConfigurationSection>();
            if(section == null)
            {
                section = new ClientConfigurationSection();
                configuration.AddSection(section);
            }
            section.AutoUpdateApplets = (bool?)featureConfiguration[AUTO_UPDATE_CONFG] ?? true;
            section.UiSolution = featureConfiguration[SOLUTION_NAME_CONFIG]?.ToString();
            return true;
        }
    }
}
