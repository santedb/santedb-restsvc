using SanteDB.Core.Interop;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Notifications;
using SanteDB.Core.Notifications.Email;
using SanteDB.Core.Notifications.Templating;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SanteDB.Rest.AMI.Operation
{
    public class TestSendNotificationOperation : IApiChildOperation
    {
        private readonly IRepositoryService<NotificationInstance> m_notificationInstanceRepositoryService;
        private readonly IRepositoryService<NotificationTemplate> m_notificationTemplateService;
        private readonly IRepositoryService<NotificationTemplateParameter> m_notificationTemplateParametersService;
        private readonly IRepositoryService<EntityTelecomAddress> m_entityTelecomAddressRepositoryService;
        private readonly INotificationTemplateRepository m_notificationTemplateRepository;
        private readonly INotificationService m_notificationService;

        public TestSendNotificationOperation(IRepositoryService<NotificationInstance> notificationInstanceRepositoryService, IRepositoryService<NotificationTemplate> notificationTemplateService, IRepositoryService<NotificationTemplateParameter> notificationTemplateParametersService, INotificationTemplateRepository notificationTemplateRepository, IEmailService emailService, INotificationService notificationService, IRepositoryService<EntityTelecomAddress> entityTelecomAddressRepositoryService)
        {
            this.m_notificationInstanceRepositoryService = notificationInstanceRepositoryService;
            this.m_notificationTemplateService = notificationTemplateService;
            this.m_notificationTemplateParametersService = notificationTemplateParametersService;
            this.m_notificationTemplateRepository = notificationTemplateRepository;
            this.m_notificationService = notificationService;
            this.m_entityTelecomAddressRepositoryService = entityTelecomAddressRepositoryService;
        }

        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        public Type[] ParentTypes => new[] { typeof(NotificationInstance) };

        public string Name => "test-send-notification";

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

            var targetExpression = QueryExpressionParser.BuildLinqExpression<EntityTelecomAddress>(instance.TargetExpression);

            var telecomAddress = this.m_entityTelecomAddressRepositoryService.Find(targetExpression).FirstOrDefault()?.IETFValue;

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
                var templateParameter = this.m_notificationTemplateParametersService.Get(parameter.TemplateParameterKey);
                model.Add(templateParameter.Name, parameter.Expression);
            }

            var templateFiller = new SimpleNotificationTemplateFiller(this.m_notificationTemplateRepository);

            var filledTemplate = templateFiller.FillTemplate(instance, CultureInfo.CurrentCulture.TwoLetterISOLanguageName, model);

            this.m_notificationService.SendNotification(new String[]{ telecomAddress }, filledTemplate.Subject, filledTemplate.Body);

            return null;
        }
    }
}
