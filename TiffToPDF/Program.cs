using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiffToPDF
{
    class Program
    {
        static void Main(string[] args)
        {
            ConvertFirstApproach();
            Compress();
        }

        public static void Compress()
        {
            //Our working folder
            string workingFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            //Name of large PDF to create
            string largePDF = @"C:\Users\Mohd\Documents\visual studio 2013\Projects\TiffToPDF\TiffToPDF\tiff.tif.pdf";
            //Name of compressed PDF to create
            string smallPDF = @"C:\Users\Mohd\Documents\visual studio 2013\Projects\TiffToPDF\TiffToPDF\tiff_small.tif.pdf";


            //Now we're going to open the above PDF and compress things

            //Bind a reader to our large PDF
            PdfReader reader = new PdfReader(largePDF);
            //Create our output PDF
            using (FileStream fs = new FileStream(smallPDF, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                //Bind a stamper to the file and our reader
                using (PdfStamper stamper = new PdfStamper(reader, fs))
                {
                    //NOTE: This code only deals with page 1, you'd want to loop more for your code
                    //Get page 1
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        PdfDictionary page = reader.GetPageN(i);
                        //Get the xobject structure
                        PdfDictionary resources = (PdfDictionary)PdfReader.GetPdfObject(page.Get(PdfName.RESOURCES));
                        PdfDictionary xobject = (PdfDictionary)PdfReader.GetPdfObject(resources.Get(PdfName.XOBJECT));
                        if (xobject != null)
                        {
                            PdfObject obj;
                            //Loop through each key
                            foreach (PdfName name in xobject.Keys)
                            {
                                obj = xobject.Get(name);
                                if (obj.IsIndirect())
                                {
                                    //Get the current key as a PDF object
                                    PdfDictionary imgObject = (PdfDictionary)PdfReader.GetPdfObject(obj);
                                    //See if its an image
                                    if (imgObject.Get(PdfName.SUBTYPE).Equals(PdfName.IMAGE))
                                    {
                                        //NOTE: There's a bunch of different types of filters, I'm only handing the simplest one here which is basically raw JPG, you'll have to research others
                                        if (imgObject.Get(PdfName.FILTER).Equals(PdfName.DCTDECODE))
                                        {
                                            //Get the raw bytes of the current image
                                            byte[] oldBytes = PdfReader.GetStreamBytesRaw((PRStream)imgObject);
                                            //Will hold bytes of the compressed image later
                                            byte[] newBytes;
                                            //Wrap a stream around our original image
                                            using (MemoryStream sourceMS = new MemoryStream(oldBytes))
                                            {
                                                //Convert the bytes into a .Net image
                                                using (System.Drawing.Image oldImage = Bitmap.FromStream(sourceMS))
                                                {
                                                    //Shrink the image to 90% of the original
                                                   
                                                    using (System.Drawing.Image newImage = ShrinkImage(oldImage, 0.7f))
                                                    {
                                                        //Convert the image to bytes using JPG at 85%
                                                        newBytes = ConvertImageToBytes(newImage, 70);
                                                    }
                                                }
                                            }
                                            //Create a new iTextSharp image from our bytes
                                            iTextSharp.text.Image compressedImage = iTextSharp.text.Image.GetInstance(newBytes);
                                            //Kill off the old image
                                            PdfReader.KillIndirect(obj);
                                            //Add our image in its place
                                            stamper.Writer.AddDirectImageSimple(compressedImage, (PRIndirectReference)obj);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }

        }
        //Standard image save code from MSDN, returns a byte array
        private static byte[] ConvertImageToBytes(System.Drawing.Image image, long compressionLevel)
        {
            if (compressionLevel < 0)
            {
                compressionLevel = 0;
            }
            else if (compressionLevel > 100)
            {
                compressionLevel = 100;
            }
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, compressionLevel);
            myEncoderParameters.Param[0] = myEncoderParameter;
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, jgpEncoder, myEncoderParameters);
                return ms.ToArray();
            }

        }
        //standard code from MSDN
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        //Standard high quality thumbnail generation from http://weblogs.asp.net/gunnarpeipman/archive/2009/04/02/resizing-images-without-loss-of-quality.aspx
        private static System.Drawing.Image ShrinkImage(System.Drawing.Image sourceImage, float scaleFactor)
        {
            int newWidth = Convert.ToInt32(sourceImage.Width * scaleFactor);
            int newHeight = Convert.ToInt32(sourceImage.Height * scaleFactor);

            var thumbnailBitmap = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(thumbnailBitmap))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                System.Drawing.Rectangle imageRectangle = new System.Drawing.Rectangle(0, 0, newWidth, newHeight);
                g.DrawImage(sourceImage, imageRectangle);
            }
            return thumbnailBitmap;
        }

        public static void ConvertFirstApproach()
        {
            // creation of the document with a certain size and certain margins
            iTextSharp.text.Document document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 0, 0, 0, 0);

            // creation of the different writers
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, new System.IO.FileStream(@"C:\Users\Mohd\Documents\visual studio 2013\Projects\TiffToPDF\TiffToPDF\tiff.tif.pdf", System.IO.FileMode.Create));

            // load the tiff image and count the total pages
            System.Drawing.Bitmap bm = new System.Drawing.Bitmap(@"C:\Users\Mohd\Documents\visual studio 2013\Projects\TiffToPDF\TiffToPDF\tiff.tif");
            int total = bm.GetFrameCount(System.Drawing.Imaging.FrameDimension.Page);

            document.Open();
            iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;
            for (int k = 0; k < total; ++k)
            {
                bm.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Page, k);

                iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(bm, ImageFormat.Jpeg);
                img.SetAbsolutePosition(0, 0);
                   img.ScaleAbsolute(500, 500);
                //  img.ScalePercent(72f / img.DpiX * 100);
                img.ScaleAbsoluteHeight(document.PageSize.Height);
                img.ScaleAbsoluteWidth(document.PageSize.Width);
            //    img.CompressionLevel = 1;
            //    img.Deflated = true;
                cb.AddImage(img);

                document.NewPage();
            }
            document.Close();
        }

    }

}
