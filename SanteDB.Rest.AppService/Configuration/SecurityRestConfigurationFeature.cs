using Newtonsoft.Json.Linq;
using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Configuration feature for security configuration
    /// </summary>
    public class SecurityRestConfigurationFeature : IClientConfigurationFeature
    {
        private readonly SecurityConfigurationSection m_configurationSection;
        private readonly IRestClientFactory m_restClientFactory;
        public const string AUDIT_RETENTION_SETTING = "auditRetention";
        public const string ASSIGNED_FACILITY_SETTING = "facility";
        public const string ASSIGNED_OWNER_SETTING = "owner";
        public const string RESTRICT_LOGIN_POLICY_SETTING = "restrictLogin";
        public const string ALLOW_OFFLINE_LOGIN_SETTING = "allowOffline";
        public const string SECURITY_SIGN_KEYS_SETTING = "signingKeys";
        public const string HS256_MASK = "XXXX";

        /// <summary>
        /// DI constructor
        /// </summary>
        public SecurityRestConfigurationFeature(IConfigurationManager configurationManager, IRestClientFactory restClientFactory)
        {
            this.m_configurationSection = configurationManager.GetSection<SecurityConfigurationSection>();
            this.m_restClientFactory = restClientFactory;
        }

        /// <inheritdoc/>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "security";

        /// <inheritdoc/>
        public ConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <summary>
        /// Get configuration
        /// </summary>
        private ConfigurationDictionary<string, object> GetConfiguration() =>
            new ConfigurationDictionary<string, object>()
            {
                { AUDIT_RETENTION_SETTING, this.m_configurationSection.GetSecurityPolicy(SecurityPolicyIdentification.AuditRetentionTime, new TimeSpan(30, 0, 0, 0)) },
                { ALLOW_OFFLINE_LOGIN_SETTING, this.m_configurationSection.GetSecurityPolicy(SecurityPolicyIdentification.AllowCachingOfUserCredentials, true) },
                { RESTRICT_LOGIN_POLICY_SETTING, this.m_configurationSection.GetSecurityPolicy(SecurityPolicyIdentification.AllowNonAssignedUsersToLogin, false) },
                { ASSIGNED_FACILITY_SETTING, this.m_configurationSection.GetSecurityPolicy<Guid?>(SecurityPolicyIdentification.AssignedFacilityUuid, null) },
                { ASSIGNED_OWNER_SETTING, this.m_configurationSection.GetSecurityPolicy<Guid?>(SecurityPolicyIdentification.AssignedOwnerUuid, null) },
                { SECURITY_SIGN_KEYS_SETTING, this.m_configurationSection?.Signatures?.Select(o=> new Dictionary<String, Object>()
                {
                    {  "name", o.KeyName },
                    { "type", o.Algorithm },
                    { "value", o.Algorithm == SignatureAlgorithm.HS256 ? HS256_MASK : o.FindValue }
                }).ToArray()
                }
            };


        /// <inheritdoc/>
        public string ReadPolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public string WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<SecurityConfigurationSection>();
            if (section == null)
            {
                section = new SecurityConfigurationSection()
                {
                    PasswordRegex = @"^(?=.*\d){1,}(?=.*[a-z]){1,}(?=.*[A-Z]){1,}(?=.*[^\w\d]){1,}.{6,}$",
                    SecurityPolicy = new List<SecurityPolicyConfiguration>()
                    {
                        new SecurityPolicyConfiguration(SecurityPolicyIdentification.SessionLength, new TimeSpan(0,30,0)),
                        new SecurityPolicyConfiguration(SecurityPolicyIdentification.RefreshLength, new TimeSpan(0,35,0))
                    }
                };
            }


            if (featureConfiguration.TryGetValue(AUDIT_RETENTION_SETTING, out var auditRetentionRaw) && TimeSpan.TryParse(auditRetentionRaw.ToString(), out var retention))
            {
                section.SetPolicy(SecurityPolicyIdentification.AuditRetentionTime, retention);
            }
            if (featureConfiguration.TryGetValue(ALLOW_OFFLINE_LOGIN_SETTING, out var offlineLoginSettingRaw) && Boolean.TryParse(offlineLoginSettingRaw.ToString(), out var offlineLoginSetting))
            {
                section.SetPolicy(SecurityPolicyIdentification.AllowCachingOfUserCredentials, offlineLoginSettingRaw);
            }

            // HACK: Find a better way
            if (featureConfiguration.TryGetValue(ASSIGNED_FACILITY_SETTING, out var assignedFacilityRaw) && Guid.TryParse(assignedFacilityRaw?.ToString(), out var assignedFacility))
            {
                section.SetPolicy(SecurityPolicyIdentification.AssignedFacilityUuid, assignedFacility);
                using(var client = this.m_restClientFactory.GetRestClientFor(Core.Interop.ServiceEndpointType.HealthDataService))
                {
                    client.Post<EntityRelationship, EntityRelationship>($"EntityRelationship", new EntityRelationship()
                    {
                        SourceEntityKey = section.GetSecurityPolicy<Guid>(SecurityPolicyIdentification.AssignedDeviceEntityId),
                        TargetEntityKey = assignedFacility,
                        RelationshipTypeKey = EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation
                    });
                }
            }
            if (featureConfiguration.TryGetValue(ASSIGNED_OWNER_SETTING, out var assignedOwnerRaw) && Guid.TryParse(assignedOwnerRaw?.ToString(), out var assignedOwner))
            {
                section.SetPolicy(SecurityPolicyIdentification.AssignedOwnerUuid, assignedOwner);
                using (var client = this.m_restClientFactory.GetRestClientFor(Core.Interop.ServiceEndpointType.HealthDataService))
                {
                    client.Post<EntityRelationship, EntityRelationship>($"EntityRelationship", new EntityRelationship()
                    {
                        SourceEntityKey = section.GetSecurityPolicy<Guid>(SecurityPolicyIdentification.AssignedDeviceEntityId),
                        TargetEntityKey = assignedOwner,
                        RelationshipTypeKey = EntityRelationshipTypeKeys.AssignedEntity
                    });
                }
            }
            if(featureConfiguration.TryGetValue(RESTRICT_LOGIN_POLICY_SETTING, out var restrictLoginSettingRaw) && Boolean.TryParse(restrictLoginSettingRaw.ToString(), out var restrictLoginSetting))
            {
                section.SetPolicy(SecurityPolicyIdentification.AllowNonAssignedUsersToLogin, restrictLoginSetting);
            }

            // security signing keys
            foreach (JObject itm in (JArray)featureConfiguration[SECURITY_SIGN_KEYS_SETTING])
            {
                var existingKey = section.Signatures?.Find(o => o.KeyName == itm["name"].ToString());
                if(existingKey == null)
                {
                    existingKey = new SecuritySignatureConfiguration()
                    {
                        KeyName = itm["name"].ToString()
                    };
                }

                if (Enum.TryParse<SignatureAlgorithm>(itm["type"].ToString(), out var algorithm))
                {
                    existingKey.Algorithm = algorithm;
                }

                if(existingKey.Algorithm == SignatureAlgorithm.HS256 && !HS256_MASK.Equals(itm["value"].ToString()))
                {
                    existingKey.HmacSecret = itm["value"].ToString();
                }
                else if(existingKey.Algorithm == SignatureAlgorithm.RS256)
                {
                    existingKey.StoreLocation = System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser;
                    existingKey.StoreName = System.Security.Cryptography.X509Certificates.StoreName.My;
                    existingKey.FindType = System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint;
                    existingKey.StoreLocationSpecified = existingKey.StoreNameSpecified = existingKey.FindTypeSpecified = true;
                    existingKey.FindValue = itm["value"].ToString();
                }
            }
            return true;
        }
    }
}
