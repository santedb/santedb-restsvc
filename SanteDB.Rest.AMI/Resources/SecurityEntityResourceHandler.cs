/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that wraps a security based entity
    /// </summary>
    /// <typeparam name="TSecurityEntity">The type of security entity being wrapped</typeparam>
    public abstract class SecurityEntityResourceHandler<TSecurityEntity> : ChainedResourceHandlerBase
        where TSecurityEntity : NonVersionedEntityData
    {
        // The repository for the entity
        private IRepositoryService<TSecurityEntity> m_repository;

        // CAche Service
        private IDataCachingService m_cacheService;

        // Policy information service
        protected IPolicyInformationService m_policyInformationService;

        readonly IAuditService _AuditService;

        /// <summary>
        /// Create a new instance of the respository handler
        /// </summary>
        public SecurityEntityResourceHandler(IAuditService auditService, IPolicyInformationService policyInformationService, ILocalizationService localizationService, IDataCachingService cachingService = null, IRepositoryService<TSecurityEntity> repository = null)
            : base(localizationService)

        {
            this.m_cacheService = cachingService;
            this.m_repository = repository;
            this.m_policyInformationService = policyInformationService;
            _AuditService = auditService;
        }

        /// <summary>
        /// Raise the security attributes change event
        /// </summary>
        protected void FireSecurityAttributesChanged(object scope, bool success, params String[] changedProperties)
        {
            _AuditService.Audit().ForSecurityAttributeAction(new object[] { scope }, success, changedProperties).Send();
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public override string ResourceName => typeof(TSecurityEntity).GetCustomAttribute<XmlRootAttribute>().ElementName;

        /// <summary>
        /// Gets the type that this handles
        /// </summary>
        public override Type Type => typeof(TSecurityEntity);

        /// <summary>
        /// Get the wrapped type
        /// </summary>
        protected abstract Type WrapperType { get; }

        /// <summary>
        /// Gets the scope of the object
        /// </summary>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of the resource
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;

        /// <summary>
        /// Gets the repository
        /// </summary>
        protected IRepositoryService<TSecurityEntity> GetRepository()
        {
            if (this.m_repository == null)
            {
                this.m_repository = ApplicationServiceContext.Current.GetService<IRepositoryService<TSecurityEntity>>();
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
        public override object Create(object data, bool updateIfExists)
        {
            // First, we want to copy over the roles
            var td = data as ISecurityEntityInfo<TSecurityEntity>;
            if (td is null)
            {
                this.m_tracer.TraceError($"Invalid type {nameof(data)}");
                throw new ArgumentException(this.LocalizationService.GetString("error.type.ArgumentException", new
                {
                    param = nameof(data)
                }));
            }

            try
            {
                if (updateIfExists)
                {
                    td.Entity = this.GetRepository().Save(td.Entity);
                    _AuditService.Audit().ForEventDataAction(EventTypeCodes.SecurityObjectChanged, Core.Model.Audit.ActionType.Update, Core.Model.Audit.AuditableObjectLifecycle.Amendment, Core.Model.Audit.EventIdentifierType.SecurityAlert, Core.Model.Audit.OutcomeIndicator.Success, null, td.Entity).Send();
                }
                else
                {
                    td.Entity = this.GetRepository().Insert(td.Entity);
                    _AuditService.Audit().ForEventDataAction(EventTypeCodes.SecurityObjectChanged, Core.Model.Audit.ActionType.Create, Core.Model.Audit.AuditableObjectLifecycle.Creation, Core.Model.Audit.EventIdentifierType.SecurityAlert, Core.Model.Audit.OutcomeIndicator.Success, null, td.Entity).Send();
                }

                // Add policies
                if (td.Policies?.Any() == true)
                {
                    foreach (var pol in td.Policies.GroupBy(o => o.Grant))
                    {
                        this.m_policyInformationService.AddPolicies(td.Entity, pol.Key, AuthenticationContext.Current.Principal, pol.Select(o => o.Oid).ToArray());
                    }
                }

                // Special case for security entity wrappers, we want to load them from DB from fresh
                this.m_cacheService?.Remove(td.Entity.Key.Value);

                td.Policies = this.m_policyInformationService.GetPolicies(td.Entity).Select(o => new SecurityPolicyInfo(o)).ToList();
                return td;
            }
            catch
            {
                _AuditService.Audit().ForEventDataAction<TSecurityEntity>(EventTypeCodes.SecurityObjectChanged, Core.Model.Audit.ActionType.Create, Core.Model.Audit.AuditableObjectLifecycle.Creation, Core.Model.Audit.EventIdentifierType.SecurityAlert, Core.Model.Audit.OutcomeIndicator.MinorFail, null, td.Entity).Send();
                throw;
            }
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Get(object id, object versionId)
        {
            // Get the object
            var data = this.GetRepository().Get((Guid)id, (Guid)versionId);

            var retVal = Activator.CreateInstance(this.WrapperType, data) as ISecurityEntityInfo<TSecurityEntity>;
            retVal.Policies = this.m_policyInformationService.GetPolicies(data).Select(o => new SecurityPolicyInfo(o)).ToList();
            return retVal;
        }

        /// <summary>
        /// Obsolete the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Delete(object key)
        {
            try
            {
                var retVal = Activator.CreateInstance(this.WrapperType, this.GetRepository().Delete((Guid)key));
                _AuditService.Audit().ForEventDataAction(EventTypeCodes.SecurityObjectChanged, Core.Model.Audit.ActionType.Delete, Core.Model.Audit.AuditableObjectLifecycle.LogicalDeletion, Core.Model.Audit.EventIdentifierType.SecurityAlert, Core.Model.Audit.OutcomeIndicator.Success, key.ToString(), retVal).Send();

                // Special case for security entity wrappers, we want to load them from DB from fresh
                this.m_cacheService?.Remove((Guid)key);

                return retVal;
            }
            catch
            {
                _AuditService.Audit().ForEventDataAction<TSecurityEntity>(EventTypeCodes.SecurityObjectChanged, Core.Model.Audit.ActionType.Delete, Core.Model.Audit.AuditableObjectLifecycle.LogicalDeletion, Core.Model.Audit.EventIdentifierType.SecurityAlert, Core.Model.Audit.OutcomeIndicator.MinorFail, key.ToString()).Send();
                throw;
            }
        }

        /// <summary>
        /// Query for the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var query = QueryExpressionParser.BuildLinqExpression<TSecurityEntity>(queryParameters);

            try
            {
                return new TransformQueryResultSet<TSecurityEntity, ISecurityEntityInfo<TSecurityEntity>>(this.m_repository.Find(query), (o) =>
                {
                    var r = Activator.CreateInstance(this.WrapperType, o) as ISecurityEntityInfo<TSecurityEntity>;
                    r.Policies = this.m_policyInformationService.GetPolicies(o).Select(p => new SecurityPolicyInfo(p)).ToList();
                    return r;
                });
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error querying security resource {0} - {1}", typeof(TSecurityEntity), e);
                throw new Exception(this.LocalizationService.GetString("error.rest.ami.subscriptionQuery"), e);
            }
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Update(object data)
        {
            // First, we want to copy over the roles
            var td = data as ISecurityEntityInfo<TSecurityEntity>;
            if (td is null)
            {
                this.m_tracer.TraceError($"Invalid type {nameof(data)}");
                throw new ArgumentException(this.LocalizationService.GetString("error.type.ArgumentException", new
                {
                    param = nameof(data)
                }));
            }

            try
            {
                td.Entity = this.GetRepository().Save(td.Entity);

                // Add policies
                if (td.Policies != null)
                {
                    var currentPolicies = this.m_policyInformationService.GetPolicies(td.Entity).Select(o => new { rule = o.Rule, pol = o.Policy.Oid });
                    var newPolicies = td.Policies.Select(p => new { rule = p.Grant, pol = p.Oid });
                    var addedPolicies = newPolicies.Except(currentPolicies);
                    var removedPolicies = currentPolicies.Except(newPolicies);

                    if (addedPolicies.Any() || removedPolicies.Any())
                    {
                        this.m_policyInformationService.RemovePolicies(td.Entity, AuthenticationContext.Current.Principal, removedPolicies.Select(o => o.pol).ToArray());
                        foreach (var pol in addedPolicies.GroupBy(o => o.rule))
                        {
                            this.m_policyInformationService.AddPolicies(td.Entity, pol.Key, AuthenticationContext.Current.Principal, pol.Select(o => o.pol).ToArray());
                        }
                    }
                }

                FireSecurityAttributesChanged(td.Entity, true);

                // Special case for security entity wrappers, we want to load them from DB from fresh
                this.m_cacheService?.Remove(td.Entity.Key.Value);

                td.Policies = this.m_policyInformationService.GetPolicies(td.Entity).Select(o => new SecurityPolicyInfo(o)).ToList();

                return td;
            }
            catch
            {
                FireSecurityAttributesChanged(td.Entity, false);
                throw;
            }
        }


        /// <summary>
        /// Remove a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            return base.RemoveChildObject(scopingEntityKey, propertyName, subItemKey);
        }

        /// <summary>
        /// Query child objects
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override IQueryResultSet QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter)
        {
            return base.QueryChildObjects(scopingEntityKey, propertyName, filter);
        }

        /// <summary>
        /// Add a child object instance
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            return base.AddChildObject(scopingEntityKey, propertyName, scopedItem);
        }

        /// <summary>
        /// Get a child object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object GetChildObject(object scopingEntity, string propertyName, object subItemKey)
        {
            return base.GetChildObject(scopingEntity, propertyName, subItemKey);
        }

    }
}