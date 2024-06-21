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
 * User: fyfej
 * Date: 2024-1-5
 */
using RestSrvr;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Cdss;
using SanteDB.Core.Data.Quality;
using SanteDB.Core.Data.Quality.Configuration;
using SanteDB.Core.Http;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Data quality resource handler
    /// </summary>
    public class DataQualityResourceHandler : ChainedResourceHandlerBase
    {
        // Data quality service
        private readonly IDataQualityConfigurationProviderService m_dataQualityConfigurationProvider;

        /// <summary>
        /// DI ctor
        /// </summary>
        public DataQualityResourceHandler(IDataQualityConfigurationProviderService dataQualityConfigurationProviderService, ILocalizationService localizationService) : base(localizationService)
        {
            this.m_dataQualityConfigurationProvider = dataQualityConfigurationProviderService;
        }

        /// <inheritdoc/>
        public override string ResourceName => "DataQualityRulesetConfiguration";

        /// <inheritdoc/>
        public override Type Type => typeof(DataQualityRulesetConfiguration);

        /// <inheritdoc/>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create
            | ResourceCapabilityType.CreateOrUpdate
            | ResourceCapabilityType.Update
            | ResourceCapabilityType.Delete
            | ResourceCapabilityType.Get
            | ResourceCapabilityType.Search;

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public override object Create(object data, bool updateIfExists)
        {
            switch(data)
            {
                case DataQualityRulesetConfiguration dqrc:
                    return this.m_dataQualityConfigurationProvider.SaveRuleSet(dqrc);
                case Stream str:
                    return this.m_dataQualityConfigurationProvider.SaveRuleSet(DataQualityRulesetConfiguration.Load(str));
                    break;
                case IEnumerable<MultiPartFormData> multiPartData:
                    var source = multiPartData.FirstOrDefault(o => o.Name == "ruleset");
                    if (source?.IsFile == true)
                    {
                        using (var ms = new MemoryStream(source.Data))
                        {
                            return this.m_dataQualityConfigurationProvider.SaveRuleSet(DataQualityRulesetConfiguration.Load(ms));
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Expected a ruleset form value", nameof(data));
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
           
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public override object Delete(object key)
        {
            this.m_dataQualityConfigurationProvider.RemoveRuleSet(key.ToString());
            return null;
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {

            var library = this.m_dataQualityConfigurationProvider.GetRuleSet(id.ToString());
            switch (RestOperationContext.Current.IncomingRequest.QueryString["_format"])
            {
                case "xml":
                    RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment;filename=\"{id}.xml\"");
                    RestOperationContext.Current.OutgoingResponse.ContentType = "application/xml";
                    return library;
                default:
                    return library;
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var allResults = this.m_dataQualityConfigurationProvider.GetRuleSets();
            var filter = QueryExpressionParser.BuildLinqExpression<DataQualityRulesetConfiguration>(queryParameters);
            return new MemoryQueryResultSet<DataQualityRulesetConfiguration>(allResults).Where(filter);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public override object Update(object data)
        {
            if (data is DataQualityRulesetConfiguration dqrc)
            {
                return this.m_dataQualityConfigurationProvider.SaveRuleSet(dqrc);
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(DataQualityRulesetConfiguration), data.GetType()));
            }
        }
    }
}
