/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-5
 */

using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Matching;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Represents a match operation
    /// </summary>
    public class MatchOperation : IApiChildOperation
    {
        // Matching service
        private IRecordMatchingService m_matchingService;

        // Match configuration
        private IRecordMatchingConfigurationService m_matchConfiguration;

        /// <summary>
        /// Matching service
        /// </summary>
        public MatchOperation(IRecordMatchingService matchingService = null, IRecordMatchingConfigurationService matchConfigService = null)
        {
            this.m_matchingService = matchingService;
            this.m_matchConfiguration = matchConfigService;
        }

        /// <summary>
        /// Gets all the types that this is exposed on
        /// </summary>
        public Type[] ParentTypes => new Type[]
        {
            typeof(Patient),
            typeof(Entity),
            typeof(Provider),
            typeof(Place),
            typeof(Organization),
            typeof(Material),
            typeof(ManufacturedMaterial)
        };

        /// <summary>
        /// Property name
        /// </summary>
        public string Name => "match";

        /// <summary>
        /// Binding for this operation
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Get the match report for the specified object
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (!(scopingKey is Guid uuid) && !Guid.TryParse(scopingKey.ToString(), out uuid))
            {
                throw new ArgumentException(nameof(scopingKey), "Must be UUID");
            }

            if (this.m_matchingService is IMatchReportFactory reportFactory)
            {
                var repoService = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(scopingType)) as IRepositoryService;
                var source = repoService.Get(uuid);
                if (source == null)
                {
                    throw new KeyNotFoundException($"{uuid} not found");
                }

                // key of match configuration
                if (parameters.TryGet<String>("configuration", out String configuration))
                {
                    return reportFactory.CreateMatchReport(scopingType, source, this.m_matchingService.Match(source, configuration, null));
                }
                else
                {
                    return reportFactory.CreateMatchReport(scopingType, source, this.m_matchConfiguration.Configurations.Where(o => o.Metadata.State == MatchConfigurationStatus.Active).SelectMany(c => this.m_matchingService.Match(source, configuration, null)));
                }
            }

            return null;
        }
    }
}