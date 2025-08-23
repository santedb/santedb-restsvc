/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
using SanteDB.Core.ViewModel.Json;
using SanteDB.Core.Data.Quality;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Templates.Definition;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for applets
    /// </summary>
    public partial class AppServiceBehavior
    {

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IdentifiedData GetTemplateModel(string templateId)
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

            // get the template 
            var tplDef = this.m_dataTemplateManagerService.Find(a => a.Mnemonic == templateId).FirstOrDefault();
            if (tplDef == null)
            {
                throw new KeyNotFoundException($"template/{templateId}");
            }

            var result = tplDef.FillObject(parameters.ToList().ToDictionaryIgnoringDuplicates(o => o.Key, o => o.Value), (o)=>this.m_referenceResolver.ResolveAsString(o));

            if (result is IHasTemplate template) // Correct any type-os in the JSON
            {
                template.Template = new TemplateDefinition() { Key = tplDef.Key, Description = tplDef.Name, Mnemonic = templateId };
                template.TemplateKey = tplDef.Key;
            }

            return result;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public TemplateDefinitionViewModel GetTemplate(string templateId)
        {
            var tpl = this.m_dataTemplateManagerService.Find(o => o.Mnemonic == templateId).FirstOrDefault();
            if (tpl == null)
            {
                throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = "Template", id = templateId }));
            }
            return new TemplateDefinitionViewModel(tpl);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public void GetTemplateForm(string templateId)
        {
            var template = this.GetTemplate(templateId);
            RestOperationContext.Current.OutgoingResponse.Redirect(template.Form);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public List<TemplateDefinitionViewModel> GetTemplates()
        {
            var query = QueryExpressionParser.BuildLinqExpression<TemplateDefinitionViewModel>(RestOperationContext.Current.IncomingRequest.QueryString, null, true);
            return this.m_dataTemplateManagerService.Find(o => o.IsActive).Select(r => new TemplateDefinitionViewModel(r)).Where(query.Compile()).ToList();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public void GetTemplateView(string templateId)
        {
            var template = this.GetTemplate(templateId);
            RestOperationContext.Current.OutgoingResponse.Redirect(template.View);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public Stream GetTemplateDefinition(string templateId)
        {
            var templateDefinition = this.m_dataTemplateManagerService.Find(t => t.Mnemonic == templateId).FirstOrDefault();
            if (templateDefinition == null)
            {
                throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = "Template", id = templateId }));
            }
            else if (templateDefinition.JsonTemplate.ContentType == DataTemplateContentType.reference)
            {
                RestOperationContext.Current.OutgoingResponse.Redirect(templateDefinition.JsonTemplate.Content);
                return null;
            }
            else
            {
                RestOperationContext.Current.OutgoingResponse.ContentType = "text/json";
                return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(templateDefinition.JsonTemplate.Content));
            }
        }

        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public Stream GetTemplateView(String templateId, String viewType)
        {
            if (!Enum.TryParse<DataTemplateViewType>(viewType, out var viewTypeEnum))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, viewType, $"BackEntry, Entry, View, SummaryView"));
            }
            var template = this.m_dataTemplateManagerService.Find(o => o.Mnemonic == templateId).FirstOrDefault();
            if (template == null)
            {
                throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = "Template", id = templateId }));
            }
            else
            {
                var view = template.Views.Find(v => v.ViewType == viewTypeEnum);
                if (view == null)
                {
                    throw new FileNotFoundException($"/Template/{templateId}/view/{viewType}.html");
                }
                else if (view.Content is String str)
                {
                    RestOperationContext.Current.OutgoingResponse.Redirect(str);
                    return null;
                }
                else
                {
                    var memoryStream = new MemoryStream();
                    RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
                    view.Render(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return memoryStream;
                }
            }

        }

    }
}
