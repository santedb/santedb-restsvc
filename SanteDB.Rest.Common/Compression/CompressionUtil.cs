/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using System;

namespace SanteDB.Rest.Common.Compression
{
    /// <summary>
    /// Compression utilities
    /// </summary>
    public static class CompressionUtil
    {

        // Buffer size
        private const int BUFFER_SIZE = 1024;

        /// <summary>
        /// Get compression scheme
        /// </summary>
        public static ICompressionScheme GetCompressionScheme(String schemeName)
        {
            switch(schemeName)
            {
                case "gzip":
                    return new GzipCompressionScheme();
                case "deflate":
                    return new DeflateCompressionScheme();
                case "lzma":
                    return new LzmaCompressionScheme();
                case "bzip2":
                    return new BZip2CompressionScheme();
                default:
                    return null;
            }
        }
        
    }
}
