/*
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
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Matching;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which serves out match metadata
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class MatchConfigurationResourceHandler : ChainedResourceHandlerBase, IApiResourceHandler, IOperationalApiResourceHandler, ICheckoutResourceHandler
    {
        // Configuration service
        private readonly IRecordMatchingConfigurationService m_configurationService;
        private readonly IResourceCheckoutService m_checkoutService;

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public override string ResourceName => "MatchConfiguration";

        /// <summary>
        /// Gets the type that this returns
        /// </summary>
        public override Type Type => typeof(IRecordMatchingConfiguration);

        /// <summary>
        /// Gets the scope
        /// </summary>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of this service
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Delete;

        /// <summary>
        /// Match configuration resource handler
        /// </summary>
        public MatchConfigurationResourceHandler(ILocalizationService localizationService, IResourceCheckoutService checkoutService, IRecordMatchingConfigurationService configurationService = null) :
            base(localizationService)
        {
            // TODO: Throw method not support exception if someone calls this
            this.m_configurationService = configurationService;
            this.m_checkoutService = checkoutService;
        }

        /// <summary>
        /// Create a match configuration
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is IRecordMatchingConfiguration configMatch)
            {
                return this.m_configurationService.SaveConfiguration(configMatch);
            }
            else
            {
                throw new ArgumentException("Incorrect match configuration type");
            }
        }

        /// <summary>
        /// Get the specified match configuration identifier
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Get(object id, object versionId)
        {
            return this.m_configurationService.GetConfiguration(id.ToString());
        }

        /// <summary>
        /// Delete a match configuration
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public override object Delete(object key)
        {
            return this.m_configurationService.DeleteConfiguration(key.ToString());
        }

        /// <summary>
        /// Query for match configurations
        /// </summary>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            if (queryParameters.TryGetValue("name", out var values))
            {
                return new MemoryQueryResultSet(this.m_configurationService.Configurations
                    .Where(o => o.Id.Contains(values.First().Replace("~", "")) && !o.Id.StartsWith("$")));
            }
            else
            {
                return new MemoryQueryResultSet(this.m_configurationService.Configurations.Where(o => !o.Id.StartsWith("$"))); // hide the $ystem configuration
            }
        }

        /// <summary>
        /// Update a match configuration
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public override object Update(object data)
        {
            if (data is IRecordMatchingConfiguration configMatch)
            {
                return this.m_configurationService.SaveConfiguration(configMatch);
            }
            else
            {
                throw new ArgumentException("Incorrect match configuration type");
            }
        }

        /// <summary>
        /// Invoke the specified operation
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public override object InvokeOperation(object scopingEntityKey, string operationName, ParameterCollection parameters)
        {
            return base.InvokeOperation(scopingEntityKey, operationName, parameters);
        }


        /// <summary>
        /// Query for associated entities
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override IQueryResultSet QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter)
        {
            return base.QueryChildObjects(scopingEntityKey, propertyName, filter);
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public override object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            return base.RemoveChildObject(scopingEntityKey, propertyName, subItemKey);
        }

        /// <summary>
        /// Add an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public override object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            return base.AddChildObject(scopingEntityKey, propertyName, scopedItem);
        }

        /// <summary>
        /// Get associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public override object GetChildObject(object scopingEntity, string propertyName, object subItem)
        {
            return base.GetChildObject(scopingEntity, propertyName, subItem);
        }

        /// <summary>
        /// Checkout the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public object CheckOut(object key)
        {
            var match = this.Get(key, null) as IRecordMatchingConfiguration;
            if (match != null &&
                this.m_checkoutService?.Checkout<IRecordMatchingConfiguration>(match.Uuid) == false)
            {
                throw new ObjectLockedException();
            }
            return null;
        }

        /// <summary>
        /// Checkout the specified object
        /// </summary>
        public object CheckIn(object key)
        {
            var match = this.Get(key, null) as IRecordMatchingConfiguration;
            if (match != null &&
                this.m_checkoutService?.Checkin<IRecordMatchingConfiguration>(match.Uuid) == false)
            {
                throw new ObjectLockedException();
            }
            return null;
        }
    }
}