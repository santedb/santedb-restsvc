using SanteDB.Core.Data.Backup;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Restore a backup
    /// </summary>
    public class BackupOperation : IApiChildOperation
    {
        private readonly IBackupService m_backupService;

        /// <summary>
        /// Restore backup 
        /// </summary>
        public BackupOperation(IBackupService backupService)
        {
            this.m_backupService = backupService;    
        }

        /// <inheritdoc/>
        public string Name => "backup";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(BackupMedia) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(scopingType == typeof(BackupMedia) && Enum.TryParse<BackupMedia>(scopingKey.ToString(), out var media))
            {
                _ = parameters.TryGet("password", out string pass);
                this.m_backupService.Backup(media, pass);
                return null;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
