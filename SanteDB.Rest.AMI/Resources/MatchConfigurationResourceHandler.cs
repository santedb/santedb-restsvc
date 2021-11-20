/*
* Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
* User: fyfej (Justin Fyfe)
* Date: 2021-8-5
*/

using SanteDB.Core.Interop;
using SanteDB.Core.Matching;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which serves out match metadata
    /// </summary>
    public class MatchConfigurationResourceHandler : IApiResourceHandler, IOperationalApiResourceHandler, IChainedApiResourceHandler
    {
        // Configuration service
        private IRecordMatchingConfigurationService m_configurationService;

        // Property providers
        private ConcurrentDictionary<String, IApiChildOperation> m_childOperations = new ConcurrentDictionary<string, IApiChildOperation>();

        // Child resources
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();

        // Localization service
        private ILocalizationService m_localizationService;

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
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Delete;

        /// <summary>
        /// Gets the operations
        /// </summary>
        public IEnumerable<IApiChildOperation> Operations => this.m_childOperations.Values;

        /// <summary>
        /// Get the child resources
        /// </summary>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

        /// <summary>
        /// Match configuration resource handler
        /// </summary>
        public MatchConfigurationResourceHandler(ILocalizationService localizationService, IRecordMatchingConfigurationService configurationService = null)
        {
            // TODO: Throw method not support exception if someone calls this
            this.m_configurationService = configurationService;
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Create a match configuration
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public object Create(object data, bool updateIfExists)
        {
            if (data is IRecordMatchingConfiguration configMatch)
                return this.m_configurationService.SaveConfiguration(configMatch);
            else
                throw new ArgumentException("Incorrect match configuration type");
        }

        /// <summary>
        /// Get the specified match configuration identifier
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public object Get(object id, object versionId)
        {
            return this.m_configurationService.GetConfiguration(id.ToString());
        }

        /// <summary>
        /// Delete a match configuration
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public object Obsolete(object key)
        {
            return this.m_configurationService.DeleteConfiguration(key.ToString());
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
            totalCount = this.m_configurationService.Configurations.Count();
            if (queryParameters.TryGetValue("name", out List<String> values))
                return this.m_configurationService.Configurations
                    .Where(o => o.Id.Contains(values.First().Replace("~", "")))
                    .Skip(offset)
                    .Take(count)
                    .OfType<Object>();
            else
                return this.m_configurationService.Configurations
                    .Skip(offset)
                    .Take(count)
                    .OfType<Object>();
        }

        /// <summary>
        /// Update a match configuration
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterMatchConfiguration)]
        public object Update(object data)
        {
            if (data is IRecordMatchingConfiguration configMatch)
                return this.m_configurationService.SaveConfiguration(configMatch);
            else
                throw new ArgumentException("Incorrect match configuration type");
        }

        /// <summary>
        /// Add an operation
        /// </summary>
        public void AddOperation(IApiChildOperation property)
        {
            this.m_childOperations.TryAdd(property.Name, property);
        }

        /// <summary>
        /// Invoke the specified operation
        /// </summary>
        public object InvokeOperation(object scopingEntityKey, string operationName, ParameterCollection parameters)
        {
            if (this.TryGetOperation(operationName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildOperation handler))
            {
                return handler.Invoke(typeof(IRecordMatchingConfiguration), scopingEntityKey, parameters);
            }
            else
            {
                throw new NotSupportedException(this.m_localizationService.FormatString("error.type.NotSupportedException.operation", new
                {
                    param = operationName
                }));
            }
        }

        /// <summary>
        /// Try to get operation
        /// </summary>
        public bool TryGetOperation(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildOperation operationHandler)
        {
            var retVal = this.m_childOperations.TryGetValue(propertyName, out operationHandler) &&
                operationHandler.ScopeBinding.HasFlag(bindingType);
            if (!retVal)
            {
                operationHandler = null;//clear in case of lazy programmers like me
            }
            return retVal;
        }

        /// <summary>
        /// Try to get a chained resource
        /// </summary>
        public bool TryGetChainedResource(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildResourceHandler childHandler)
        {
            var retVal = this.m_propertyProviders.TryGetValue(propertyName, out childHandler) &&
                childHandler.ScopeBinding.HasFlag(bindingType);
            if (!retVal)
            {
                childHandler = null;//clear in case of lazy programmers like me
            }
            return retVal;
        }

        /// <summary>
        /// Add the property handler to this handler
        /// </summary>
        public void AddChildResource(IApiChildResourceHandler property)
        {
            this.m_propertyProviders.TryAdd(property.Name, property);
        }

        /// <summary>
        /// Query for associated entities
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IEnumerable<object> QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(typeof(IRecordMatchingConfiguration), scopingEntityKey, filter, offset, count, out totalCount);
            }
            else
            {
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Remove(typeof(IRecordMatchingConfiguration), scopingEntityKey, subItemKey);
            }
            else
            {
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Add an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Add(typeof(IRecordMatchingConfiguration), scopingEntityKey, scopedItem);
            }
            else
            {
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Get associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object GetChildObject(object scopingEntity, string propertyName, object subItem)
        {
            Guid objectKey = (Guid)scopingEntity, subItemKey = (Guid)subItem;
            if (this.TryGetChainedResource(propertyName, scopingEntity == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Get(typeof(IRecordMatchingConfiguration), objectKey, subItemKey);
            }
            else
            {
                throw new KeyNotFoundException(this.m_localizationService.FormatString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }
    }
}