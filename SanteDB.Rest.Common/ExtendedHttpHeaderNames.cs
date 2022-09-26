/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * Date: 2022-5-30
 */
namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Headers which are used by SanteDB services for special control over its own APIs
    /// </summary>
    public static class ExtendedHttpHeaderNames
    {

        /// <summary>
        /// Extended claims made by the client when authenticating against the services
        /// </summary>
        public const string BasicHttpClientClaimHeaderName = "X-SanteDBClient-Claim";
        /// <summary>
        /// Extended authentication header used by HTTP BASIC to identify the client
        /// </summary>
        public const string BasicHttpClientCredentialHeaderName = "X-SanteDBClient-Authorization";
        /// <summary>
        /// When serializing in view model JSON the name of the view model definition to use
        /// </summary>
        public const string ViewModelHeaderName = "X-SanteDB-ViewModel";
        /// <summary>
        /// When making a request to the configured upstream server
        /// </summary>
        public const string UpstreamHeaderName = "X-SanteDB-Upstream";
        /// <summary>
        /// When deleting, the mode in which data should be removed (LogicalData, PermanentDelete)
        /// </summary>
        public const string DeleteModeHeaderName = "X-SanteDB-DeleteMode";
        /// <summary>
        /// Applet package identifier for HEAD operations
        /// </summary>
        public const string PackageIdentifierHeaderName = "X-SanteDB-PakID";
        /// <summary>
        /// Package hash information for clients to verify the applet content
        /// </summary>
        public const string PackageHashHeaderName = "X-SanteDB-Hash";
        /// <summary>
        /// Magic number expected to be sent to APIs whenever the CDR is running in a restricted client environment
        /// </summary>
        public const string ClientMagicNumberHeaderName = "X-SanteDB-Magic";
        /// <summary>
        /// Force the application of patches - ignoring conflicts
        /// </summary>
        public const string ForceApplyPatchHeaderName = "X-Patch-Force";
        /// <summary>
        /// Device authorization when using HTTP basic auth
        /// </summary>
        public const string HttpDeviceCredentialHeaderName = "X-Device-Authorization";
        /// <summary>
        /// Two-factor authentication secret value in HTTP header.
        /// </summary>
        public const string TfaSecret = "X-SanteDB-TfaSecret";
    }
}
