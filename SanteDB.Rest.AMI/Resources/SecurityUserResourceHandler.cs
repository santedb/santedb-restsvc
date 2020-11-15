/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2019-11-27
 */
using RestSrvr.Exceptions;
using SanteDB.Core;
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
    /// Represents a resource handler that can handle security users
    /// </summary>
    public class SecurityUserResourceHandler : SecurityEntityResourceHandler<SecurityUser>, ILockableResourceHandler, IAssociativeResourceHandler
    {

        /// <summary>
        /// Gets the type of object that is expected
        /// </summary>
        public override Type Type => typeof(SecurityUserInfo);

        /// <summary>
        /// Add an associated entity to this 
        /// </summary>
        public object AddAssociatedEntity(object scopingEntityKey, string propertyName, object scopedItem)
        {
            try
            {
                var securityUser = this.GetRepository().Get((Guid)scopingEntityKey);
                if (securityUser == null)
                    throw new KeyNotFoundException($"User with key {scopingEntityKey} not found");

                switch(propertyName)
                {
                    case "challenge":
                        var challengeSvc = ApplicationServiceContext.Current.GetService<ISecurityChallengeService>();
                        if (challengeSvc == null)
                            throw new InvalidOperationException("Canont find challenge service");

                        // Add the challenge
                        var strongType = (SecurityUserChallengeInfo)scopedItem;
                        challengeSvc.Set(securityUser.UserName, strongType.ChallengeKey, strongType.ChallengeResponse, AuthenticationContext.Current.Principal);
                        return null;
                    default:
                        throw new KeyNotFoundException($"Sub-property {propertyName} not found");
                }
            }
            catch (Exception e)
            {

                throw new Exception($"Error adding associated {propertyName} to {scopingEntityKey}", e);
            }
        }


        /// <summary>
        /// Creates the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.CreateIdentity)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is SecurityUser)
                data = new SecurityUserInfo(data as SecurityUser);
            var td = data as SecurityUserInfo;

            // Don't allow callers to overwrite expiration
            td.Entity.PasswordExpiration = null;
            // Insert the user
            var retVal = base.Create(data, updateIfExists) as SecurityUserInfo;

            // User information to roles
            if (td.Roles.Count > 0)
                ApplicationServiceContext.Current.GetService<IRoleProviderService>().AddUsersToRoles(new string[] { retVal.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);

            return new SecurityUserInfo(retVal.Entity)
            {
                Roles = td.Roles
            };
        }

        /// <summary>
        /// Get a specific associated entity
        /// </summary>
        public object GetAssociatedEntity(object scopingEntity, string propertyName, object subItemKey)
        {
            try
            {
                var securityUser = this.GetRepository().Get((Guid)scopingEntity);
                if (securityUser == null)
                    throw new KeyNotFoundException($"User with key {scopingEntity} not found");

                switch (propertyName)
                {
                    case "challenge":
                        var challengeSvc = ApplicationServiceContext.Current.GetService<ISecurityChallengeService>();
                        if (challengeSvc == null)
                            throw new InvalidOperationException("Canont find challenge service");

                        // Add the challenge
                        var subKey = (Guid)subItemKey;
                        var retVal = challengeSvc.Get(subKey, AuthenticationContext.Current.Principal).FirstOrDefault(o => o.Key.Value == subKey);
                        if (retVal == null)
                            throw new KeyNotFoundException($"Cannot find challenge {subItemKey}");
                        else return retVal;
                    
                    default:
                        throw new KeyNotFoundException($"Sub-property {propertyName} not found");
                }
            }
            catch (Exception e)
            {

                throw new Exception($"Error getting associated {propertyName} to {scopingEntity}", e);
            }
        }

        /// <summary>
        /// Lock the specified user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterIdentity)]
        public object Lock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().LockUser((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = true");
            return retVal;
        }

        /// <summary>
        /// Query for associated entities
        /// </summary>
        public IEnumerable<object> QueryAssociatedEntities(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            try
            {
                var securityUser = this.GetRepository().Get((Guid)scopingEntityKey);
                if (securityUser == null)
                    throw new KeyNotFoundException($"User with key {scopingEntityKey} not found");

                switch (propertyName)
                {
                    case "challenge":
                        var challengeSvc = ApplicationServiceContext.Current.GetService<ISecurityChallengeService>();
                        if (challengeSvc == null)
                            throw new InvalidOperationException("Canont find challenge service");

                        // Add the challenge
                        var subKey = (Guid)scopingEntityKey;
                        var retVal = challengeSvc.Get(subKey, AuthenticationContext.Current.Principal);
                        totalCount = retVal.Count();
                        return retVal.Skip(offset).Take(count);
                    default:
                        throw new KeyNotFoundException($"Sub-property {propertyName} not found");
                }
            }
            catch (Exception e)
            {

                throw new Exception($"Error getting associated {propertyName} to {scopingEntityKey}", e);
            }
        }

        /// <summary>
        /// Removes an associated entity 
        /// </summary>
        public object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            try
            {
                var securityUser = this.GetRepository().Get((Guid)scopingEntityKey);
                if (securityUser == null)
                    throw new KeyNotFoundException($"User with key {scopingEntityKey} not found");

                switch (propertyName)
                {
                    case "challenge":
                        var challengeSvc = ApplicationServiceContext.Current.GetService<ISecurityChallengeService>();
                        if (challengeSvc == null)
                            throw new InvalidOperationException("Canont find challenge service");

                        // Add the challenge
                        var subKey = (Guid)subItemKey;
                        challengeSvc.Remove(securityUser.UserName, subKey, AuthenticationContext.Current.Principal);
                        return null;
                    default:
                        throw new KeyNotFoundException($"Sub-property {propertyName} not found");
                }
            }
            catch (Exception e)
            {

                throw new Exception($"Error removing associated {propertyName} to {scopingEntityKey}", e);
            }
        }

        /// <summary>
        /// Unlock user
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AlterIdentity)]
        public object Unlock(object key)
        {
            ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().UnlockUser((Guid)key);
            var retVal = this.Get(key, Guid.Empty);
            this.FireSecurityAttributesChanged(retVal, true, "Lockout = false");
            return retVal;
        }

        /// <summary>
        /// Override the update function
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginPasswordOnly, true)]
        public override object Update(object data)
        {
            if (data is SecurityUser)
                data = new SecurityUserInfo(data as SecurityUser);

            var td = data as SecurityUserInfo;
            // Don't allow callers to overwrite expiration
            td.Entity.PasswordExpiration = null;

            // Update the user
            if (td.PasswordOnly)
            {
                // Validate that the user name matches the SID
                var user = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityUser>>().Get(td.Entity.Key.Value);
                if (user.UserName?.ToLowerInvariant() != td.Entity.UserName.ToLowerInvariant())
                    throw new FaultException(403, $"Username mismatch expect {user.UserName.ToLowerInvariant()} but got {td.Entity.UserName.ToLowerInvariant()}");

                ApplicationServiceContext.Current.GetService<IIdentityProviderService>().ChangePassword(td.Entity.UserName, td.Entity.Password, AuthenticationContext.Current.Principal);
                this.FireSecurityAttributesChanged(user, true, "Password");
                return null;
            }
            else
            {
                // We're doing a general update, so we have to demand access 
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(PermissionPolicyIdentifiers.LoginAsService);
                td.Entity.Password = null;

                //td.Entity.Roles = td.Roles.Select(o => new SecurityRole() { Name = o }).ToList();
                var retVal = base.Update(data) as SecurityUserInfo;

                // Roles? We want to update
                if (td.Roles != null && td.Roles.Count > 0)
                {
                    var irps = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
                    irps.RemoveUsersFromRoles(new String[] { td.Entity.UserName }, irps.GetAllRoles().Where(o => !td.Roles.Contains(o)).ToArray(), AuthenticationContext.Current.Principal);
                    irps.AddUsersToRoles(new string[] { td.Entity.UserName }, td.Roles.ToArray(), AuthenticationContext.Current.Principal);
                    this.FireSecurityAttributesChanged(retVal.Entity, true, $"Roles = {String.Join(",", td.Roles)}");
                }

                return new SecurityUserInfo(retVal.Entity)
                {
                    Roles = td.Roles
                };

            }

        }
    }
}
