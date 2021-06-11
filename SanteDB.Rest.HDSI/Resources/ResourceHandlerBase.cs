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

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public abstract class ResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData>, INullifyResourceHandler, ICancelResourceHandler, IAssociativeResourceHandler, ILockableResourceHandler, IApiResourceHandlerEx
        where TData : IdentifiedData, new()
    {

        // Property providers
        private ConcurrentDictionary<String, IRestAssociatedPropertyProvider> m_propertyProviders = new ConcurrentDictionary<string, IRestAssociatedPropertyProvider>();

        /// <summary>
        /// Gets the scope
        /// </summary>
        override public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// OBsoletion wrapper with locking
        /// </summary>
        public override object Obsolete(object key)
        {
            try
            {
                this.Lock((Guid)key);
                return base.Obsolete(key);
            }
            finally
            {
                this.Unlock((Guid)key);
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
                    this.Lock((Guid)id.Key);
                return base.Update(data);
            }
            finally
            {
                if (data is IdentifiedData id)
                    this.Unlock((Guid)id.Key);
            }
        }

        /// <summary>
        /// Add an associated entity
        /// </summary>
        public virtual object AddAssociatedEntity(object scopingEntityKey, string propertyName, object scopedItem)
        {
            Guid objectKey = (Guid)scopingEntityKey;

            try
            {
                this.Lock(objectKey);
                if(this.m_propertyProviders.TryGetValue(propertyName, out IRestAssociatedPropertyProvider propertyProvider))
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
                this.Unlock(objectKey);
            }
        }

        /// <summary>
        /// Cancel the specified object
        /// </summary>
        public object Cancel(object key)
        {
            try
            {
                this.Lock(key);
                if (this.GetRepository() is ICancelRepositoryService<TData>)
                    return (this.GetRepository() as ICancelRepositoryService<TData>).Cancel((Guid)key);
                else
                    throw new NotSupportedException($"Repository for {this.ResourceName} does not support Cancel");
            }
            finally
            {
                this.Unlock(key);
            }
        }

        /// <summary>
        /// Get associated entity
        /// </summary>
        public virtual object GetAssociatedEntity(object scopingEntity, string propertyName, object subItem)
        {
            Guid objectKey = (Guid)scopingEntity, subItemKey = (Guid)subItem;
            if (this.m_propertyProviders.TryGetValue(propertyName, out IRestAssociatedPropertyProvider propertyProvider))
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
        public object Lock(object key)
        {
            var adHocCache = ApplicationServiceContext.Current.GetService<IResourceEditLockService>();
            adHocCache?.Lock<TData>((Guid)key);
            return null;
        }

        /// <summary>
        /// Nullify the specified object
        /// </summary>
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
        public virtual IEnumerable<object> QueryAssociatedEntities(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            Guid objectKey = (Guid)scopingEntityKey;
            if (this.m_propertyProviders.TryGetValue(propertyName, out IRestAssociatedPropertyProvider propertyProvider))
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
        public virtual object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            Guid objectKey = (Guid)scopingEntityKey;

            try
            {
                this.Lock(objectKey);
                if (this.m_propertyProviders.TryGetValue(propertyName, out IRestAssociatedPropertyProvider propertyProvider))
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
                this.Unlock(objectKey);
            }
        }

        /// <summary>
        /// Release the specified lock
        /// </summary>
        public object Unlock(object key)
        {
            var adHocCache = ApplicationServiceContext.Current.GetService<IResourceEditLockService>();
            adHocCache?.Unlock<TData>((Guid)key);
            return null;
        }

        /// <summary>
        /// Touch the specified object
        /// </summary>
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
        public void AddPropertyHandler(IRestAssociatedPropertyProvider property)
        {
            this.m_propertyProviders.TryAdd(property.PropertyName, property);
        }
    }
}