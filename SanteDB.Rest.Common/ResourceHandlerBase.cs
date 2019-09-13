/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: justi
 * Date: 2019-1-12
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;
using SanteDB.Core.Model.Query;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using RestSrvr;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Resource handler base
    /// </summary>
    public abstract class ResourceHandlerBase<TResource> : IApiResourceHandler where TResource : IdentifiedData
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(ResourceHandlerBase<TResource>));

        /// <summary>
        /// IRepository service
        /// </summary>
        protected IRepositoryService<TResource> m_repository = null;

        /// <summary>
        /// Constructs the resource handler base
        /// </summary>
        public ResourceHandlerBase()
        {
        }

        /// <summary>
        /// Gets the repository
        /// </summary>
        protected IRepositoryService<TResource> GetRepository()
        {
            if (this.m_repository == null)
                this.m_repository = ApplicationServiceContext.Current.GetService<IRepositoryService<TResource>>();
            if(this.m_repository == null)
            {
                this.m_tracer.TraceWarning("IRepositoryService<{0}> was not found will generate a default one using IRepositoryServiceFactory", typeof(TResource).FullName);
                var factoryService = ApplicationServiceContext.Current.GetService<IRepositoryServiceFactory>();
                if (factoryService == null)
                    throw new KeyNotFoundException($"IRepositoryService<{typeof(TResource).FullName}> not found and no repository is found");
                this.m_repository = factoryService.CreateRepository<TResource>();
            }
            return this.m_repository;
        }

        /// <summary>
        /// Gets the scope of the resource handler
        /// </summary>
        public abstract Type Scope { get; }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public virtual string ResourceName
        {
            get
            {
                return typeof(TResource).GetCustomAttribute<XmlRootAttribute>().ElementName;
            }
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        public Type Type
        {
            get
            {
                return typeof(TResource);
            }
        }

        /// <summary>
        /// Gets the capabilities of this resource handler
        /// </summary>
        public virtual ResourceCapabilityType Capabilities
        {
            get
            {
                return ResourceCapabilityType.Create |
                    ResourceCapabilityType.CreateOrUpdate |
                    ResourceCapabilityType.Delete |
                    ResourceCapabilityType.Get |
                    ResourceCapabilityType.GetVersion |
                    ResourceCapabilityType.History |
                    ResourceCapabilityType.Search |
                    ResourceCapabilityType.Update;
            }
        }


        /// <summary>
        /// Create a resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Create(Object data, bool updateIfExists)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            else if ((this.Capabilities & ResourceCapabilityType.Create) == 0 &&
                (this.Capabilities & ResourceCapabilityType.CreateOrUpdate) == 0)
                throw new NotSupportedException();

            var bundle = data as Bundle;

            bundle?.Reconstitute();

            var processData = bundle?.Entry ?? data;
            
            try
            {
                if (!(processData is TResource))
                    throw new ArgumentException($"Invalid data submission. Expected {typeof(TResource).FullName} but received {processData.GetType().FullName}. If you are submitting a bundle, ensure it has an entry point.");
                else if (processData is TResource)
                {
                    
                    var resourceData = processData as TResource;
                    resourceData = updateIfExists ? this.GetRepository().Save(resourceData) : this.GetRepository().Insert(resourceData);

                    AuditUtil.AuditCreate(Core.Auditing.OutcomeIndicator.Success, null, resourceData);
                    return resourceData;
                }
            }
            catch(Exception e)
            {
                AuditUtil.AuditCreate(Core.Auditing.OutcomeIndicator.MinorFail, null, data);
                throw e;
            }

            throw new ArgumentException(nameof(data), "Invalid data type");
        }

        /// <summary>
        /// Read clinical data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Get(object id, object versionId)
        {
            if ((this.Capabilities & ResourceCapabilityType.Get) == 0 &&
                (this.Capabilities & ResourceCapabilityType.GetVersion) == 0)
                throw new NotSupportedException();

            try
            {
                var retVal = this.GetRepository().Get((Guid)id, (Guid)versionId);
                AuditUtil.AuditQuery(Core.Auditing.OutcomeIndicator.Success, id.ToString(), retVal);
                return retVal;
            }
            catch(Exception e)
            {
                AuditUtil.AuditQuery<TResource>(Core.Auditing.OutcomeIndicator.MinorFail, id.ToString());
                throw e;
            }
        }

        /// <summary>
        /// Obsolete data
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Obsolete(object key)
        {
            if ((this.Capabilities & ResourceCapabilityType.Delete) == 0)
                throw new NotSupportedException();

            try
            {
                var retVal = this.GetRepository().Obsolete((Guid)key);
                AuditUtil.AuditDelete(Core.Auditing.OutcomeIndicator.Success, key.ToString(), retVal);
                return retVal;
            }
            catch(Exception e)
            {
                AuditUtil.AuditDelete<TResource>(Core.Auditing.OutcomeIndicator.MinorFail, key.ToString());
                throw e;
            }
        }

        /// <summary>
        /// Perform a query 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            if ((this.Capabilities & ResourceCapabilityType.Search) == 0)
                throw new NotSupportedException();

            try
            {
                int tr = 0;
                var retVal = this.Query(queryParameters, 0, 100, out tr);

                return retVal;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        /// <summary>
        /// Perform the actual query
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            if ((this.Capabilities & ResourceCapabilityType.Search) == 0)
                throw new NotSupportedException();
            try
            {

                var queryExpression = QueryExpressionParser.BuildLinqExpression<TResource>(queryParameters, null, false);
                List<String> query = null, id = null, orderBy = null;

                // Order by
                List<ModelSort<TResource>> sortParameters = new List<ModelSort<TResource>>();
                if(queryParameters.TryGetValue("_orderBy", out orderBy))
                    foreach(var itm in orderBy)
                    {
                        var sortData = itm.Split(':');
                        sortParameters.Add(new ModelSort<TResource>(
                            QueryExpressionParser.BuildPropertySelector<TResource>(sortData[0]),
                            sortData.Length == 1 || sortData[1] == "asc" ? Core.Model.Map.SortOrderType.OrderBy : Core.Model.Map.SortOrderType.OrderByDescending
                        ));
                    }

                IEnumerable<TResource> retVal = null;
                if (queryParameters.TryGetValue("_id", out id)) {
                    var obj = this.GetRepository().Get(Guid.Parse(id.First()));
                    if (obj != null)
                        retVal = new List<TResource>() { obj };
                    else
                        retVal = new List<TResource>();
                    totalCount = retVal.Count();
                }
                else if (queryParameters.TryGetValue("_queryId", out query) && this.GetRepository() is IPersistableQueryRepositoryService<TResource>)
                {
                    Guid queryId = Guid.Parse(query[0]);
                    List<String> data = null;
                    if (queryParameters.TryGetValue("_subscription", out data))
                    { // subscription based query
                        totalCount = 0;
                        retVal = ApplicationServiceContext.Current.GetService<ISubscriptionExecutor>()?.Execute(Guid.Parse(data.First()), queryParameters, offset, count, out totalCount, queryId).OfType<TResource>();
                    }
                    else if (queryParameters.TryGetValue("_lean", out data) && data[0] == "true" && this.GetRepository() is IFastQueryRepositoryService<TResource>)
                        retVal = (this.GetRepository() as IFastQueryRepositoryService<TResource>).FindFast(queryExpression, offset, count, out totalCount, queryId);
                    else
                        retVal = (this.GetRepository() as IPersistableQueryRepositoryService<TResource>).Find(queryExpression, offset, count, out totalCount, queryId, sortParameters.ToArray());
                }
                else
                {
                    List<String> lean = null;
                    if (queryParameters.TryGetValue("_lean", out lean) && lean[0] == "true" && this.GetRepository() is IFastQueryRepositoryService<TResource>)
                        retVal = (this.GetRepository() as IFastQueryRepositoryService<TResource>).FindFast(queryExpression, offset, count, out totalCount, Guid.Empty);
                    else
                        retVal = this.GetRepository().Find(queryExpression, offset, count, out totalCount, sortParameters.ToArray());
                }

                AuditUtil.AuditQuery(Core.Auditing.OutcomeIndicator.Success, queryParameters.ToString(), retVal.ToArray());
                return retVal;
            }
            catch (Exception e)
            {
                AuditUtil.AuditQuery<TResource>(Core.Auditing.OutcomeIndicator.MinorFail, queryParameters.ToString());
                this.m_tracer.TraceError("Error executing query: {0}", e);
                throw e;
            }
        }

        /// <summary>
        /// Perform an update
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual Object Update(Object data)
        {

            if ((this.Capabilities & ResourceCapabilityType.Update) == 0)
                throw new NotSupportedException();

            Bundle bundleData = data as Bundle;
            bundleData?.Reconstitute();
            var processData = bundleData?.Entry ?? data;

            try
            {
                if (!(processData is TResource))
                    throw new ArgumentException($"Invalid data submission. Expected {typeof(TResource).FullName} but received {processData.GetType().FullName}. If you are submitting a bundle, ensure it has an entry point.");
                else if (processData is TResource)
                {
                    var entityData = processData as TResource;
                    
                    var retVal = this.GetRepository().Save(entityData);
                    AuditUtil.AuditUpdate(Core.Auditing.OutcomeIndicator.Success, null, retVal);
                    return retVal;
                }
                else
                {
                    throw new ArgumentException("Invalid persistence type");
                }
            }
            catch (Exception e)
            {
                AuditUtil.AuditUpdate(Core.Auditing.OutcomeIndicator.MinorFail, null, data);
                throw e;
            }
        }
    }
}
