/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which serves out match metadata
    /// </summary>
    public class MatchConfigurationResourceHandler : IApiResourceHandler, IChainedApiResourceHandler
    {

        // Property providers
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "MatchConfiguration";

        /// <summary>
        /// Gets the type that this returns
        /// </summary>
        public Type Type => typeof(IRecordMatchingConfiguration);

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of this service
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <summary>
        /// Child resources
        /// </summary>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

        /// <summary>
        /// Add an associative entity
        /// </summary>
        public object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Add(this.Type, scopingEntityKey, scopedItem);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Create a match configuration
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException("Currently not supported");
        }

        /// <summary>
        /// Get the specified match configuration identifier
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public object Get(object id, object versionId)
        {
            var service = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();
            if (service == null)
                throw new InvalidOperationException("Matching configuration manager is not enabled");
            return service.GetConfiguration(id.ToString());
        }

        /// <summary>
        /// Get an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata), Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Get(this.Type, scopingEntity, subItemKey);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Delete a match configuration
        /// </summary>
        public object Obsolete(object key)
        {
            throw new NotSupportedException("Not supported yet");
        }

        /// <summary>
        /// Query for match configurations
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            return this.Query(queryParameters, 0, 100, out int t);
        }

        /// <summary>
        /// Query for match configurations
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var service = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();
            if (service == null)
                throw new InvalidOperationException("Matching configuration service is not enabled");

            totalCount = service.Configurations.Count();
            if (queryParameters.TryGetValue("name", out List<String> values))
                return service.Configurations
                    .Where(o => o == values.First())
                    .Skip(offset)
                    .Take(count)
                    .Select(o => service.GetConfiguration(o))
                    .OfType<Object>();
            else
                return service.Configurations
                    .Skip(offset)
                    .Take(count)
                    .Select(o => service.GetConfiguration(o))
                    .OfType<Object>();
        }

        /// <summary>
        /// Query for associated entities on a particular sub-path
        /// </summary>
        public IEnumerable<object> QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(this.Type, scopingEntityKey, filter, offset, count, out totalCount);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        public object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Remove(this.Type,scopingEntityKey, subItemKey);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Update a match configuration
        /// </summary>
        public object Update(object data)
        {
            throw new NotSupportedException("Not currently supported");
        }


        /// <summary>
        /// Add the property handler to this handler
        /// </summary>
        public void AddChildResource(IApiChildResourceHandler property)
        {
            this.m_propertyProviders.TryAdd(property.ResourceName, property);
        }
    }
}
