﻿using Newtonsoft.Json.Linq;
using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
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
    public class ApplicationConfigurationFeature : IClientConfigurationFeature
    {

        public const string SERVICES_SETTING = "service";
        public const string APPSETTING_SETTING = "setting";
        public const string INSTANCE_NAME_SETTING = "instance";
        private readonly ApplicationServiceContextConfigurationSection m_configurationSection;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ApplicationConfigurationFeature(IConfigurationManager configurationManager)
        {
            this.m_configurationSection = configurationManager.GetSection<ApplicationServiceContextConfigurationSection>();
            
        }

        /// <inheritdoc/>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "application";

        /// <inheritdoc/>
        public ConfigurationDictionary<string, object> Configuration => this.Refresh();

        /// <inheritdoc/>
        public String ReadPolicy => PermissionPolicyIdentifiers.Login;

        /// <inheritdoc/>
        public String WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <summary>
        /// Refresh the configuration
        /// </summary>
        private ConfigurationDictionary<string, object> Refresh() => new ConfigurationDictionary<string, object>()
            {
                { SERVICES_SETTING, m_configurationSection.ServiceProviders.Select(o=> o.TypeXml).ToArray() },
                { APPSETTING_SETTING, m_configurationSection.AppSettings },
                { INSTANCE_NAME_SETTING, m_configurationSection.InstanceName }
            };

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

            section.AppSettings = ((IEnumerable)featureConfiguration[APPSETTING_SETTING])?.OfType<JObject>().Select(o => new AppSettingKeyValuePair(o["key"].ToString(), o["value"]?.ToString())).ToList();
            //section.ServiceProviders = ((IEnumerable)featureConfiguration[SERVICES_SETTING])?.OfType<JObject>().Select(o => new TypeReferenceConfiguration(o["type"].ToString())).ToList();
            return true;

        }
    }
}