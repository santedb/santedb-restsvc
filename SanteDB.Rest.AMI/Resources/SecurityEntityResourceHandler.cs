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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
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
    public abstract class SecurityEntityResourceHandler<TSecurityEntity> : IApiResourceHandler, IChainedApiResourceHandler
        where TSecurityEntity : SecurityEntity
    {


        // Property providers
        private ConcurrentDictionary<String, IApiChildResourceHandler> m_propertyProviders = new ConcurrentDictionary<string, IApiChildResourceHandler>();

        // The repository for the entity
        private IRepositoryService<TSecurityEntity> m_repository;

        // Get the tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(SecurityEntityResourceHandler<TSecurityEntity>));

        /// <summary>
        /// Create a new instance of the security entity resource handler
        /// </summary>
        public SecurityEntityResourceHandler()
        {
            ApplicationServiceContext.Current.Started += (o, e) => this.m_repository = ApplicationServiceContext.Current.GetService<IRepositoryService<TSecurityEntity>>();
        }

        /// <summary>
        /// Raise the security attributes change event
        /// </summary>
        protected void FireSecurityAttributesChanged(object scope, bool success, params String[] changedProperties)
        {
            AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, success, changedProperties);
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
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;

        /// <summary>
        /// Get the child resources
        /// </summary>
        public IEnumerable<IApiChildResourceHandler> ChildResources => this.m_propertyProviders.Values;

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
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object Create(object data, bool updateIfExists)
        {

            // First, we want to copy over the roles
            var td = data as ISecurityEntityInfo<TSecurityEntity>;
            if (td is null) throw new ArgumentException("Invalid type", nameof(data));

            try
            {
                // Now for the fun part we want to map any policies over to the wrapped type
                if (td.Entity.Policies != null && td.Policies != null)
                    td.Entity.Policies = td.Policies.Select(p => new SecurityPolicyInstance(p.Policy, p.Grant)).ToList();

                if (updateIfExists)
                {
                    td.Entity = this.GetRepository().Save(td.Entity);
                    AuditUtil.AuditDataAction(EventTypeCodes.SecurityObjectChanged, Core.Auditing.ActionType.Update, Core.Auditing.AuditableObjectLifecycle.Amendment, Core.Auditing.EventIdentifierType.SecurityAlert, Core.Auditing.OutcomeIndicator.Success, null, td.Entity);
                }
                else
                {
                    td.Entity = this.GetRepository().Insert(td.Entity);
                    AuditUtil.AuditDataAction(EventTypeCodes.SecurityObjectChanged, Core.Auditing.ActionType.Create, Core.Auditing.AuditableObjectLifecycle.Creation, Core.Auditing.EventIdentifierType.SecurityAlert, Core.Auditing.OutcomeIndicator.Success, null, td.Entity);
                }

                // Special case for security entity wrappers, we want to load them from DB from fresh
                ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(td.Entity.Key.Value);
                return td;
            }
            catch
            {
                AuditUtil.AuditDataAction<TSecurityEntity>(EventTypeCodes.SecurityObjectChanged, Core.Auditing.ActionType.Create, Core.Auditing.AuditableObjectLifecycle.Creation, Core.Auditing.EventIdentifierType.SecurityAlert, Core.Auditing.OutcomeIndicator.MinorFail, null, td.Entity);
                throw;
            }
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object Get(object id, object versionId)
        {
            // Get the object
            var data = this.GetRepository().Get((Guid)id, (Guid)versionId);

            var retVal = Activator.CreateInstance(this.Type, data) as ISecurityEntityInfo<TSecurityEntity>;
            retVal.Policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicies(data).Select(o => new SecurityPolicyInfo(o)).ToList();
            return retVal;

        }

        /// <summary>
        /// Obsolete the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object Obsolete(object key)
        {
            try
            {
                var retVal = Activator.CreateInstance(this.Type, this.GetRepository().Obsolete((Guid)key));
                AuditUtil.AuditDataAction(EventTypeCodes.SecurityObjectChanged, Core.Auditing.ActionType.Delete, Core.Auditing.AuditableObjectLifecycle.LogicalDeletion, Core.Auditing.EventIdentifierType.SecurityAlert, Core.Auditing.OutcomeIndicator.Success, key.ToString(), retVal);

                // Special case for security entity wrappers, we want to load them from DB from fresh
                ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove((Guid)key);

                return retVal;
            }
            catch
            {
                AuditUtil.AuditDataAction<TSecurityEntity>(EventTypeCodes.SecurityObjectChanged, Core.Auditing.ActionType.Delete, Core.Auditing.AuditableObjectLifecycle.LogicalDeletion, Core.Auditing.EventIdentifierType.SecurityAlert, Core.Auditing.OutcomeIndicator.MinorFail, key.ToString());
                throw;
            }
        }

        /// <summary>
        /// Query for the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Query for specified objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var query = QueryExpressionParser.BuildLinqExpression<TSecurityEntity>(queryParameters);

            List<String> orderBy = null, queryId = null;
            Guid? queryIdParsed = null;
            // Order by
            ModelSort<TSecurityEntity>[] sortParameters = null;
            if (queryParameters.TryGetValue("_orderBy", out orderBy))
                sortParameters = QueryExpressionParser.BuildSort<TSecurityEntity>(orderBy);
            if (queryParameters.TryGetValue("_queryId", out queryId))
                queryIdParsed = Guid.Parse(queryId.First());

            var repo = this.GetRepository();
            IEnumerable<TSecurityEntity> results = null;
            if (repo is IPersistableQueryRepositoryService<TSecurityEntity> && queryIdParsed.HasValue)
                results = (repo as IPersistableQueryRepositoryService<TSecurityEntity>).Find(query, offset, count, out totalCount, queryIdParsed.Value, sortParameters);
            else
                results = repo.Find(query, offset, count, out totalCount, sortParameters);

            return results.Select(o =>
            {
                var r = Activator.CreateInstance(this.Type, o) as ISecurityEntityInfo<TSecurityEntity>;
                r.Policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicies(o).Select(p => new SecurityPolicyInfo(p)).ToList();
                return r;
            }).OfType<Object>();

        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public virtual object Update(object data)
        {
            // First, we want to copy over the roles
            var td = data as ISecurityEntityInfo<TSecurityEntity>;
            if (td is null) throw new ArgumentException("Invalid type", nameof(data));

            try
            {
                // Now for the fun part we want to map any policies over to the wrapped type
                if (td.Policies != null)
                    td.Entity.Policies = td.Policies.Select(p => new SecurityPolicyInstance(p.Policy, p.Grant)).ToList();
                td.Entity = this.GetRepository().Save(td.Entity);

                FireSecurityAttributesChanged(td.Entity, true);

                // Special case for security entity wrappers, we want to load them from DB from fresh
                ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(td.Entity.Key.Value);

                return td;
            }
            catch
            {
                FireSecurityAttributesChanged(td.Entity, false);
                throw;
            }
        }

        /// <summary>
        /// Add a child resource
        /// </summary>
        public void AddChildResource(IApiChildResourceHandler property)
        {
            this.m_propertyProviders.TryAdd(property.ResourceName, property);
        }

        /// <summary>
        /// Remove a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            Guid objectKey = (Guid)scopingEntityKey;

            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Remove(typeof(TSecurityEntity), objectKey, subItemKey);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Query child objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public IEnumerable<object> QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            Guid objectKey = (Guid)scopingEntityKey;
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Query(typeof(TSecurityEntity), objectKey, filter, offset, count, out totalCount);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Add a child object instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            Guid objectKey = (Guid)scopingEntityKey;
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Add(typeof(TSecurityEntity), scopingEntityKey, scopedItem);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }

        /// <summary>
        /// Get a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            Guid objectKey = (Guid)scopingEntity;
            if (this.m_propertyProviders.TryGetValue(propertyName, out IApiChildResourceHandler propertyProvider))
            {
                return propertyProvider.Get(typeof(TSecurityEntity), objectKey, subItemKey);
            }
            else
            {
                throw new KeyNotFoundException($"{propertyName} not found");
            }
        }
    }
}
