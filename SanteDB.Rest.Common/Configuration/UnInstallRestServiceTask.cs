using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common.Configuration
{
    /// <summary>
    /// Remove REST service into the configuration section
    /// </summary>
    public class UnInstallRestServiceTask : IConfigurationTask
    {
        // Configuration
        private RestServiceConfiguration m_configuration;

        private Func<bool> m_queryValidateFunc;

        /// <summary>
        /// Remove rest service task
        /// </summary>
        public UnInstallRestServiceTask(IFeature owner, RestServiceConfiguration configuration, Func<bool> queryValidateFunc)
        {
            this.Feature = owner;
            this.m_configuration = configuration;
            this.m_queryValidateFunc = queryValidateFunc;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Removes the {this.m_configuration.Name} REST service at {this.m_configuration.Endpoints[0].Address}";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => $"Remove {this.m_configuration.Name} REST API";

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