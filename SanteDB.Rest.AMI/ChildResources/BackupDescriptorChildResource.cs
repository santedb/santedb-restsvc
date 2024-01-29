using Microsoft.VisualBasic;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Backup;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Represents a child resource for managing backups directly
    /// </summary>
    public class BackupDescriptorChildResource : IApiChildResourceHandler
    {
        private readonly IBackupService m_backupService;

        /// <summary>
        /// DI ctor
        /// </summary>
        public BackupDescriptorChildResource(IBackupService backupService)
        {
            this.m_backupService = backupService;
        }

        /// <inheritdoc/>
        public string Name => "Descriptor";

        /// <inheritdoc/>
        public Type PropertyType => typeof(BackupDescriptorInfo);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Delete;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance | ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(BackupMedia) };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageBackups)]
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if(scopingType == typeof(BackupMedia) && Enum.TryParse<BackupMedia>(scopingKey.ToString(), out var media))
            {
                var backupSet = this.m_backupService.GetBackup(media, key.ToString());
                return new BackupDescriptorInfo(backupSet, media);
            }
            else if (scopingKey.Equals("Any"))
            {
                var backupSet = this.m_backupService.GetBackup(key.ToString(), out media);
                return new BackupDescriptorInfo(backupSet, media);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageBackups)]
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            if (scopingType == typeof(BackupMedia) && Enum.TryParse<BackupMedia>(scopingKey.ToString(), out var media))
            {
                var backupSet = this.m_backupService.GetBackupDescriptors(media);
                if (!String.IsNullOrEmpty(filter["label"]))
                {
                    backupSet = backupSet.Where(k => k.Label.Contains(filter["label"].Replace("~", "")));
                }

                return new MemoryQueryResultSet<BackupDescriptorInfo>(backupSet.Select(o => new BackupDescriptorInfo(o, media)));
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ManageBackups)]
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            if (scopingType == typeof(BackupMedia) && Enum.TryParse<BackupMedia>(scopingKey.ToString(), out var media))
            {
                this.m_backupService.RemoveBackup(media, key.ToString());
                return null;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
