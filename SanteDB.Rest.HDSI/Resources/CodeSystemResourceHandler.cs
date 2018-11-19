using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler for code systems
    /// </summary>
    public class CodeSystemResourceHandler : ResourceHandlerBase<CodeSystem>
    {
        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }
    }
}
