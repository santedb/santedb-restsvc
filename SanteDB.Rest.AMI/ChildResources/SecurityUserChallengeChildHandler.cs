/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
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
 * DatERROR: 2021-8-27
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
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
    public class SecurityUserChallengeChildHandler : IApiChildResourceHandler
    {

        // Challenge service
        private ISecurityChallengeService m_challengeService;

        // Repo service
        private IRepositoryService<SecurityUser> m_repositoryService;

        /// <summary>
        /// Security challenge child handler
        /// </summary>
        public SecurityUserChallengeChildHandler(IRepositoryService<SecurityUser> repositoryService, ISecurityChallengeService securityChallengeService)
        {
            this.m_challengeService = securityChallengeService;
            this.m_repositoryService = repositoryService;
        }

        /// <summary>
        /// Gets the types this child can be attached to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(SecurityUserInfo) };

        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name => "challenge";

        /// <summary>
        /// Gets the type of resource
        /// </summary>
        public Type PropertyType => typeof(SecurityChallenge);

        /// <summary>
        /// Gets the capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Delete;

        /// <summary>
        /// Binding for this operation
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Add the security challenge
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            var securityUser = this.m_repositoryService.Get((Guid)scopingKey);
            if (securityUser == null)
                throw new KeyNotFoundException($"User with key {scopingKey} not found");

            // Add the challenge
            var strongType = (SecurityUserChallengeInfo)item;
            this.m_challengeService.Set(securityUser.UserName, strongType.ChallengeKey, strongType.ChallengeResponse, AuthenticationContext.Current.Principal);
            return null;
        }

        /// <summary>
        /// Get the specified challenge
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            var retVal = this.m_challengeService.Get((Guid)scopingKey, AuthenticationContext.Current.Principal).FirstOrDefault(o => o.Key.Value == (Guid)key);
            if (retVal == null)
                throw new KeyNotFoundException($"Cannot find challenge {key}");
            else return retVal;
        }

        /// <summary>
        /// Get all challenges
        /// </summary>
        public IEnumerable<object> Query(Type scopingType, object scopingKey, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            // Add the challenge
            var retVal = this.m_challengeService.Get((Guid)scopingKey, AuthenticationContext.Current.Principal);
            totalCount = retVal.Count();
            return retVal.Skip(offset).Take(count);
        }

        /// <summary>
        /// Remove the challenge
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            var securityUser = this.m_repositoryService.Get((Guid)scopingKey);
            if (securityUser == null)
                throw new KeyNotFoundException($"User with key {scopingKey} not found");

            // Add the challenge
            this.m_challengeService.Remove(securityUser.UserName, (Guid)key, AuthenticationContext.Current.Principal);
            return null;
        }
    }
}
