using RestSrvr;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;
using SanteDB.Core.Model.Audit;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Restore a backup
    /// </summary>
    public class RestoreBackupOperation : IApiChildOperation
    {
        private readonly IAuditService m_auditService;
        private readonly IBackupService m_backupService;

        /// <summary>
        /// Restore backup 
        /// </summary>
        public RestoreBackupOperation(IBackupService backupService, IAuditService auditService)
        {
            this.m_auditService = auditService;
            this.m_backupService = backupService;    
        }

        /// <inheritdoc/>
        public string Name => "restore";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(BackupMedia) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(scopingType == typeof(BackupMedia) && Enum.TryParse<BackupMedia>(scopingKey.ToString(), out var media) && parameters.TryGet("label", out string label))
            {
                var audit = this.m_auditService.Audit()
                    .WithAction(Core.Model.Audit.ActionType.Execute)
                    .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Import)
                    .WithEventType("RESTORE_BACKUP", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Restore From Backup")
                    .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                    .WithLocalDestination()
                    .WithPrincipal()
                    .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient())
                    .WithAuditableObjects(new AuditableObject()
                    {
                        IDTypeCode = AuditableObjectIdType.Custom,
                        CustomIdTypeCode = new AuditCode("BACKUPSET", "http://santedb.org/model"),
                        LifecycleType = AuditableObjectLifecycle.Access,
                        ObjectId = label,
                        Role = AuditableObjectRole.JobStream,
                        Type = AuditableObjectType.SystemObject
                    });

                try
                {
                    _ = parameters.TryGet("password", out string password);
                    this.m_backupService.Restore(media, label, password);
                    audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.Success);
                    return null;
                }
                catch(Exception e)
                {
                    audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.EpicFail);
                    throw;
                }
                finally
                {
                    audit.WithTimestamp().Send();
                }
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
