using SanteDB.Core.Interop;
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;
using System;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    public class NotificationInstanceResourceHandler : ResourceHandlerBase<NotificationInstance>
    {
        private readonly IRepositoryService<NotificationInstance> m_repositoryService;
        private readonly IRepositoryService<NotificationInstanceParameter> m_instanceParameterRepositoryService;

        /// <inheritdoc />
        public NotificationInstanceResourceHandler(ILocalizationService localizationService, IRepositoryService<NotificationInstance> repositoryService, IRepositoryService<NotificationInstanceParameter> instanceParameterRepositoryService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
            this.m_repositoryService = repositoryService;
            this.m_instanceParameterRepositoryService = instanceParameterRepositoryService;
        }

        /// <inheritdoc />
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;

        public override object Delete(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var instance = this.m_repositoryService.Get((Guid)key);

            if (!instance.ObsoletionTime.HasValue && !instance.ObsoletedByKey.HasValue)
            {
                // Soft Delete
                return base.Delete(key);
            }

            // Hard Delete
            using (DataPersistenceControlContext.Create(DeleteMode.PermanentDelete))
            {
                instance.InstanceParameters.ForEach(instanceParameter =>
                {
                    this.m_instanceParameterRepositoryService.Delete(instanceParameter.Key.Value);
                });

                this.m_repositoryService.Delete((Guid)key);
            }

            return null;
        }

        public override object Update(object data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var sentInstance = data as NotificationInstance;

            var databaseInstance = this.m_repositoryService.Get(sentInstance.Key.Value);

            if (databaseInstance == null)
            {
                throw new ArgumentException($"Notification instance with key {sentInstance.Key} not found");
            }

            //if(databaseInstance.NotificationTemplateKey != sentInstance.NotificationTemplateKey)
            //{
            //    var parameters = this.m_instanceParameterRepositoryService.Find(o => o.NotificationInstanceKey == sentInstance.Key).ToList();

            //    using (DataPersistenceControlContext.Create(DeleteMode.PermanentDelete))
            //    {
            //        parameters.ForEach(instanceParameter =>
            //        {
            //            this.m_instanceParameterRepositoryService.Delete(instanceParameter.Key.Value);
            //        });
            //    }
            //}

            return base.Update(data);
        }
    }
}