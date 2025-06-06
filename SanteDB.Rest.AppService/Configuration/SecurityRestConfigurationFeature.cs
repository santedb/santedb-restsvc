﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using Newtonsoft.Json.Linq;
using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Configuration feature for security configuration
    /// </summary>
    public class SecurityRestConfigurationFeature : IClientConfigurationFeature
    {
        private readonly SecurityConfigurationSection m_configurationSection;
        private readonly IRestClientFactory m_restClientFactory;
        /// <summary>
        /// The name of the audit retention setting in the configuration dictionary
        /// </summary>
        public const string AUDIT_RETENTION_SETTING = "auditRetention";
        /// <summary>
        /// The name of the subscribed/assigned facility setting in the configuraiton dictionary
        /// </summary>
        public const string ASSIGNED_FACILITY_SETTING = "facility";
        /// <summary>
        /// The name of hte device owner setting in the configuration dictionary
        /// </summary>
        public const string ASSIGNED_OWNER_SETTING = "owner";
        /// <summary>
        /// The name of the restrict login policy setting in the configuration dictionary
        /// </summary>
        public const string RESTRICT_LOGIN_POLICY_SETTING = "restrictLogin";
        /// <summary>
        /// The name of the allow offline login configuration dictionary setting
        /// </summary>
        public const string ALLOW_OFFLINE_LOGIN_SETTING = "allowOffline";
        /// <summary>
        /// The name of the security signing keys setting
        /// </summary>
        public const string SECURITY_SIGN_KEYS_SETTING = "signingKeys";
        /// <summary>
        /// The name of the HS256 masking string
        /// </summary>
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
        public string ReadPolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public string WritePolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

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
            var deviceEntityId = section.GetSecurityPolicy<Guid>(SecurityPolicyIdentification.AssignedDeviceEntityId);
            if (featureConfiguration.TryGetValue(ASSIGNED_FACILITY_SETTING, out var assignedFacilityRaw) && Guid.TryParse(assignedFacilityRaw?.ToString(), out var assignedFacility))
            {
                section.SetPolicy(SecurityPolicyIdentification.AssignedFacilityUuid, assignedFacility);
                using (var client = this.m_restClientFactory.GetRestClientFor(Core.Interop.ServiceEndpointType.HealthDataService))
                {
                    var existingAssignedFacilty = client.Get<Bundle>(typeof(EntityRelationship).GetSerializationName(), $"source={deviceEntityId}&relationshipType={EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation}&_includeTotal=false".ParseQueryString());

                    foreach(var itm in existingAssignedFacilty.Item)
                    {
                        client.Delete<EntityRelationship>($"{typeof(EntityRelationship).GetSerializationName()}/{itm.Key}");
                    }

                    client.Post<EntityRelationship, EntityRelationship>(typeof(EntityRelationship).GetSerializationName(), new EntityRelationship()
                    {
                        SourceEntityKey = deviceEntityId,
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
                    var existingAssignedFacilty = client.Get<Bundle>(typeof(EntityRelationship).GetSerializationName(), $"source={deviceEntityId}&relationshipType={EntityRelationshipTypeKeys.AssignedEntity}&_includeTotal=false".ParseQueryString());

                    foreach (var itm in existingAssignedFacilty.Item)
                    {
                        client.Delete<EntityRelationship>($"{typeof(EntityRelationship).GetSerializationName()}/{itm.Key}");
                    }


                    client.Post<EntityRelationship, EntityRelationship>(typeof(EntityRelationship).GetSerializationName(), new EntityRelationship()
                    {
                        SourceEntityKey = deviceEntityId,
                        TargetEntityKey = assignedOwner,
                        RelationshipTypeKey = EntityRelationshipTypeKeys.AssignedEntity
                    });
                }
            }
            if (featureConfiguration.TryGetValue(RESTRICT_LOGIN_POLICY_SETTING, out var restrictLoginSettingRaw) && Boolean.TryParse(restrictLoginSettingRaw.ToString(), out var restrictLoginSetting))
            {
                section.SetPolicy(SecurityPolicyIdentification.AllowNonAssignedUsersToLogin, restrictLoginSetting);
            }

            // security signing keys
            foreach (JObject itm in (JArray)featureConfiguration[SECURITY_SIGN_KEYS_SETTING])
            {
                var existingKey = section.Signatures?.Find(o => o.KeyName == itm["name"].ToString());
                if (existingKey == null)
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

                if (existingKey.Algorithm == SignatureAlgorithm.HS256 && !HS256_MASK.Equals(itm["value"].ToString()))
                {
                    existingKey.HmacSecret = itm["value"].ToString();
                    existingKey.StoreLocationSpecified = false;
                    existingKey.FindTypeSpecified = false;
                    existingKey.FindValue = null;
                }
                else if (existingKey.Algorithm == SignatureAlgorithm.RS256)
                {
                    existingKey.HmacSecret = null;
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
