using SanteDB.Core.Interop;
using SanteDB.Core.Notifications;
using SanteDB.Core.Services;

namespace SanteDB.Rest.AMI.Resources
{
    public class NotificationResourceHandler : ResourceHandlerBase<Notification>
    {
        /// <inheritdoc />
        public NotificationResourceHandler(ILocalizationService localizationService, IRepositoryService<Notification> repositoryService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <inheritdoc />
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;
    }
}