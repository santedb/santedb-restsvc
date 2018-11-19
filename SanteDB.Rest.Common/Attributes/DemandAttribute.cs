using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common.Attributes
{
    /// <summary>
    /// Indicates a demand for a policy in the local execution environment
    /// </summary>
    public class DemandAttribute : Attribute
    {

        /// <summary>
        /// Creates a new demand attribute
        /// </summary>
        public DemandAttribute(String policyId)
        {
            this.PolicyId = policyId;
        }

        /// <summary>
        /// Gets or sets the policy id
        /// </summary>
        public String PolicyId { get; set; }

    }
}
