using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZXing;

namespace SanteDB.Rest.HDSI.Vrp
{
    /// <summary>
    /// Generate barcodes based on QR 
    /// </summary>
    public class QrBarcodeProvider : IBarcodeGenerator
    {

        /// <inheritdoc/>
        public string BarcodeAlgorithm => "QR";

        /// <inheritdoc/>
        public Stream Generate(IHasIdentifiers entity, params string[] identityDomains)
        {
            try
            {
                var bcId = entity.Identifiers.Single(o => identityDomains.Contains(o.IdentityDomain.DomainName) && (o.ExpiryDate == null || o.ExpiryDate > DateTimeOffset.Now));

                var barcodeData = bcId.Value;
                if (!String.IsNullOrEmpty(bcId.CheckDigit))
                {
                    barcodeData += $" {bcId.CheckDigit}";
                }
                return this.Generate(Encoding.UTF8.GetBytes(barcodeData));
            }
            catch (Exception e)
            {
                throw new Exception("Cannot generate barcdecode for specified identifier list", e);
            }
        }

        /// <inheritdoc/>
        public Stream Generate(byte[] rawData)
        {
            // Now generate the token
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions()
                {
                    Height = 300,
                    Width = 300,
                    NoPadding = true,
                    PureBarcode = true,
                    Margin = 0
                },
            };

            using (var bmp = writer.Write(Encoding.UTF8.GetString(rawData)))
            {
                var retVal = new MemoryStream();
                bmp.Save(retVal, new PngEncoder());
                retVal.Seek(0, SeekOrigin.Begin);
                return retVal;
            }
        }
    }
}
