using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Model.AMI
{
    /// <summary>
    /// Model class extension methods
    /// </summary>
    public static class ExtensionMethods
    {


        /// <summary>
        /// Convert an IPolicy to a policy instance
        /// </summary>
        public static SecurityPolicyInstance ToPolicyInstance(this SecurityPolicyInfo me)
        {
            return new SecurityPolicyInstance(
                me.Policy,
                (PolicyGrantType)(int)me.Grant
            );
        }

    }
}
