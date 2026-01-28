/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using System;
using System.Collections.Generic;
using System.Globalization;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Represents an operation to validate a notification instance against its template
    /// </summary>
    public class ValidateNotificationOperation : IApiChildOperation
    {
        private readonly IRepositoryService<NotificationInstance> m_notificationInstanceRepositoryService;
        private readonly IRepositoryService<NotificationTemplate> m_notificationTemplateService;
        private readonly IRepositoryService<NotificationTemplateParameter> m_notificationTemplateParametersService;
        private readonly INotificationTemplateFiller m_notificationTemplateFiller;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ValidateNotificationOperation(IRepositoryService<NotificationInstance> notificationInstanceRepositoryService, IRepositoryService<NotificationTemplate> notificationTemplateService, IRepositoryService<NotificationTemplateParameter> notificationTemplateParametersService, INotificationTemplateRepository notificationTemplateRepository, INotificationTemplateFiller notificationTemplateFiller)
        {
            this.m_notificationInstanceRepositoryService = notificationInstanceRepositoryService;
            this.m_notificationTemplateService = notificationTemplateService;
            this.m_notificationTemplateParametersService = notificationTemplateParametersService;
            this.m_notificationTemplateFiller = notificationTemplateFiller;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new[] { typeof(NotificationInstance) };

        /// <inheritdoc/>
        public string Name => "validate-notification";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (scopingKey == null)
            {
                throw new ArgumentNullException(nameof(scopingKey));
            }

            var instanceId = Guid.Parse(scopingKey.ToString());

            var instance = this.m_notificationInstanceRepositoryService.Get(instanceId);
            var template = this.m_notificationTemplateService.Get(instance.NotificationTemplateKey);

            instance.NotificationTemplate = template;

            if (instance == null)
            {
                throw new KeyNotFoundException($"Notification instance with key {instanceId} not found");
            }

            if (template == null)
            {
                throw new KeyNotFoundException($"Notification template with key {instance.NotificationTemplateKey} not found");
            }

            var model = new Dictionary<string, object>();

            foreach (var parameter in instance.InstanceParameters)
            {
                //var templateParameter = this.m_notificationTemplateParametersService.Get(parameter.TemplateParameterKey);
                model.Add(parameter.ParameterName, parameter.Expression);
            }

            var language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            return this.m_notificationTemplateFiller.FillTemplate(instance, language, model);
        }
    }
}
