/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
using RestSrvr;
using SanteDB.Core.Data.Backup;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;

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
            if (scopingType == typeof(BackupMedia) && Enum.TryParse<BackupMedia>(scopingKey.ToString(), out var media) && parameters.TryGet("label", out string label))
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
                catch (Exception e)
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
