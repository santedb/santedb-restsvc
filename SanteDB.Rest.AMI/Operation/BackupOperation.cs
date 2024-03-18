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
 * User: fyfej
 * Date: 2024-1-29
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
using System.Linq;

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
            if (scopingType == typeof(BackupMedia) && Enum.TryParse<BackupMedia>(scopingKey.ToString(), out var media))
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
                catch (Exception)
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
