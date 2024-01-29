using RestSrvr;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using SharpCompress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Restore a backup
    /// </summary>
    public class BackupOperation : IApiChildOperation
    {
        private readonly IAuditService m_auditService;
        private readonly IBackupService m_backupService;

        /// <summary>
        /// Restore backup 
        /// </summary>
        public BackupOperation(IBackupService backupService, IAuditService auditService)
        {
            this.m_auditService = auditService;
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
                var audit = this.m_auditService.Audit()
                   .WithAction(Core.Model.Audit.ActionType.Execute)
                   .WithEventIdentifier(Core.Model.Audit.EventIdentifierType.Export)
                   .WithEventType("CREATE_BACKUP", "http://santedb.org/conceptset/SecurityAuditCode#Rest", "Create Backup")
                   .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                   .WithLocalDestination()
                   .WithPrincipal()
                   .WithRemoteSource(RemoteEndpointUtil.Current.GetRemoteClient());

                try
                {
                    _ = parameters.TryGet("password", out string pass);
                    var label = this.m_backupService.Backup(media, pass);
                    audit.WithOutcome(OutcomeIndicator.Success)
                        .WithAuditableObjects(new AuditableObject()
                        {
                            IDTypeCode = AuditableObjectIdType.Custom,
                            CustomIdTypeCode = new AuditCode("BACKUPSET", "http://santedb.org/model"),
                            LifecycleType = AuditableObjectLifecycle.Creation,
                            ObjectId = label.Label,
                            ObjectData = label.Assets.Select(o => new ObjectDataExtension(o.Name, o.AssetClassId.ToByteArray())).ToList(),
                            Role = AuditableObjectRole.JobStream,
                            Type = AuditableObjectType.SystemObject
                        }, new AuditableObject()
                        {
                            IDTypeCode = AuditableObjectIdType.Uri,
                            ObjectId = $"media://{media}",
                            Role = AuditableObjectRole.DataDestination
                        });
                    return null;
                }
                catch(Exception)
                {
                    audit.WithOutcome(OutcomeIndicator.EpicFail);
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
