using SanteDB.Core;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// A resource handler which handles security roles
    /// </summary> 
    public class SecurityRoleResourceHandler : SecurityEntityResourceHandler<SecurityRole>
    {

        /// <summary>
        /// Get the type
        /// </summary>
        public override Type Type => typeof(SecurityRoleInfo);

        /// <summary>
        /// Create the specified security role
        /// </summary>
        public override object Create(object data, bool updateIfExists)
        {
            var retVal = base.Create(data, updateIfExists) as SecurityRoleInfo;
            var td = data as SecurityRoleInfo;
            
            if(td.Users.Count > 0)
            {
                ApplicationServiceContext.Current.GetService<ISecurityInformationService>().AddUsersToRoles(td.Users.ToArray(), new string[] { td.Entity.Name });
            }
            return new SecurityRoleInfo(retVal.Entity);
        }

    }
}
