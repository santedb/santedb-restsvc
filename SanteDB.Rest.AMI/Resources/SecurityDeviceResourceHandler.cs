/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: Justin Fyfe
 * Date: 2019-8-8
 */
using SanteDB.Core;
using SanteDB.Core.Api.Security;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that handles security device operations
    /// </summary>
    public class SecurityDeviceResourceHandler : SecurityEntityResourceHandler<SecurityDevice>, ILockableResourceHandler, IAssociativeResourceHandler
    {
        /// <summary>
        /// Type of security device
        /// </summary>
        public override Type Type => typeof(SecurityDeviceInfo);

        /// <summary>
        /// Create device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Create(object data, bool updateIfExists)
        {

            if (data is SecurityDevice)
                data = new SecurityDeviceInfo(data as SecurityDevice);

            var sde = data as SecurityDeviceInfo;
            // If no policies then assign the ones from DEVICE
            if (sde.Policies == null || sde.Policies.Count == 0 && sde.Entity?.Policies == null || sde.Entity.Policies.Count == 0)
            {
                var role = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>()?.GetRole("DEVICE");
                var policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>()?.GetActivePolicies(role);
                if (policies != null)
                    sde.Policies = policies.Select(o => new SecurityPolicyInfo(o)).ToList();
            }

            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Update the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Update(object data)
        {
            if (data is SecurityDevice)
                data = new SecurityDeviceInfo(data as SecurityDevice);
            return base.Update(data);
        }

        /// <summary>
        /// Obolete the device
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public object Lock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().LockDevice((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = true");
            return retVal;
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public object Unlock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().UnlockDevice((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }

        /// <summary>
        /// Get associated entity
        /// </summary>
        public object GetAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            throw new NotSupportedException("Use root resource getter for property by UUID");
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {

            switch (propertyName)
            {
                case "policy":
                    var scope = this.GetRepository().Get(Guid.Parse(scopingEntityKey.ToString()));
                    if (scope == null)
                        throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingEntityKey}");

                    var policy = scope.Policies.FirstOrDefault(o => o.Policy.Key == Guid.Parse(subItemKey.ToString()));
                    if (policy == null)
                        throw new KeyNotFoundException($"Policy {subItemKey} is not associated with this device");

                    try
                    {
                        ApplicationServiceContext.Current.GetService<IPolicyInformationService>().RemovePolicies(scope, AuthenticationContext.Current.Principal, policy.Policy.Oid);
                        scope.Policies.Remove(policy);
                        var retVal = this.Update(scope);
                        this.FireSecurityAttributesChanged(scope, true, $"del policy={policy.Policy.Oid}");
                        return retVal;
                    }
                    catch
                    {
                        this.FireSecurityAttributesChanged(scope, false, $"del policy={policy.Policy.Oid}");
                        throw;
                    }

                default:
                    throw new ArgumentException($"Property with {propertyName} not valid");
            }
        }

        /// <summary>
        /// Query for associated items
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public IEnumerable<object> QueryAssociatedEntities(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {

            switch (propertyName)
            {
                case "policy":
                    var scope = this.GetRepository().Get(Guid.Parse(scopingEntityKey.ToString()));
                    if (scope == null)
                        throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingEntityKey}");

                    var policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetActivePolicies(scope).OrderBy(o => o.Policy.Oid).Select(o => o.ToPolicyInstance());
                    totalCount = policies.Count();
                    var filterExpression = QueryExpressionParser.BuildLinqExpression<SecurityPolicy>(filter).Compile();
                    return policies.Where(o => filterExpression(o.Policy)).Skip(offset).Take(count).Select(o => new SecurityPolicyInfo(o));
                default:
                    throw new ArgumentException($"Property {propertyName} is not valid for this container");
            }
        }

        /// <summary>
        /// Add an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateDevice)]
        public object AddAssociatedEntity(object scopingEntityKey, string propertyName, object scopedItem)
        {
            switch (propertyName)
            {
                case "policy":
                    var scope = this.GetRepository()?.Get(Guid.Parse(scopingEntityKey.ToString()));
                    if (scope == null)
                        throw new KeyNotFoundException($"Device with key {scopingEntityKey} not found");
                    // Get or create the scoped item
                    if (scopedItem is SecurityPolicy)
                        scopedItem = new SecurityPolicyInfo(scopedItem as SecurityPolicy);

                    var rd = scopedItem as SecurityPolicyInfo;
                    ApplicationServiceContext.Current.GetService<IPolicyInformationService>().AddPolicies(scope, rd.Grant, AuthenticationContext.Current.Principal, rd.Oid);
                    base.FireSecurityAttributesChanged(scope, true, $"{rd.Grant} policy={rd.Oid}");

                    return rd;
                default:
                    throw new KeyNotFoundException($"Property {propertyName} is not valid");
            }
        }
    }
}
