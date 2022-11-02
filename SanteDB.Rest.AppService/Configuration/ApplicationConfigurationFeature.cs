using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// The application service configuration feature
    /// </summary>
    public class ApplicationConfigurationFeature : IRestConfigurationFeature
    {

        public const string SERVICES_SETTING = "service";
        public const string APPSETTING_SETTING = "setting";
        public const string INSTANCE_NAME_SETTING = "instance";

        /// <summary>
        /// DI constructor
        /// </summary>
        public ApplicationConfigurationFeature(IConfigurationManager configurationManager)
        {
            var section = configurationManager.GetSection<ApplicationServiceContextConfigurationSection>();
            this.Configuration = new RestConfigurationDictionary<string, object>()
            {
                { SERVICES_SETTING, section.ServiceProviders.Select(o=> o.TypeXml).ToArray() },
                { APPSETTING_SETTING, section.AppSettings },
                { INSTANCE_NAME_SETTING, section.InstanceName }
            };
        }

        /// <inheritdoc/>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "application";

        /// <inheritdoc/>
        public RestConfigurationDictionary<string, object> Configuration { get; }

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            if(section == null)
            {
                section = new ApplicationServiceContextConfigurationSection()
                {
                    ServiceProviders = new List<TypeReferenceConfiguration>(),
                    AppSettings = new List<AppSettingKeyValuePair>()
                };
                configuration.AddSection(section);
            }

            section.AppSettings = ((IDictionary<String, Object>)featureConfiguration[APPSETTING_SETTING])?.Select(o => new AppSettingKeyValuePair(o.Key, o.Value?.ToString())).ToList();
            section.ServiceProviders = ((IEnumerable)featureConfiguration[SERVICES_SETTING])?.OfType<String>().Select(o => new TypeReferenceConfiguration(o)).ToList();
            return true;

        }
    }
}
