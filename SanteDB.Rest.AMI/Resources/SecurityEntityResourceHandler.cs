﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-11-20
 */
using SanteDB.Core;
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
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that wraps a security based entity
    /// </summary>
    /// <typeparam name="TSecurityEntity">The type of security entity being wrapped</typeparam>
    public abstract class SecurityEntityResourceHandler<TSecurityEntity> : IResourceHandler
        where TSecurityEntity : SecurityEntity
    {

        // The repository for the entity
        private IRepositoryService<TSecurityEntity> m_repository;
        
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
            td.Entity.Policies = td.Policies.Select(p => new SecurityPolicyInstance(p.Policy, p.Grant)).ToList();

            if(updateIfExists)
                td.Entity = this.m_repository.Save(td.Entity);
            else
                td.Entity = this.m_repository.Insert(td.Entity);

            return td;
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        public virtual object Get(object id, object versionId)
        {
            // Get the object
            var data = this.m_repository.Get((Guid)id, (Guid)versionId);

            var retVal = Activator.CreateInstance(this.Type, data) as ISecurityEntityInfo<TSecurityEntity>;
            retVal.Policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetActivePolicies(data).Select(o=>new SecurityPolicyInfo(o)).ToList();
            return retVal;

        }

        /// <summary>
        /// Obsolete the specified object
        /// </summary>
        public virtual object Obsolete(object key)
        {
            return Activator.CreateInstance(this.Type, this.m_repository.Obsolete((Guid)key));
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
            var results = this.m_repository.Find(query, offset, count, out totalCount);
            return results.AsParallel().Select(o =>
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
            td.Entity = this.m_repository.Save(td.Entity);

            return td;
        }
    }
}
