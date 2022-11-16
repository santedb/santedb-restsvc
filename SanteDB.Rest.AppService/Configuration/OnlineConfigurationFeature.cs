using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Rest.AMI.Configuration;
using SanteDB.Rest.BIS.Configuration;
using SanteDB.Rest.HDSI.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Online configuration feature
    /// </summary>
    public class OnlineConfigurationFeature : IClientConfigurationFeature
    {
        /// <summary>
        /// Get the ordering of this feature
        /// </summary>
        public int Order => 10;

        /// <summary>
        /// Get the name of the configuration section
        /// </summary>
        public string Name => "online";

        /// <summary>
        /// Get the configuratoin
        /// </summary>
        public ConfigurationDictionary<string, object> Configuration => new ConfigurationDictionary<string, object>();

        /// <summary>
        /// Read policy
        /// </summary>
        public string ReadPolicy => PermissionPolicyIdentifiers.Login;

        /// <summary>
        /// Write policy
        /// </summary>
        public string WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <summary>
        /// Configure this feature
        /// </summary>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            if (configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings?.Any(p => p.Key == "integration-mode" && p.Value == "online") == true)
            {
                var amiSection = configuration.GetSection<AmiConfigurationSection>();
                var hdsiSection = configuration.GetSection<HdsiConfigurationSection>();
                var bisSection = configuration.GetSection<BisServiceConfigurationSection>();
                if(amiSection == null)
                {
                    amiSection = new AmiConfigurationSection();
                    configuration.AddSection(amiSection);
                }
                if(hdsiSection == null)
                {
                    hdsiSection = new HdsiConfigurationSection();
                    configuration.AddSection(hdsiSection);
                }
                if(bisSection == null)
                {
                    bisSection = new BisServiceConfigurationSection();
                    configuration.AddSection(bisSection);
                }

                bisSection.AutomaticallyForwardRequests = hdsiSection.AutomaticallyForwardRequests = true;
                amiSection.AutomaticallyForwardRequests = false;
            }
            return true;
        }
    }
}
