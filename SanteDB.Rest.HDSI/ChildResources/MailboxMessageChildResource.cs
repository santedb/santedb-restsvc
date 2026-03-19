using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Extensions;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.HDSI.ChildResources
{
    /// <summary>
    /// Message children
    /// </summary>
    public class MailboxMessageChildResource : IApiChildResourceHandler
    {

        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(MailboxMessageChildResource));
        private readonly IMailMessageService m_mailMessageService;

        /// <summary>
        /// Mail message service
        /// </summary>
        public MailboxMessageChildResource(IMailMessageService mailMessageService)
        {
            this.m_mailMessageService = mailMessageService;
        }

        /// <inheritdoc/>
        public string Name => typeof(MailMessage).GetSerializationName();

        /// <inheritdoc/>
        public Type PropertyType => typeof(MailboxMailMessage);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Delete;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Mailbox) };

        private Guid GetMailboxKey(object id)
        {
            switch(id)
            {
                case Guid uuid:
                    return uuid;
                case String str:
                    if(Guid.TryParse(str, out var uuid2))
                    {
                        return uuid2;
                    }
                    else
                    {
                        return this.m_mailMessageService.GetMailboxByName(str)?.Key ?? throw new KeyNotFoundException($"{typeof(Mailbox).GetSerializationName()}/{str}");
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public object Get(Type scopingType, object scopingKey, object key)
        {
            var mailbox = this.GetMailboxKey(scopingKey);
            if (key is Guid messageKey)
            {
                return this.m_mailMessageService.GetMailMessage(mailbox, messageKey) ?? throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, $"{typeof(Mailbox).GetSerializationName()}/{scopingKey}/MailMessage/{key}"));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            var mailbox = this.GetMailboxKey(scopingKey);
            var hdsiQuery = QueryExpressionParser.BuildLinqExpression<MailboxMailMessage>(filter);
            return this.m_mailMessageService.GetMessages(mailbox).Where(hdsiQuery);
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            var mailbox = this.GetMailboxKey(scopingKey);
            if(key is Guid messageKey)
            {
                return this.m_mailMessageService.DeleteMessage(mailbox, messageKey);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
        }
    }
}
