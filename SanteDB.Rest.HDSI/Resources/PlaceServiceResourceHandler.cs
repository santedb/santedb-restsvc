/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Place service resource handler
    /// </summary>
    public class PlaceServiceResourceHandler : HdsiResourceHandlerBase<PlaceService>
    {
        private readonly ISecurityRepositoryService m_securityRepository;
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public PlaceServiceResourceHandler(ILocalizationService localizationService, ISecurityRepositoryService securityRepository, IPolicyEnforcementService pepService, IRepositoryService<PlaceService> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
            this.m_securityRepository = securityRepository;
            this.m_pepService = pepService;
        }

        /// <inheritdoc/>
        public override object Create(object data, bool updateIfExists)
        {
            if (data is PlaceService ps)
            {
                this.DemandAlterPlace(ps);
                return base.Create(data, updateIfExists);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Demands that the user either be associated with the facility in some manner or the user has permission to alter a place service
        /// </summary>
        private void DemandAlterPlace(PlaceService ps)
        {
            // TODO: Allow for the classification of a facility administrative user
            var thisUser = this.m_securityRepository.GetCdrEntity(AuthenticationContext.Current.Principal);
            if (thisUser?.LoadProperty(o => o.Relationships).Any(r => r.RelationshipTypeKey == EntityRelationshipTypeKeys.MaintainedEntity && r.TargetEntityKey == ps.SourceEntityKey) != true)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.WritePlacesAndOrgs);
            }
            
        }

        /// <inheritdoc/>
        public override object Update(object data)
        {
            if (data is PlaceService ps)
            {
                this.DemandAlterPlace(ps);
                return base.Update(data);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }


        /// <inheritdoc/>
        public override object Delete(object key)
        {
            if (key is Guid uuidKey)
            {
                var placeService = this.m_repository.Get(uuidKey);
                this.DemandAlterPlace(placeService);
                return base.Delete(key);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
