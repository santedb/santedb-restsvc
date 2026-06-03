using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Notifications.Email;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.HDSI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Mailbox resource handler
    /// </summary>
    public class MailboxResourceHandler : ResourceHandlerBase<Mailbox>
    {

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(MailboxResourceHandler));
        private readonly IMailMessageService m_mailService;

        /// <summary>
        /// DI Ctor
        /// </summary>
        public MailboxResourceHandler(IMailMessageService mailService, ILocalizationService localizationService, IRepositoryService<Mailbox> repositoryService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
            this.m_mailService = mailService;
        }

        /// <inheritdoc/>
        public override string ResourceName => typeof(Mailbox).GetSerializationName();

        /// <inheritdoc/>
        public override Type Type => typeof(Mailbox);

        /// <inheritdoc/>
        public override Type Scope => typeof(IHdsiServiceContract);

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities =>
            ResourceCapabilityType.Create |
            ResourceCapabilityType.Update |
            ResourceCapabilityType.CreateOrUpdate |
            ResourceCapabilityType.Delete |
            ResourceCapabilityType.Get |
            ResourceCapabilityType.Search;

        /// <inheritdoc/>
        public override object Create(object data, bool updateIfExists)
        {
            if (data is Mailbox box)
            {
                if (AuthenticationContext.Current.Principal.IsNonInteractivePrincipal()) // Sync
                {
                    return base.Create(box, updateIfExists);
                }
                else
                {
                    return this.m_mailService.CreateMailbox(box.Name);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Mailbox), data.GetType()));
            }
        }

        /// <inheritdoc/>
        public override object Delete(object key)
        {
            if(key is String str)
            {
                var mbox = this.m_mailService.GetMailboxByName(str);
                if(mbox == null)
                {
                    throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, str));
                }
                return this.m_mailService.DeleteMailbox(mbox.Key.Value);
            }
            else if(key is Guid guid)
            {
                if (AuthenticationContext.Current.Principal.IsNonInteractivePrincipal())
                {
                    return base.Delete(key);
                }
                else
                {
                    return this.m_mailService.DeleteMailbox(guid);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), key.GetType()));
            }
        }

        /// <inheritdoc/>
        public override object Get(object id, object versionId)
        {
            if (AuthenticationContext.Current.Principal.IsNonInteractivePrincipal())
            {
                return base.Get(id, versionId);
            }
            else
            {
                switch (id)
                {
                    case Guid uuid:
                        return this.m_mailService.GetMailbox(uuid) ?? throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, uuid));
                    case String str:
                        if (Guid.TryParse(str, out var uuid2))
                        {
                            return this.m_mailService.GetMailbox(uuid2) ?? throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, uuid2));
                        }
                        else
                        {
                            return this.m_mailService.GetMailboxByName(str) ?? throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, str));
                        }
                    default:
                        throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), id.GetType()));
                }
            }
        }

        /// <inheritdoc/>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            if (AuthenticationContext.Current.Principal.IsNonInteractivePrincipal())
            {
                return base.Query(queryParameters);
            }
            else
            {
                var mailboxQuery = QueryExpressionParser.BuildLinqExpression<Mailbox>(queryParameters);
                return this.m_mailService.GetMailboxes().Where(mailboxQuery);
            }
        }

        /// <inheritdoc/>
        public override object Update(object data)
        {
            if(data is Mailbox mbx)
            {
                if (AuthenticationContext.Current.Principal.IsNonInteractivePrincipal())
                {
                    return base.Update(data);
                }
                else
                {
                    return this.m_mailService.RenameMailbox(mbx.Key.Value, mbx.Name);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Mailbox), data.GetType()));
            }
        }
    }
}
