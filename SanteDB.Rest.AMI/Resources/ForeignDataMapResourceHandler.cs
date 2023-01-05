using SanteDB.Core.Data.Import.Definition;
using SanteDB.Core.Interop;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Foreign data map resource handler
    /// </summary>
    public class ForeignDataMapResourceHandler : ResourceHandlerBase<ForeignDataMap>
    {
        /// <inheritdoc/>
        public ForeignDataMapResourceHandler(ILocalizationService localizationService, IRepositoryService<ForeignDataMap> repositoryService, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, freetextSearchService)
        {
        }

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.GetVersion;


    }
}
