using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common.Configuration
{
    /// <summary>
    /// Install REST service into the configuration section
    /// </summary>
    public class InstallRestServiceTask : IConfigurationTask
    {
        // Configuration
        private RestServiceConfiguration m_configuration;

        private Func<bool> m_queryValidateFunc;

        /// <summary>
        /// Install rest service task
        /// </summary>
        public InstallRestServiceTask(IFeature owner, RestServiceConfiguration configuration, Func<bool> queryValidateFunc)
        {
            this.Feature = owner;
            this.m_configuration = configuration;
            this.m_queryValidateFunc = queryValidateFunc;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Installs the {this.m_configuration.Name} REST service at {this.m_configuration.Endpoints[0].Address}";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => $"Install {this.m_configuration.Name} REST API";

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the installation
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            var restSection = configuration.GetSection<RestConfigurationSection>();
            if (restSection == null)
            {
                restSection = new RestConfigurationSection();
                configuration.AddSection(restSection);
            }

            restSection.Services.RemoveAll(o => o.Name == this.m_configuration.Name);
            restSection.Services.Add(this.m_configuration);
            return true;
        }

        /// <summary>
        /// Rollback
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            return true;
        }

        /// <summary>
        /// Verify state
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration) => this.m_queryValidateFunc();
    }
}