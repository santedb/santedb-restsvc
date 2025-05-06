using SanteDB.Core.Interop;
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;
using System;

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
                    this.m_instanceParameterRepositoryService.Delete((Guid)instanceParameter.Key);
                });

                this.m_repositoryService.Delete((Guid)key);
            }

            return null;
        }

    }
}