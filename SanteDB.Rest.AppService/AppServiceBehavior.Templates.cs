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
 *
 */
using RestSrvr;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for applets
    /// </summary>
    public partial class AppServiceBehavior
    {

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IdentifiedData GetTemplateDefinition(string templateId)
        {
            // First, get the template definition
            var parameters = RestOperationContext.Current.IncomingRequest.QueryString;

            // Add context parameters
            var userEntity = this.m_securityRepositoryService.GetUserEntity(AuthenticationContext.Current.Principal.Identity);
            if (String.IsNullOrEmpty(parameters["userEntityId"]))
            {
                parameters.Add("userEntityId", userEntity?.Key.ToString());
            }
            if (String.IsNullOrEmpty(parameters["facilityId"]))
            {
                // Does the current principal have a facility claim?
                if (AuthenticationContext.Current.Principal is IClaimsPrincipal cp &&
                    cp.TryGetClaimValue(SanteDBClaimTypes.XspaFacilityClaim, out var facilityId))
                {
                    parameters.Add("facilityId", facilityId);
                }
                else
                {
                    parameters.Add("facilityId", this.m_configurationManager.GetSection<
                        SecurityConfigurationSection>().GetSecurityPolicy<Guid>(Core.Configuration.SecurityPolicyIdentification.AssignedFacilityUuid).ToString());
                }
            }

            return this.m_appletManagerService.Applets.GetTemplateInstance(templateId, parameters.ToList().GroupBy(o => o.Key).ToDictionary(o => o.Key, o => o.First().Value));
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public void GetTemplateForm(string templateId)
        {
            var template = this.m_appletManagerService.Applets.GetTemplateDefinition(templateId);
            if (template == null)
            {
                throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = "Template", id = templateId }));
            }
            if (!String.IsNullOrEmpty(template.Form))
            {
                RestOperationContext.Current.OutgoingResponse.Redirect(template.Form);
            }
            
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public List<AppletTemplateDefinition> GetTemplates()
        {
            var query = QueryExpressionParser.BuildLinqExpression<AppletTemplateDefinition>(RestOperationContext.Current.IncomingRequest.QueryString, null, true);
            return this.m_appletManagerService.Applets
                .SelectMany(o => o.Templates)
                .GroupBy(o => o.Mnemonic)
                .Select(o => o.OrderByDescending(t => t.Priority).FirstOrDefault())
                .Where(query.Compile()).ToList();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public void GetTemplateView(string templateId)
        {
            var template = this.m_appletManagerService.Applets.GetTemplateDefinition(templateId);
            if (template == null)
            {
                throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = "Template", id = templateId }));
            }
            if (!String.IsNullOrEmpty(template.View))
            {
                RestOperationContext.Current.OutgoingResponse.Redirect(template.View);
            }
        }

    }
}
