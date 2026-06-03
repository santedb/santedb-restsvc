using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.ChildResources
{
    /// <summary>
    /// Allows for querying of potential mail recipients
    /// </summary>
    public class MailRecipientChildResource : IApiChildResourceHandler
    {
     

        private readonly IRepositoryService<SecurityUser> m_userRepository;
        private readonly IRepositoryService<SecurityRole> m_roleRepository;
        private readonly IRepositoryService<SecurityDevice> m_deviceRepository;

        /// <summary>
        /// DI constructor
        /// </summary>
        public MailRecipientChildResource(
            IRepositoryService<SecurityUser> userRepository, 
            IRepositoryService<SecurityRole> roleRepository, 
            IRepositoryService<SecurityDevice> deviceRepository)
        {
            this.m_userRepository = userRepository;
            this.m_roleRepository = roleRepository;
            this.m_deviceRepository = deviceRepository;
        }

        /// <inheritdoc/>
        public string Name => "_recipientDirectory";

        /// <inheritdoc/>
        public Type PropertyType => typeof(SecurityEntity);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Mailbox) };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if (key is Guid uuid)
            {
                return (SecurityEntity)this.m_userRepository.Get(uuid) ??
                    (SecurityEntity)this.m_roleRepository.Get(uuid) ??
                    this.m_deviceRepository.Get(uuid);
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), key.GetType()));
            }
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            var nameFilter = $"{filter["name"]}%".Substring(1);
            
            var users = this.m_userRepository.Find(o => o.UserName.StartsWith(nameFilter) && o.UserName != "SYSTEM" && o.UserName != "ANONYMOUS").OfType<SecurityEntity>();
            var roles = this.m_roleRepository.Find(o => o.Name.StartsWith(nameFilter) && o.Name != "SYSTEM" && o.Name != "ANONYMOUS" && o.Name != "APPLICATIONS").OfType<SecurityEntity>();
            var devices = this.m_deviceRepository.Find(o => o.Name.StartsWith(nameFilter)  ).OfType<SecurityEntity>();
            return users.Union(roles).Union(devices).AsResultSet();
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();

        }
    }
}
