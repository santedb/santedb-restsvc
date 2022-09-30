/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 * User: fyfej
 * Date: 2022-5-30
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;

namespace SanteDB.Rest.Common.Configuration
{
    /// <summary>
    /// Remove REST service into the configuration section
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
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
        public string Description => $"Removes the {this.m_configuration.ConfigurationName} REST service at {this.m_configuration.Endpoints[0].Address}";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => $"Remove {this.m_configuration.ConfigurationName} REST API";

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

            restSection.Services.RemoveAll(o => o.ConfigurationName == this.m_configuration.ConfigurationName);
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