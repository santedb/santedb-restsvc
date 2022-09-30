using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Rest.Common.Configuration.Interop
{
    /// <summary>
    /// A tool which can bind certificates to an IP address
    /// </summary>
    public interface ISslCertificateBinder
    {

        /// <summary>
        /// Gets the platform on which this binder operates
        /// </summary>
        PlatformID Platform { get; }

        /// <summary>
        /// Unbind the certificate to the specified <paramref name="ipAddress"/>
        /// </summary>
        /// <param name="ipAddress">The IP address for the binding</param>
        /// <param name="port">The port where the certificate should be bound</param>
        /// <param name="hash">The certificate to bind</param>
        /// <param name="storeLocation">The location of the certificate store</param>
        /// <param name="storeName">The name of the certificate store</param>
        void UnbindCertificate(IPAddress ipAddress, int port, byte[] hash, StoreName storeName, StoreLocation storeLocation);

        /// <summary>
        /// Bind the specificed <paramref name="hash"/> to <paramref name="ipAddress"/>
        /// </summary>
        /// <param name="ipAddress">The IP address where the binding should be set</param>
        /// <param name="port">The port of the binding</param>
        /// <param name="hash">The certificate to bind</param>
        /// <param name="storeLocation">The location of the certificate store</param>
        /// <param name="storeName">The name of the certificate store</param>
        void BindCertificate(IPAddress ipAddress, int port, byte[] hash, StoreName storeName, StoreLocation storeLocation);
    }
}
