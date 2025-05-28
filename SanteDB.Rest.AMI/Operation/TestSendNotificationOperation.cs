using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Notifications;
using SanteDB.Core.Notifications.Email;
using SanteDB.Core.Notifications.RapidPro;
using SanteDB.Core.Notifications.Templating;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SharpCompress;

namespace SanteDB.Rest.AMI.Operation
{
    public class TestSendNotificationOperation : IApiChildOperation
    {
        private readonly IRepositoryService<NotificationInstance> m_notificationInstanceRepositoryService;
        private readonly IRepositoryService<NotificationTemplate> m_notificationTemplateService;
        private readonly IRepositoryService<NotificationTemplateParameter> m_notificationTemplateParametersService;
        private readonly INotificationTemplateRepository m_notificationTemplateRepository;
        private readonly IEmailService m_emailService;

        public TestSendNotificationOperation(IRepositoryService<NotificationInstance> notificationInstanceRepositoryService, IRepositoryService<NotificationTemplate> notificationTemplateService, IRepositoryService<NotificationTemplateParameter> notificationTemplateParametersService, INotificationTemplateRepository notificationTemplateRepository, IEmailService emailService)
        {
            this.m_notificationInstanceRepositoryService = notificationInstanceRepositoryService;
            this.m_notificationTemplateService = notificationTemplateService;
            this.m_notificationTemplateParametersService = notificationTemplateParametersService;
            this.m_notificationTemplateRepository = notificationTemplateRepository;
            this.m_emailService = emailService;
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

            var entityTypeConcept = ApplicationServiceContext.Current.GetService<IRepositoryService<Concept>>().Get(Guid.Parse(parameters.Parameters.FirstOrDefault(c => c.Name == "entityType")?.Value?.ToString()));
            if (entityTypeConcept != null)
            {
                var type = typeof(IdentifiedData).Assembly.ExportedTypes.FirstOrDefault(c => c.GetCustomAttributes<ClassConceptKeyAttribute>().Any(x => x.ClassConcept == entityTypeConcept.Key.ToString()));

                var entityRepositoryService = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(type)) as IRepositoryService;
                var entity = (Entity)entityRepositoryService.Get(Guid.Parse(parameters.Parameters.FirstOrDefault(c => c.Name == "selectedEntity")?.Value?.ToString()));
                //var entityName = entity.Names.FirstOrDefault(c => c.NameUseKey == NameUseKeys.OfficialRecord || NameUseKeys.Assigned);
            }

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

            var filledTemplate = templateFiller.FillTemplate(instance, "en", model);
            var channelTypes = template.Tags.Split(',');

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "CLHM2PFCZS4873C4YLAFARGB4XAJCWDSJHLZSEXH");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Fiddler");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Get the contact list from RapidPro
            var contactList = httpClient.GetAsync("https://app.rapidpro.io/api/v2/contacts.json").GetAwaiter().GetResult();
            var contacts = JObject.Parse(contactList.Content.ReadAsStringAsync().GetAwaiter().GetResult())["results"].ToString();
            var contactsResponse = JsonConvert.DeserializeObject<List<RapidProContact>>(contacts);

            Console.WriteLine(contacts);

            foreach (var channel in channelTypes)
            {
                switch (channel)
                {
                    case "email":
                        var emailMessage = new EmailMessage()
                        {
                            ToAddresses = new List<string>(),
                            FromAddress = "example@example.com",
                            Subject = filledTemplate.Body,
                            Body = filledTemplate.Subject
                        };
                        this.m_emailService.SendEmail(emailMessage);
                        break;
                    case "sms":
                        Console.WriteLine("send a text message");
                        break;
                    case "facebook":
                        var data = new RapidProMessage()
                        {
                            Contact = contactsResponse.Find(c => c.Name == "Jordan Webber").Uuid.ToString(),
                            Text = filledTemplate.Body,
                        };
                
                        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = httpClient.PostAsync("https://app.rapidpro.io/api/v2/messages.json", content).GetAwaiter().GetResult();
                        var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        if (response.IsSuccessStatusCode)
                        {
                            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        }

                        break;
                    default:
                        Console.WriteLine("unknown channel");
                        break;
                }
            }

            return null;
        }
    }
}
