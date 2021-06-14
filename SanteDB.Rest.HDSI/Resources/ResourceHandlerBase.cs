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
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Core.Security;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public abstract class ResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData>, INullifyResourceHandler, ICancelResourceHandler, IChainedApiResourceHandler, ICheckoutResourceHandler, IApiResourceHandlerEx
        where TData : IdentifiedData, new()
    {

        // Property providers
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();

        /// <summary>
        /// Gets the scope
        /// </summary>
        override public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Get all child resources
        /// </summary>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

        /// <summary>
        /// OBsoletion wrapper with locking
        /// </summary>
        public override object Obsolete(object key)
        {
            try
            {
                this.Checkout((Guid)key);
                return base.Obsolete(key);
            }
            finally
            {
                this.Checkin((Guid)key);
            }
        }

        /// <summary>
        /// Update with lock
        /// </summary>
        public override object Update(object data)
        {
            try
            {
                if(data is IdentifiedData id)
                    this.Checkout((Guid)id.Key);
                return base.Update(data);
            }
            finally
            {
                if (data is IdentifiedData id)
                    this.Checkin((Guid)id.Key);
            }
        }

        /// <summary>
        /// Add an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            Guid objectKey = (Guid)scopingEntityKey;

            try
            {
                this.Checkout(objectKey);
                if(this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
                {
                    return propertyProvider.Add(typeof(TData), scopingEntityKey, scopedItem);
                }
                else
                {
                    throw new KeyNotFoundException($"{propertyName} not found");
                }
            }
            finally
            {
                this.Checkin(objectKey);
            }
        }

        /// <summary>
        /// Cancel the specified object
        /// </summary>
        public object Cancel(object key)
        {
            try
            {
                this.Checkout(key);
                if (this.GetRepository() is ICancelRepositoryService<TData>)
                    return (this.GetRepository() as ICancelRepositoryService<TData>).Cancel((Guid)key);
                else
                    throw new NotSupportedException($"Repository for {this.ResourceName} does not support Cancel");
            }
            finally
            {
                this.Checkin(key);
            }
        }

        /// <summary>
        /// Get associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object GetChildObject(object scopingEntity, string propertyName, object subItem)
        {
            Guid objectKey = (Guid)scopingEntity, subItemKey = (Guid)subItem;
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Get(typeof(TData), objectKey, subItemKey);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Attempt to get a lock on the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object Checkout(object key)
        {
            var adHocCache = ApplicationServiceContext.Current.GetService<IResourceCheckoutService>();
            adHocCache?.Checkout<TData>((Guid)key);
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
                throw new NotSupportedException($"Repository for {this.ResourceName} does not support Nullify");
        }

        /// <summary>
        /// Query for associated entities
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IEnumerable<object> QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            Guid objectKey = (Guid)scopingEntityKey;
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(typeof(TData), objectKey, filter, offset, count, out totalCount);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            Guid objectKey = (Guid)scopingEntityKey;

            try
            {
                this.Checkout(objectKey);
                if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
                {
                    return propertyProvider.Remove(typeof(TData), objectKey, subItemKey);
                }
                else
                {
                    throw new KeyNotFoundException($"{propertyName} not found");
                }
            }
            finally
            {
                this.Checkin(objectKey);
            }
        }

        /// <summary>
        /// Release the specified lock
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object Checkin(object key)
        {
            var adHocCache = ApplicationServiceContext.Current.GetService<IResourceCheckoutService>();
            adHocCache?.Checkin<TData>((Guid)key);
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
                throw new InvalidOperationException("Repository service does not support TOUCH");

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