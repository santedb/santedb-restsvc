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
using SanteDB.Core.Data.Import;
using SanteDB.Core.Http;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Alien;
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

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Foreign data stage resource handler
    /// </summary>
    public class ForeignDataStageHandler : ChainedResourceHandlerBase
    {
        private readonly IForeignDataManagerService m_foreignDataService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ForeignDataStageHandler(ILocalizationService localizationService, IForeignDataManagerService foreignDataManagerService) : base(localizationService)
        {
            this.m_foreignDataService = foreignDataManagerService;
        }

        /// <inheritdoc/>
        public override string ResourceName => "ForeignData";

        /// <inheritdoc/>
        public override Type Type => typeof(IForeignDataSubmission);

        /// <inheritdoc/>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search
            | ResourceCapabilityType.Update;

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageForeignData)]
        public override object Create(object data, bool updateIfExists)
        {
            if (data is IEnumerable<MultiPartFormData> multiPartData)
            {
                var description = multiPartData.FirstOrDefault(o => o.Name == "description");
                var map = multiPartData.FirstOrDefault(o => o.Name == "map");
                var source = multiPartData.FirstOrDefault(o => o.Name == "source");
                var parameters = multiPartData.Where(o => !o.IsFile).ToDictionaryIgnoringDuplicates(o => o.Name, o => o.ToString());

                if (map != null && source != null && source.IsFile)
                {
                    return new ForeignDataInfo(this.m_foreignDataService.Stage(new MemoryStream(source.Data), source.FileName, description.ToString(), Guid.Parse(map.ToString()), parameters));
                }
                else
                {
                    throw new ArgumentException("Expected name, map and source parameters", nameof(data));
                }
            }
            else
            {
                throw new ArgumentException("Expected multipart/form-data", nameof(data));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageForeignData)]
        public override object Delete(object key)
        {
            if (key is Guid guidKey)
            {
                return new ForeignDataInfo(this.m_foreignDataService.Delete(guidKey));
            }
            else
            {
                throw new ArgumentException(nameof(key));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageForeignData)]
        public override object Get(object id, object versionId)
        {
            if (id is Guid guidId)
            {
                return new ForeignDataInfo(this.m_foreignDataService.Get(guidId));
            }
            else
            {
                throw new ArgumentException(nameof(id));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageForeignData)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var query = QueryExpressionParser.BuildLinqExpression<IForeignDataSubmission>(queryParameters);
            return new TransformQueryResultSet<IForeignDataSubmission, ForeignDataInfo>(this.m_foreignDataService.Find(query), (a) => new ForeignDataInfo(a));
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageForeignData)]
        public override object Update(object data)
        {
            if (data is IEnumerable<MultiPartFormData> multiPartData)
            {
                var description = multiPartData.FirstOrDefault(o => o.Name == "description");
                var map = multiPartData.FirstOrDefault(o => o.Name == "map");
                var source = multiPartData.FirstOrDefault(o => o.Name == "source");
                var id = multiPartData.FirstOrDefault(o => o.Name == "id");
                var parameters = multiPartData.Where(o => !o.IsFile).ToDictionaryIgnoringDuplicates(o => o.Name, o => o.ToString());
                if (Guid.TryParse(id?.ToString(), out var idGuid))
                {
                    this.Delete(idGuid);
                }
                else
                {
                    throw new ArgumentNullException("Need id for update", nameof(data));
                }

                if (map != null && source != null && source.IsFile)
                {
                    return new ForeignDataInfo(this.m_foreignDataService.Stage(new MemoryStream(source.Data), source.FileName, description.ToString(), Guid.Parse(map.ToString()), parameters));
                }
                else
                {
                    throw new ArgumentException("Expected name, map and source parameters", nameof(data));
                }
            }
            else
            {
                throw new ArgumentException("Expected multipart/form-data", nameof(data));
            }
        }
    }
}
