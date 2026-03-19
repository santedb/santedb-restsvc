using SanteDB.Core;
using SanteDB.Core.i18n;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Mailbox messgae resource handler
    /// </summary>
    public class MailboxMailMessageResourceHandler : ResourceHandlerBase<MailboxMailMessage>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public MailboxMailMessageResourceHandler(ILocalizationService localizationService, IRepositoryService<MailboxMailMessage> repositoryService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
        }


        /// <summary>
        /// Synchronize the mail
        /// </summary>
        public override string ResourceName => typeof(MailboxMailMessage).GetSerializationName();

        /// <inheritdoc/>
        public override Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Create the mail
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override object Create(object data, bool updateIfExists)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Only devices and applications are allowed to sync the mail
        /// </summary>
        private void ThrowIfNotDeviceOrApplication()
        {
            if (!AuthenticationContext.Current.Principal.IsNonInteractivePrincipal())
            {
                throw new InvalidOperationException(ErrorMessages.PRINCIPAL_NOT_APPROPRIATE);
            }
        }

        /// <summary>
        /// Query for mail messages should default to my messages
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Manage mail
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override object Delete(object key)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Delete(key);
        }

        /// <summary>
        /// Update mail message
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ManageMail)]
        public override object Update(object data)
        {
            this.ThrowIfNotDeviceOrApplication();
            return base.Update(data);
        }

        /// <summary>
        /// Get a specific mail message
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }
    }
}
