using SanteDB.Core.Data.Import;
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
using System.Text;

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
        public override string ResourceName => "alien";

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
            if(data is IDictionary<String, Object> multiPartData)
            {
                if(multiPartData.TryGetValue("name", out var name) &&
                    multiPartData.TryGetValue("map", out var map) &&
                    multiPartData.TryGetValue("source", out var source) &&
                    source is Stream sourceStream)
                {
                    return new ForeignDataInfo(this.m_foreignDataService.Stage(sourceStream, name.ToString(), Guid.Parse(map.ToString())));
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
        public override object Delete(object key)
        {
            if(key is Guid guidKey)
            {
                return new ForeignDataInfo(this.m_foreignDataService.Delete(guidKey));
            }
            else
            {
                throw new ArgumentException(nameof(key));
            }
        }

        /// <inheritdoc/>
        public override object Get(object id, object versionId)
        {
            if(id is Guid guidId)
            {
                return new ForeignDataInfo(this.m_foreignDataService.Get(guidId));
            }
            else
            {
                throw new ArgumentException(nameof(id));
            }
        }

        /// <inheritdoc/>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var query = QueryExpressionParser.BuildLinqExpression<IForeignDataSubmission>(queryParameters);
            return this.m_foreignDataService.Find(query);
        }

        /// <inheritdoc/>
        public override object Update(object data)
        {
            if (data is IDictionary<String, Object> multiPartData)
            {
                if(multiPartData.TryGetValue("id", out var oldId) && Guid.TryParse(oldId.ToString(), out var idGuid))
                {
                    this.Delete(idGuid);
                }
                else
                {
                    throw new ArgumentNullException("Need id for update", nameof(data));
                }

                if (multiPartData.TryGetValue("name", out var name) &&
                    multiPartData.TryGetValue("map", out var map) &&
                    multiPartData.TryGetValue("source", out var source) &&
                    source is Stream sourceStream)
                {
                    return new ForeignDataInfo(this.m_foreignDataService.Stage(sourceStream, name.ToString(), Guid.Parse(map.ToString())));
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
