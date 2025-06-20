/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;
using System;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a notification template resource handler
    /// </summary>
    public class NotificationTemplateResourceHandler : ResourceHandlerBase<NotificationTemplate>
    {

        private readonly IRepositoryService<NotificationTemplate> m_repositoryService;
        private readonly IRepositoryService<NotificationTemplateParameter> m_templateParameterRepositoryService;
        private readonly IRepositoryService<NotificationTemplateContents> m_templateContentsRepositoryService;
        private readonly IRepositoryService<NotificationInstance> m_notificationInstanceRepositoryService;

        /// <inheritdoc />
        public NotificationTemplateResourceHandler(ILocalizationService localizationService, IRepositoryService<NotificationTemplate> repositoryService, IRepositoryService<NotificationTemplateParameter> templateParameterRepositoryService, IRepositoryService<NotificationTemplateContents> templateContentsRepositoryService, IRepositoryService<NotificationInstance> notificationInstanceRepositoryService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
            this.m_repositoryService = repositoryService;
            this.m_templateParameterRepositoryService = templateParameterRepositoryService;
            this.m_templateContentsRepositoryService = templateContentsRepositoryService;
            this.m_notificationInstanceRepositoryService = notificationInstanceRepositoryService;
        }

        /// <inheritdoc />
        public override object Delete(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            NotificationTemplate template = null;

            if (key is Guid workingKey)
            {
                template = this.m_repositoryService.Get(workingKey);
            }
            else
            {
                throw new ArgumentException(nameof(key));
            }


            if (!template.ObsoletionTime.HasValue && !template.ObsoletedByKey.HasValue)
            {
                // Soft Delete
                return base.Delete(key);
            }

            // Check if the template is in use by any notification instance
            var notificationInstances = this.m_notificationInstanceRepositoryService.Find(o => o.NotificationTemplateKey == workingKey);

            if(notificationInstances.Any())
            {
                throw new InvalidOperationException($"Cannot delete template {template.Key} because it is in use by notification instances.");
            }

            // Permanent Delete
            using (DataPersistenceControlContext.Create(DeleteMode.PermanentDelete))
            {
                template.Parameters.ForEach(templateParameter =>
                {
                    this.m_templateParameterRepositoryService.Delete(templateParameter.Key.Value);
                });

                template.Contents.ForEach(templateContents =>
                {
                    this.m_templateContentsRepositoryService.Delete(templateContents.Key.Value);
                });

                this.m_repositoryService.Delete(workingKey);
            }

            return null;
        }

        /// <summary>
        /// Perform an update
        /// </summary>
        public override object Update(object data)
        {

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var sentTemplate = data as NotificationTemplate;

            if (sentTemplate.Key == null)
            {
                throw new InvalidOperationException("Notification template key must be provided for update.");
            }

            var databaseTemplate = this.m_repositoryService.Get(sentTemplate.Key.Value);

            if (databaseTemplate == null)
            {
                throw new ArgumentException($"Notification template with key {((NotificationTemplate)data).Key} not found");
            }


            var parameters = this.m_templateParameterRepositoryService.Find(o => o.NotificationTemplateKey == ((NotificationTemplate)data).Key).ToList();

            //using (DataPersistenceControlContext.Create(DeleteMode.PermanentDelete))
            //{
            //    parameters.ForEach(param =>
            //    {
            //        this.m_templateParameterRepositoryService.Delete(param.Key.Value);
            //    });
            //}

            return base.Update(data);
        }

        /// <inheritdoc />
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;


    }
}
