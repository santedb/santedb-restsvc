using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Notifications;
using SanteDB.Core.Notifications.Email;
using SanteDB.Core.Notifications.Templating;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;

namespace SanteDB.Rest.AMI.Operation
{
    public class ValidateNotificationOperation : IApiChildOperation
    {
        private readonly IRepositoryService<NotificationInstance> m_notificationInstanceRepositoryService;
        private readonly IRepositoryService<NotificationTemplate> m_notificationTemplateService;
        private readonly IRepositoryService<NotificationTemplateParameter> m_notificationTemplateParametersService;
        private readonly INotificationTemplateRepository m_notificationTemplateRepository;
        private readonly INotificationTemplateFiller m_notificationTemplateFiller;

        public ValidateNotificationOperation(IRepositoryService<NotificationInstance> notificationInstanceRepositoryService, IRepositoryService<NotificationTemplate> notificationTemplateService, IRepositoryService<NotificationTemplateParameter> notificationTemplateParametersService, INotificationTemplateRepository notificationTemplateRepository, INotificationTemplateFiller notificationTemplateFiller)
        {
            this.m_notificationInstanceRepositoryService = notificationInstanceRepositoryService;
            this.m_notificationTemplateService = notificationTemplateService;
            this.m_notificationTemplateParametersService = notificationTemplateParametersService;
            this.m_notificationTemplateRepository = notificationTemplateRepository;
            this.m_notificationTemplateFiller = notificationTemplateFiller;
        }
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        public Type[] ParentTypes => new[] { typeof(NotificationInstance) };

        public string Name => "validate-notification";

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
                var templateParameter = this.m_notificationTemplateParametersService.Get(parameter.TemplateParameterKey);
                model.Add(templateParameter.Name, parameter.Expression);
            }

            var language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;


            return this.m_notificationTemplateFiller.FillTemplate(instance, language, model);
        }
    }
}
