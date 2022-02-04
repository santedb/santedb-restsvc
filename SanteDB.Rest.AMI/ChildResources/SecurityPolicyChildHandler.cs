/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */

using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// API Child resource handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SecurityPolicyChildHandler : IApiChildResourceHandler
    {
        // Challenge service
        private IPolicyInformationService m_pip;

        // Policy enforcement
        private IPolicyEnforcementService m_pep;

        // Repository service
        private IRepositoryService<SecurityRole> m_roleRepository;

        // Repository service
        private IRepositoryService<SecurityDevice> m_deviceRepository;

        // Repository service
        private IRepositoryService<SecurityApplication> m_applicationRepository;

        /// <summary>
        /// Binding for this operation
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Security challenge child handler
        /// </summary>
        public SecurityPolicyChildHandler(IRepositoryService<SecurityDevice> deviceRepository, IRepositoryService<SecurityApplication> applicationRepository, IRepositoryService<SecurityRole> roleRepository, IPolicyEnforcementService pepService, IPolicyInformationService pipService)
        {
            this.m_pip = pipService;
            this.m_pep = pepService;
            this.m_roleRepository = roleRepository;
            this.m_deviceRepository = deviceRepository;
            this.m_applicationRepository = applicationRepository;
        }

        /// <summary>
        /// Gets the types this child can be attached to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(SecurityRoleInfo), typeof(SecurityDeviceInfo), typeof(SecurityApplicationInfo) };

        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name => "policy";

        /// <summary>
        /// Gets the type of resource
        /// </summary>
        public Type PropertyType => typeof(SecurityPolicyInfo);

        /// <summary>
        /// Gets the capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Search | ResourceCapabilityType.Delete;

        private void DemandFor(Type scopingType)
        {
            switch (scopingType.Name)
            {
                case "SecurityDeviceInfo":
                case "SecurityDevice":
                    this.m_pep.Demand(PermissionPolicyIdentifiers.CreateDevice);
                    break;

                case "SecurityApplicationInfo":
                case "SecurityApplication":
                    this.m_pep.Demand(PermissionPolicyIdentifiers.CreateApplication);
                    break;

                case "SecurityRole":
                case "SecurityRoleInfo":
                    this.m_pep.Demand(PermissionPolicyIdentifiers.AlterRoles);
                    break;

                default:
                    throw new InvalidOperationException("Don't understand this scoping type");
            }
        }

        /// <summary>
        /// Get scope based on type and key
        /// </summary>
        private object GetScope(Type scopingType, object scopingKey)
        {
            switch (scopingType.Name)
            {
                case "SecurityDeviceInfo":
                case "SecurityDevice":
                    return this.m_deviceRepository.Get((Guid)scopingKey);

                case "SecurityApplicationInfo":
                case "SecurityApplication":
                    return this.m_applicationRepository.Get((Guid)scopingKey);

                case "SecurityRole":
                case "SecurityRoleInfo":
                    return this.m_roleRepository.Get((Guid)scopingKey);

                default:
                    throw new InvalidOperationException("Don't understand this scoping type");
            }
        }

        /// <summary>
        /// Add the security challenge
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            // Get scope
            object scope = this.GetScope(scopingType, scopingKey);
            if (scope == null)
                throw new KeyNotFoundException($"Could not find scoped object with identifier {scopingKey}");

            try
            {
                this.DemandFor(scopingType);
                // Get or create the scoped item
                if (item is SecurityPolicy policy)
                    item = new SecurityPolicyInfo(policy);

                var rd = item as SecurityPolicyInfo;
                this.m_pip.AddPolicies(scope, rd.Grant, AuthenticationContext.Current.Principal, rd.Oid);
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, true, $"added policy={rd.Oid}:{rd.Policy}");
                return rd;
            }
            catch
            {
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, false);
                throw;
            }
        }

        /// <summary>
        /// Get the specified challenge
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get all challenges
        /// </summary>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            // Get scope
            object scope = this.GetScope(scopingType, scopingKey);
            if (scope == null)
                throw new KeyNotFoundException($"Could not find scoped object with identifier {scopingKey}");

            var policies = this.m_pip.GetPolicies(scope).OrderBy(o => o.Policy.Oid).Select(o => o.ToPolicyInstance());
            var filterExpression = QueryExpressionParser.BuildLinqExpression<SecurityPolicy>(filter).Compile();
            return new MemoryQueryResultSet(policies.Where(o => filterExpression(o.Policy)).Select(o => new SecurityPolicyInfo(o)));
        }

        /// <summary>
        /// Remove the challenge
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            // Get scope
            object scope = this.GetScope(scopingType, scopingKey);
            if (scope == null)
                throw new KeyNotFoundException($"Could not find scoped object with identifier {scopingKey}");

            var policy = this.m_pip.GetPolicies().FirstOrDefault(o => o.Key == (Guid)key);
            if (policy == null)
                throw new KeyNotFoundException($"Policy {key} not found");

            try
            {
                this.DemandFor(scopingType);
                this.m_pip.RemovePolicies(scope, AuthenticationContext.Current.Principal, policy.Oid);
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, true, $"removed policy={policy.Oid}");
                return null;
            }
            catch
            {
                AuditUtil.AuditSecurityAttributeAction(new object[] { scope }, false, $"removed policy={policy.Oid}");
                throw;
            }
        }
    }
}