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
using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Notification instance enable/disable operation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ConfigureNotificationInstanceOperation : IApiChildOperation
    {
        private readonly IRepositoryService<NotificationInstance> m_notificationInstanceRepositoryService;
        private readonly IRepositoryService<Concept> m_conceptRepositoryService;

        /// <summary>
        /// Dependency injected constructor
        /// </summary>
        public ConfigureNotificationInstanceOperation(IRepositoryService<NotificationInstance> notificationInstanceRepository, IRepositoryService<Concept> conceptRepositoryService)
        {
            this.m_notificationInstanceRepositoryService = notificationInstanceRepository;
            this.m_conceptRepositoryService = conceptRepositoryService;
        }

        /// <summary>
        /// Gets the scope binding for the object
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Gets the types this applies to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(NotificationInstance) };

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "configure";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (parameters.TryGet("isEnableState", out bool isEnable))
            {
                var instance = this.m_notificationInstanceRepositoryService.Get(Guid.Parse(scopingKey.ToString()));
                var stateMnemonic = isEnable ? "NotificationState-NotYetRun" : "NotificationState-Disabled";
                var stateConcept = this.m_conceptRepositoryService.Find(c => c.Mnemonic == stateMnemonic).FirstOrDefault();
                if (stateConcept == null)
                {
                    throw new NullReferenceException();
                }
                instance.State = stateConcept;
                instance.StateKey = (Guid)stateConcept.Key;
                this.m_notificationInstanceRepositoryService.Save(instance);
                return null;
            }
            else 
            {
                throw new ArgumentNullException("Required parameter 'isEnableState' missing");
            }

        }
    }
}