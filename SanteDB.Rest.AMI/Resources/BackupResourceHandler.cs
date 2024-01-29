using SanteDB.Core.Data.Backup;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Backup;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A REST service which can handle interactions with backup dataset
    /// </summary>
    public class BackupResourceHandler : ChainedResourceHandlerBase
    {
        private readonly IBackupService m_backupService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public BackupResourceHandler(IBackupService backupService, ILocalizationService localizationService) : base(localizationService)
        {
            this.m_backupService = backupService;
        }

        /// <inheritdoc/>
        public override string ResourceName => "Backup";

        /// <inheritdoc/>
        public override Type Type => typeof(BackupMedia);

        /// <inheritdoc/>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <inheritdoc/>
        public override object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override object Delete(object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageBackups)]
        public override object Get(object id, object versionId)
        {
            if(Enum.TryParse<BackupMedia>(id.ToString(), out var media))
            {
                return this.m_backupService.GetBackupDescriptors(media).Select(o => new BackupDescriptorInfo(o, media));
            }
            else
            {
                throw new KeyNotFoundException(id.ToString());
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageBackups)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return new MemoryQueryResultSet<BackupMedia>(new BackupMedia[]
            {
                BackupMedia.ExternalPublic,
                BackupMedia.Public,
                BackupMedia.Private
            });
        }

        /// <inheritdoc/>
        public override object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
