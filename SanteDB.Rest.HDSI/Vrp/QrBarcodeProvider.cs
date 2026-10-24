using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
                    //Force to generate at native size, we'll oversample later.
                    //Height = 300,
                    //Width = 300,
                    NoPadding = true,
                    PureBarcode = true,
                },
            };

            using (var bmp = writer.Write(Encoding.UTF8.GetString(rawData)))
            {
                //Perform a 4x oversample for better quality.
                bmp.Mutate(ix => ix.Resize(new ResizeOptions()
                {
                    Size = new SixLabors.ImageSharp.Size(bmp.Width * 4, 0),
                    Mode = ResizeMode.Max,
                    Position = AnchorPositionMode.Center,
                    Sampler = KnownResamplers.NearestNeighbor,
                    Compand = true
                }));
                var retVal = new MemoryStream();
                bmp.Save(retVal, new PngEncoder());
                retVal.Seek(0, SeekOrigin.Begin);
                return retVal;
            }
        }
    }
}
