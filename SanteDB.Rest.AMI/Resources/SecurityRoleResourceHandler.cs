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
using SanteDB.Core.Security;
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
    /// A resource handler which handles security roles
    /// </summary> 
    public class SecurityRoleResourceHandler : SecurityEntityResourceHandler<SecurityRole>, IAssociativeResourceHandler
    {

        /// <summary>
        /// Get the type
        /// </summary>
        public override Type Type => typeof(SecurityRoleInfo);

        /// <summary>
        /// Add a new associated entity to the specified role
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public object AddAssociatedEntity(object scopingEntityKey, string propertyName, object scopedItem)
        {

            var scope = this.GetRepository().Get((Guid)scopingEntityKey);
            if (scope == null)
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingEntityKey}");

            try
            {
                switch (propertyName)
                {
                    case "policy":
                        {
                            // Get or create the scoped item
                            if (scopedItem is SecurityPolicy)
                                scopedItem = new SecurityPolicyInfo(scopedItem as SecurityPolicy);

                            var rd = scopedItem as SecurityPolicyInfo;  
                            ApplicationServiceContext.Current.GetService<IPolicyInformationService>().AddPolicies(scope, rd.Grant, AuthenticationContext.Current.Principal, rd.Oid);
                            base.FireSecurityAttributesChanged(scope, true, $"{rd.Grant} policy={rd.Oid}");

                            return rd;
                        }
                    case "user":
                        {
                            // Get user entity
                            if (scopedItem is SecurityUser)
                                scopedItem = new SecurityUserInfo(scopedItem as SecurityUser);

                            var rd = scopedItem as SecurityUserInfo;
                            if (!rd.Entity.Key.HasValue)
                                rd.Entity= ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().GetUser(rd.Entity.UserName);
                            if (rd.Entity == null)
                                throw new KeyNotFoundException($"Could not find specified user");
                            ApplicationServiceContext.Current.GetService<IRoleProviderService>().AddUsersToRoles(new string[] { rd.Entity.UserName }, new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                            base.FireSecurityAttributesChanged(scope, true, $"add user={rd.Entity.UserName}");

                            return rd.Entity;
                        }
                    default:
                        throw new KeyNotFoundException($"Invalid association path {propertyName}");
                }
            }
            catch
            {
                base.FireSecurityAttributesChanged(scope, false);
                throw;
            }
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public void AddPropertyHandler(IRestAssociatedPropertyProvider property)
        {
        }

        /// <summary>
        /// Create the specified security role
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateRoles)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityRole)
                data = new SecurityRoleInfo(data as SecurityRole);


            var retVal = base.Create(data, updateIfExists) as SecurityRoleInfo;
            var td = data as SecurityRoleInfo;
            
           
            return new SecurityRoleInfo(retVal.Entity);
        }

        /// <summary>
        /// Get associated entity
        /// </summary>
        public object GetAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            throw new NotSupportedException("Use root resource getter for property by UUID");
        }

        /// <summary>
        /// Obsolete roles
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Query for permissions and policies within the scoped object
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public IEnumerable<object> QueryAssociatedEntities(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            var scope = this.GetRepository().Get(Guid.Parse(scopingEntityKey.ToString()));
            if (scope == null)
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingEntityKey}");

            switch (propertyName)
            {
                case "policy":
                    var policies = ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicies(scope).OrderBy(o => o.Policy.Oid).Select(o => o.ToPolicyInstance());
                    totalCount = policies.Count();
                    var filterExpression = QueryExpressionParser.BuildLinqExpression<SecurityPolicy>(filter).Compile();
                    return policies.Where(o => filterExpression(o.Policy)).Skip(offset).Take(count).Select(o => new SecurityPolicyInfo(o));
                case "user":
                    filter.Add("roles.id", scopingEntityKey.ToString());
                    var expr = QueryExpressionParser.BuildLinqExpression<SecurityUser>(filter);
                    // Could redirect but faster just to query and return
                    return ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Find(expr, offset, count, out totalCount);
                default:
                    throw new ArgumentException($"Property {propertyName} is not valid for this container");
            }
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            var scope = this.GetRepository().Get(Guid.Parse(scopingEntityKey.ToString()));
            if (scope == null)
                throw new KeyNotFoundException($"Could not find SecurityRole with identifier {scopingEntityKey}");

            switch(propertyName)
            {
                case "policy":

                    var policy = scope.Policies.FirstOrDefault(o => o.Policy.Key == Guid.Parse(subItemKey.ToString()));
                    if (policy == null)
                        throw new KeyNotFoundException($"Policy {subItemKey} is not associated with this role");

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
                case "user":
                    var user = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Get(Guid.Parse(subItemKey.ToString()));
                    if (user == null)
                        throw new KeyNotFoundException($"User {subItemKey} not found");
                    try
                    {
                        ApplicationServiceContext.Current.GetService<IRoleProviderService>().RemoveUsersFromRoles(new string[] { user.UserName }, new string[] { scope.Name }, AuthenticationContext.Current.Principal);
                        this.FireSecurityAttributesChanged(scope, true, $"del user={user.UserName}");
                        return user;
                    }
                    catch
                    {
                        this.FireSecurityAttributesChanged(scope, false, $"del user={subItemKey}");
                        throw;
                    }
                default:
                    throw new ArgumentException($"Property with {propertyName} not valid");
            }
        }

        /// <summary>
        /// Update roles
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.AlterRoles)]
        public override object Update(object data)
        {
            if (data is SecurityRole)
                data = new SecurityRoleInfo(data as SecurityRole);
            var td = data as SecurityRoleInfo;

            var retVal = base.Update(data) as SecurityRoleInfo;
            
            return new SecurityRoleInfo(td.Entity);
        }
    }
}
