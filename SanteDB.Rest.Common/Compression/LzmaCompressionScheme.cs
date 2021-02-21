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
using SharpCompress.Compressors;
using SharpCompress.Compressors.LZMA;
using System.IO;
using SharpCompress.IO;


namespace SanteDB.Rest.Common.Compression
{
    /// <summary>
    /// Compression scheme which uses LZMA
    /// </summary>
    public class LzmaCompressionScheme : ICompressionScheme
    {
        /// <summary>
        /// Get the encoding string
        /// </summary>
        public string Encoding
        {
            get
            {
                return "lzma";
            }
        }

        /// <summary>
        /// Create a compressed stream
        /// </summary>
        public Stream CreateCompressionStream(Stream underlyingStream)
        {
            return new LZipStream(new NonDisposingStream(underlyingStream), CompressionMode.Compress);

        }

        /// <summary>
        /// Create de-compress stream
        /// </summary>
        public Stream CreateDecompressionStream(Stream underlyingStream)
        {
            return new LZipStream(new NonDisposingStream(underlyingStream), CompressionMode.Decompress);
        }
    }
}
