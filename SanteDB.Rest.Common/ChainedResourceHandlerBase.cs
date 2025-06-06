﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Chained resource handler base
    /// </summary>
    public abstract class ChainedResourceHandlerBase : IChainedApiResourceHandler, IOperationalApiResourceHandler
    {
        // Property providers
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();
        private ConcurrentDictionary<String, IApiChildOperation> m_operationProviders = new ConcurrentDictionary<string, IApiChildOperation>();
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Get the localization service
        /// </summary>
        protected ILocalizationService LocalizationService => this.m_localizationService;

        /// <summary>
        /// The tracer
        /// </summary>
        protected readonly Tracer m_tracer;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ChainedResourceHandlerBase(ILocalizationService localizationService)
        {
            this.m_localizationService = localizationService;
            this.m_tracer = Tracer.GetTracer(this.GetType());
        }

        /// <inheritdoc/>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

        /// <inheritdoc/>
        public abstract string ResourceName { get; }
        /// <inheritdoc/>
        public abstract Type Type { get; }
        /// <inheritdoc/>
        public abstract Type Scope { get; }
        /// <inheritdoc/>
        public abstract ResourceCapabilityType Capabilities { get; }

        /// <inheritdoc/>
        public IEnumerable<IApiChildOperation> Operations => this.m_operationProviders.Values;

        /// <summary>
        /// Add a child resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual void AddChildResource(IApiChildResourceHandler property)
        {
            this.m_propertyProviders.TryAdd(property.Name, property);
        }

        /// <summary>
        /// Remove a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Remove(this.Type, scopingEntityKey, subItemKey);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Query child objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IQueryResultSet QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(this.Type, scopingEntityKey, filter);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Add a child object instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Add(this.Type, scopingEntityKey, scopedItem);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Get a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntity == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Get(this.Type, scopingEntity, subItemKey);
            }
            else
            {
                this.m_tracer.TraceError($"{propertyName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = propertyName
                }));
            }
        }

        /// <summary>
        /// Try to get a chained resource
        /// </summary>
        public virtual bool TryGetChainedResource(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildResourceHandler childHandler)
        {
            var retVal = this.m_propertyProviders.TryGetValue(propertyName, out childHandler) &&
                childHandler.ScopeBinding.HasFlag(bindingType);
            if (!retVal)
            {
                childHandler = null;//clear in case of lazy programmers like me
            }
            return retVal;
        }

        /// <inheritdoc/>
        public abstract object Create(object data, bool updateIfExists);
        /// <inheritdoc/>
        public abstract object Delete(object key);
        /// <inheritdoc/>
        public abstract object Get(object id, object versionId);
        /// <inheritdoc/>
        public abstract IQueryResultSet Query(NameValueCollection queryParameters);
        /// <inheritdoc/>
        public abstract object Update(object data);

        /// <inheritdoc/>
        public void AddOperation(IApiChildOperation operation)
        {
            this.m_operationProviders.TryAdd(operation.Name, operation);
        }

        /// <inheritdoc/>
        public virtual object InvokeOperation(object scopingEntityKey, string operationName, ParameterCollection parameters)
        {
            if (this.TryGetOperation(operationName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out var operationProvider))
            {
                return operationProvider.Invoke(this.Type, scopingEntityKey, parameters);
            }
            else
            {
                this.m_tracer.TraceError($"{operationName} not found");
                throw new KeyNotFoundException(this.m_localizationService.GetString("error.type.KeyNotFoundException.notFound", new
                {
                    param = operationName
                }));
            }
        }

        /// <inheritdoc/>
        public bool TryGetOperation(string operationName, ChildObjectScopeBinding bindingType, out IApiChildOperation operationHandler)
        {
            var retVal = this.m_operationProviders.TryGetValue(operationName, out operationHandler) &&
                operationHandler.ScopeBinding.HasFlag(bindingType);
            if (!retVal)
            {
                operationHandler = null;//clear in case of lazy programmers like me
            }
            return retVal;
        }
    }
}