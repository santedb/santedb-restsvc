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
using SharpCompress.Compressors.BZip2;
using System.IO;

namespace SanteDB.Rest.Common.Compression
{
    /// <summary>
    /// BZip2 Compression stream
    /// </summary>
    public class BZip2CompressionScheme : ICompressionScheme
    {
        /// <summary>
        /// Get the encoding
        /// </summary>
        public string Encoding
        {
            get
            {
                return "bzip2";
            }
        }

        /// <summary>
        /// Create compression stream
        /// </summary>
        public Stream CreateCompressionStream(Stream underlyingStream)
        {
            return new BZip2Stream(underlyingStream, SharpCompress.Compressors.CompressionMode.Compress, true);
        }

        /// <summary>
        /// Create decompression stream
        /// </summary>
        public Stream CreateDecompressionStream(Stream underlyingStream)
        {
            return new BZip2Stream(underlyingStream, SharpCompress.Compressors.CompressionMode.Decompress, true);

        }
    }
}
