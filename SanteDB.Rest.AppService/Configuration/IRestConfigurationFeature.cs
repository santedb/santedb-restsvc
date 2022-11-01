using System;
using System.Collections.Generic;
using System.Text;
using SanteDB.Core.Configuration;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Implementers of this class can disclose and update the <see cref="SanteDBConfiguration"/>. The 
    /// use of this class is to separate the steps of configuration with the 
    /// </summary>
    public interface IRestConfigurationFeature
    {

        /// <summary>
        /// Get the preferred order for the configuration
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets the name of the feature
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the configuration object
        /// </summary>
        RestConfigurationDictionary<String, Object> Configuration { get; }

        /// <summary>
        /// Configure this feature with the specified <paramref name="featureConfiguration"/>
        /// </summary>
        /// <param name="configuration">The configuration to which the configuration option is a target</param>
        /// <param name="featureConfiguration">The feature conifguration provided by the user</param>
        /// <returns>True if the configuraiton was successful</returns>
        bool Configure(SanteDBConfiguration configuration, IDictionary<String, Object> featureConfiguration);
    }
}
