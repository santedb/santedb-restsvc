using RestSrvr.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SanteDB.Rest.WWW
{

    /// <summary>
    /// Represents a WWW service contract
    /// </summary>
    [ServiceContract(Name = "WWW")]
    public interface IWwwServiceContract
    {

        /// <summary>
        /// Get the icon
        /// </summary>
        /// <returns></returns>
        [Get("/favicon.ico")]
        Stream GetIcon();

        /// <summary>
        /// Get specified object
        /// </summary>
        [Get("*")]
        Stream Get();

    }
}
