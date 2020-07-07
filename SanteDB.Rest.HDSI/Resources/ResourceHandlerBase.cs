/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.Api.Services;
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

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public abstract class ResourceHandlerBase<TData> : SanteDB.Rest.Common.ResourceHandlerBase<TData>, INullifyResourceHandler, ICancelResourceHandler, IAssociativeResourceHandler, ILockableResourceHandler, IApiResourceHandlerEx
        where TData : IdentifiedData
    {

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
                switch (propertyName)
                {
                    // Merge a duplicate
                    case "_merge":

                        if (scopedItem is Bundle bundle)
                        {
                            var mergeService = ApplicationServiceContext.Current.GetService<IRecordMergingService<TData>>();
                            if (mergeService == null)
                                throw new ConfigurationException($"Missing merge service registration for {typeof(TData)}");

                            // Get scoped entity
                            return mergeService.Merge(objectKey, bundle.Item.Select(o => o.Key.Value));
                        }
                        else
                            throw new ArgumentException($"Merge body must be a Bundle of items to be merged into {objectKey}");
                    case "_flag":
                        {
                            var mergeService = ApplicationServiceContext.Current.GetService<IRecordMergingService<TData>>();
                            if (mergeService == null)
                                throw new ConfigurationException($"Missing merge service registration for {typeof(TData)}");
                            // Get scoped entity
                            if (objectKey == Guid.Empty)
                            {
                                mergeService.FlagDuplicates();
                                return null;
                            }
                            else
                                return mergeService.FlagDuplicates(objectKey);
                        }
                    default:
                        throw new KeyNotFoundException($"Cannot find {propertyName}");
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
            switch (propertyName)
            {
                case "_duplicate":
                    {
                        var mergeService = ApplicationServiceContext.Current.GetService<IRecordMergingService<TData>>();
                        if (mergeService == null)
                            throw new ConfigurationException($"Missing merge service registration for {typeof(TData)}");

                        // Get scoped entity
                        return mergeService.Diff(objectKey, subItemKey);
                    }
                default:
                    throw new KeyNotFoundException($"Cannot find {propertyName}");
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
            switch (propertyName)
            {
                case "_duplicate":
                    {
                        var mergeService = ApplicationServiceContext.Current.GetService<IRecordMergingService<TData>>();
                        if (mergeService == null)
                            throw new ConfigurationException($"Missing merge service registration for {typeof(TData)}");

                        // Get scoped entity
                        var query = QueryExpressionParser.BuildLinqExpression<TData>(filter).Compile();

                        var results = mergeService.GetDuplicates(objectKey).Where(query);
                        totalCount = results.Count();
                        return results.Skip(offset).Take(count);
                    }
                case "_ignore":
                    {
                        var mergeService = ApplicationServiceContext.Current.GetService<IRecordMergingService<TData>>();
                        if (mergeService == null)
                            throw new ConfigurationException($"Missing merge service registration for {typeof(TData)}");

                        // Get scoped entity
                        var query = QueryExpressionParser.BuildLinqExpression<TData>(filter).Compile();

                        var results = mergeService.GetIgnored(objectKey).Where(query);
                        totalCount = results.Count();
                        return results.Skip(offset).Take(count);
                    }
                default:
                    throw new KeyNotFoundException($"Cannot find {propertyName}");
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
                switch (propertyName)
                {
                    case "_duplicate":
                        {
                            var mergeService = ApplicationServiceContext.Current.GetService<IRecordMergingService<TData>>();
                            if (mergeService == null)
                                throw new ConfigurationException($"Missing merge service registration for {typeof(TData)}");

                            // Get scoped entity
                            return mergeService.Ignore(objectKey, new Guid[] { (Guid)subItemKey });
                        }
                    case "_ignore":
                        {
                            var mergeService = ApplicationServiceContext.Current.GetService<IRecordMergingService<TData>>();
                            if (mergeService == null)
                                throw new ConfigurationException($"Missing merge service registration for {typeof(TData)}");

                            // Get scoped entity
                            return mergeService.UnIgnore(objectKey, new Guid[] { (Guid)subItemKey });
                        }
                    default:
                        throw new KeyNotFoundException($"Cannot find {propertyName}");
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
    }
}