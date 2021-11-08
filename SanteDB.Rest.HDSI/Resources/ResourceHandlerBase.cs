/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-5
 */

using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SanteDB.Rest.HDSI.Resources
{
	/// <summary>
	/// Represents a resource handler base type that is always bound to HDSI
	/// </summary>
	/// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
	[ServiceProvider("HDSI Resource Handler")]
    public abstract class ResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData>,
        INullifyResourceHandler,
        ICancelResourceHandler,
        IChainedApiResourceHandler,
        ICheckoutResourceHandler,
        IApiResourceHandlerEx,
        IOperationalApiResourceHandler

        where TData : IdentifiedData, new()
    {

        // Property providers
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();

        // Property providers
        private ConcurrentDictionary<String, IApiChildOperation> m_operationProviders = new ConcurrentDictionary<string, IApiChildOperation>();

        /// <summary>
        /// Creates a new resource handler base with a specified privacy service
        /// </summary>
        /// <param name="localizationService"></param>
        protected ResourceHandlerBase(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>
        /// Gets the scope
        /// </summary>
        override public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Get all child resources
        /// </summary>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

        /// <summary>
        /// Get all child resources
        /// </summary>
        public IEnumerable<IApiChildOperation> Operations => this.m_operationProviders.Values;

        /// <summary>
        /// OBsoletion wrapper with locking
        /// </summary>
        public override object Obsolete(object key)
        {
            try
            {
                this.CheckOut((Guid)key);
                return base.Obsolete(key);
            }
            finally
            {
                this.CheckIn((Guid)key);
            }
        }

        /// <summary>
        /// Update with lock
        /// </summary>
        public override object Update(object data)
        {
            try
            {
                if (data is IdentifiedData id)
                    this.CheckOut((Guid)id.Key);
                return base.Update(data);
            }
            finally
            {
                if (data is IdentifiedData id)
                    this.CheckIn((Guid)id.Key);
            }
        }

        /// <summary>
        /// Add an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            try
            {
                if (scopingEntityKey is Guid objectKey)
                {
                    this.CheckOut(objectKey);
                }
                if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
                {
                    return propertyProvider.Add(typeof(TData), scopingEntityKey, scopedItem);
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
            finally
            {
                if (scopingEntityKey is Guid objectKey)
                {
                    this.CheckIn(objectKey);
                }
            }
        }

        /// <summary>
        /// Cancel the specified object
        /// </summary>
        public object Cancel(object key)
        {
            try
            {
                this.CheckOut(key);
                if (this.GetRepository() is ICancelRepositoryService<TData>)
                    return (this.GetRepository() as ICancelRepositoryService<TData>).Cancel((Guid)key);
                else
                {
                    this.m_tracer.TraceError($"Repository for {this.ResourceName} does not support Cancel");
                    throw new NotSupportedException(this.m_localizationService.GetString("error.rest.hdsi.notSupportCancel", new 
                    { 
                        param = this.ResourceName
                    }));
                }
            }
            finally
            {
                this.CheckIn(key);
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
                return propertyProvider.Get(typeof(TData), objectKey, subItemKey);
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
        /// Attempt to get a lock on the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object CheckOut(object key)
        {
            var adHocCache = ApplicationServiceContext.Current.GetService<IResourceCheckoutService>();
            if (adHocCache?.Checkout<TData>((Guid)key) == false)
            {
                throw new ObjectLockedException(this.m_localizationService.GetString("error.type.ObjectLockedException"));
            }
            return null;
        }

        /// <summary>
        /// Nullify the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object Nullify(object key)
        {
            if (this.GetRepository() is IRepositoryServiceEx<TData> exRepo)
                return exRepo.Nullify((Guid)key);
            else
            {
                this.m_tracer.TraceError($"Repository for {this.ResourceName} does not support Nullify");
                throw new NotSupportedException(this.m_localizationService.GetString("error.rest.hdsi.notSupportNullify", new
                {
                    param = this.ResourceName
                }));
            }
        }

        /// <summary>
        /// Query for associated entities
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IEnumerable<object> QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(typeof(TData), scopingEntityKey, filter, offset, count, out totalCount);
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
        /// Remove an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            try
            {
                if (scopingEntityKey is Guid objectKey)
                {
                    this.CheckOut(objectKey);
                }
                if (this.TryGetChainedResource(propertyName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildResourceHandler propertyProvider))
                {
                    return propertyProvider.Remove(typeof(TData), scopingEntityKey, subItemKey);
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
            finally
            {
                if (scopingEntityKey is Guid objectKey)
                {
                    this.CheckIn(objectKey);
                }
            }
        }

        /// <summary>
        /// Release the specified lock
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object CheckIn(object key)
        {
            var adHocCache = ApplicationServiceContext.Current.GetService<IResourceCheckoutService>();
            if (adHocCache?.Checkin<TData>((Guid)key) == false)
            {
                throw new ObjectLockedException(this.m_localizationService.GetString("error.type.ObjectLockedException"));
            }
            return null;
        }

        /// <summary>
        /// Touch the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object Touch(object key)
        {
            if (this.GetRepository() is IRepositoryServiceEx<TData> exRepo)
            {
                var objectKey = (Guid)key;
                exRepo.Touch(objectKey);
                ApplicationServiceContext.Current.GetService<IDataCachingService>().Remove(objectKey);
                return this.Get(key, Guid.Empty);
            }
            else
            {
                this.m_tracer.TraceError("Repository service does not support TOUCH");
                throw new InvalidOperationException(this.m_localizationService.GetString("error.rest.hdsi.supportTouch"));
            }
        }

        /// <summary>
        /// Add the property handler to this handler
        /// </summary>
        public void AddChildResource(IApiChildResourceHandler property)
        {
            this.m_propertyProviders.TryAdd(property.Name, property);
        }

        /// <summary>
        /// Add the child operation
        /// </summary>
        public void AddOperation(IApiChildOperation operation)
        {
            this.m_operationProviders.TryAdd(operation.Name, operation);
        }

        /// <summary>
        /// Invoke the specified operation
        /// </summary>
        public object InvokeOperation(object scopingEntityKey, string operationName, ApiOperationParameterCollection parameters)
        {
            if (this.TryGetOperation(operationName, scopingEntityKey == null ? ChildObjectScopeBinding.Class : ChildObjectScopeBinding.Instance, out IApiChildOperation handler))
            {
                return handler.Invoke(typeof(TData), scopingEntityKey, parameters);
            }
            else
            {
                this.m_tracer.TraceError($"Operation {operationName} not supported");
                throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException.operation", new
                {
                    param = operationName
                }));
            }
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
        /// Try to get operation
        /// </summary>
        public bool TryGetOperation(string propertyName, ChildObjectScopeBinding bindingType, out IApiChildOperation operationHandler)
        {
            var retVal = this.m_operationProviders.TryGetValue(propertyName, out operationHandler) &&
                operationHandler.ScopeBinding.HasFlag(bindingType);
            if (!retVal)
            {
                operationHandler = null;//clear in case of lazy programmers like me
            }
            return retVal;
        }
    }
}