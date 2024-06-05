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
using SanteDB.Core.Data.Quality;
using SanteDB.Core.Data.Quality.Configuration;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;

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
            if (data is DataQualityRulesetConfiguration dqrc)
            {
                return this.m_dataQualityConfigurationProvider.SaveRuleSet(dqrc);
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(DataQualityRulesetConfiguration), data.GetType()));
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
            return this.m_dataQualityConfigurationProvider.GetRuleSet(id.ToString());
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
