using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Solution configuration feature
    /// </summary>
    public class SolutionConfigurationFeature : IRestConfigurationFeature
    {

        /// <summary>
        /// Solution name configuration setting
        /// </summary>
        public const string SOLUTION_NAME_CONFIG = "solution";
        /// <summary>
        /// Auto update configuration
        /// </summary>
        public const string AUTO_UPDATE_CONFG = "autoUpdate";

        /// <summary>
        /// DI constructor
        /// </summary>
        public SolutionConfigurationFeature(IConfigurationManager configurationManager)
        {
            var config = configurationManager.GetSection<ClientConfigurationSection>();
            this.Configuration = new RestConfigurationDictionary<string, object>()
            {
                { SOLUTION_NAME_CONFIG, config?.UiSolution },
                { AUTO_UPDATE_CONFG, config?.AutoUpdateApplets }
            };

        }

        /// <summary>
        /// Gets the order in the configuration
        /// </summary>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "applet";

        /// <inheritdoc/>
        public RestConfigurationDictionary<string, object> Configuration { get; }

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
