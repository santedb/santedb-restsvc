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
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that wraps a security based entity
    /// </summary>
    /// <typeparam name="TSecurityEntity">The type of security entity being wrapped</typeparam>
    public abstract class SecurityEntityResourceHandler<TSecurityEntity> : IApiResourceHandler
        where TSecurityEntity : SecurityEntity
    {

        // The repository for the entity
        private IRepositoryService<TSecurityEntity> m_repository;

        // Get the tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(SecurityEntityResourceHandler<TSecurityEntity>));

        /// <summary>
        /// Create a new instance of the security entity resource handler
        /// </summary>
        public SecurityEntityResourceHandler() 
        {
            ApplicationServiceContext.Current.AddStarted((o, e) => this.m_repository = ApplicationServiceContext.Current.GetService<IRepositoryService<TSecurityEntity>>());
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName => typeof(TSecurityEntity).GetCustomAttribute<XmlRootAttribute>().ElementName;

        /// <summary>
        /// Gets the type that this handles
        /// </summary>
        public virtual Type Type => typeof(ISecurityEntityInfo<TSecurityEntity>);

        /// <summary>
        /// Gets the scope of the object
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of the resource
        /// </summary>
        public ResourceCapability Capabilities => ResourceCapability.Create | ResourceCapability.CreateOrUpdate | ResourceCapability.Delete | ResourceCapability.Get | ResourceCapability.Search | ResourceCapability.Update;

        /// <summary>
        /// Gets the repository
        /// </summary>
        protected IRepositoryService<TSecurityEntity> GetRepository()
        {
            if (this.m_repository == null)
                this.m_repository = ApplicationServiceContext.Current.GetService<IRepositoryService<TSecurityEntity>>();
            if (this.m_repository == null)
            {
                this.m_tracer.TraceWarning("IRepositoryService<{0}> was not found will generate a default one using IRepositoryServiceFactory", typeof(TSecurityEntity).FullName);
                var factoryService = ApplicationServiceContext.Current.GetService<IRepositoryServiceFactory>();
                if (factoryService == null)
                    throw new KeyNotFoundException($"IRepositoryService<{typeof(TSecurityEntity).FullName}> not found and no repository is found");
                this.m_repository = factoryService.CreateRepository<TSecurityEntity>();
            }
            return this.m_repository;
        }

        /// <summary>
        /// Creates the specified object in the underlying data store
        /// </summary>
        /// <param name="data">The data that is to be created</param>
        /// <param name="updateIfExists">True if the data should be updated if it already exists</param>
        /// <returns>The created object</returns>
        public virtual object Create(object data, bool updateIfExists)
        {

            // First, we want to copy over the roles
            var td = data as ISecurityEntityInfo<TSecurityEntity>;
            if (td is null) throw new ArgumentException("Invalid type", nameof(data));
            // Now for the fun part we want to map any policies over to the wrapped type
            if(td.Entity.Policies != null)
                td.Entity.Policies = td.Policies.Select(p => new SecurityPolicyInstance(p.Policy, p.Grant)).ToList();

            if(updateIfExists)
                td.Entity = this.GetRepository().Save(td.Entity);
            else
                td.Entity = this.GetRepository().Insert(td.Entity);

            return td;
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        public virtual object Get(object id, object versionId)
        {
            // Get the object
            var data = this.GetRepository().Get((Guid)id, (Guid)versionId);

            var retVal = Activator.CreateInstance(this.Type, data) as ISecurityEntityInfo<TSecurityEntity>;
            retVal.Policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetActivePolicies(data).Select(o=>new SecurityPolicyInfo(o)).ToList();
            return retVal;

        }

        /// <summary>
        /// Obsolete the specified object
        /// </summary>
        public virtual object Obsolete(object key)
        {
            return Activator.CreateInstance(this.Type, this.GetRepository().Obsolete((Guid)key));
        }

        /// <summary>
        /// Query for the specified object
        /// </summary>
        public virtual IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Query for specified objects
        /// </summary>
        public virtual IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var query = QueryExpressionParser.BuildLinqExpression<TSecurityEntity>(queryParameters);

            List<String> orderBy = null;
            // Order by
            List<ModelSort<TSecurityEntity>> sortParameters = new List<ModelSort<TSecurityEntity>>();
            if (queryParameters.TryGetValue("_orderBy", out orderBy))
                foreach (var itm in orderBy)
                {
                    var sortData = itm.Split(':');
                    sortParameters.Add(new ModelSort<TSecurityEntity>(
                        QueryExpressionParser.BuildPropertySelector<TSecurityEntity>(sortData[0]),
                        sortData.Length == 1 || sortData[1] == "asc" ? Core.Model.Map.SortOrderType.OrderBy : Core.Model.Map.SortOrderType.OrderByDescending
                    ));
                }

            var results = this.GetRepository().Find(query, offset, count, out totalCount, sortParameters.ToArray());

            return results.AsParallel().AsOrdered().Select(o =>
            {
                var r = Activator.CreateInstance(this.Type, o) as ISecurityEntityInfo<TSecurityEntity>;
                r.Policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetActivePolicies(o).Select(p=>new SecurityPolicyInfo(p)).ToList();
                return r;
            }).OfType<Object>();

        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        public virtual object Update(object data)
        {
            // First, we want to copy over the roles
            var td = data as ISecurityEntityInfo<TSecurityEntity>;
            if (td is null) throw new ArgumentException("Invalid type", nameof(data));

            // Now for the fun part we want to map any policies over to the wrapped type
            td.Entity.Policies = td.Policies.Select(p => new SecurityPolicyInstance(p.Policy, p.Grant)).ToList();
            td.Entity = this.GetRepository().Save(td.Entity);

            return td;
        }
    }
}
