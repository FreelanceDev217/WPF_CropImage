// Image Utility class with various functionalities
// 

using System;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using System.Windows;

namespace PCKLIB
{
    public class ImgUtility
    {
        public static BitmapSource GetWpfBmpSourceFromWinformBitmap(System.Drawing.Bitmap bitmap)
        {
           var bitmapData = bitmap.LockBits(
               new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
               System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

           var bitmapSource = BitmapSource.Create(
               bitmapData.Width, bitmapData.Height,
               bitmap.HorizontalResolution, bitmap.VerticalResolution,
               PixelFormats.Bgra32, null,
               bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

           bitmap.UnlockBits(bitmapData);
           return bitmapSource;
        }
        public static System.Drawing.Bitmap GetWinformBitmap(System.Windows.Media.Imaging.BitmapSource srcBitmap)
        {
           using (MemoryStream outStream = new MemoryStream())
           {
               PngBitmapEncoder enc = new PngBitmapEncoder();
               enc.Frames.Add(BitmapFrame.Create(srcBitmap));
               enc.Save(outStream);
               System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

               return new Bitmap(bitmap);
           }
        }

        /// <summary>
        /// Get binary data from bitmap with Format32bppArgb
        /// </summary>
        /// <param name="srcBitmap"></param>
        /// <returns></returns>
        public static byte[] GetBinaryFromWinformBitmap(System.Drawing.Bitmap srcBitmap)
        {
            int w = srcBitmap.Width;
            int h = srcBitmap.Height;
            byte[] bin = new byte[w * h * 4];
            int n = bin.Length;

            BitmapData data = srcBitmap.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var ptr = data.Scan0;
            Marshal.Copy(ptr, bin, 0, n);
            srcBitmap.UnlockBits(data);

            return bin;
        }

        /// <summary>
        /// Get bitmap with format Format32bppArgb from binary data
        /// </summary>
        /// <param name="binaryData">Binary data for the image with Format32bppArgb.</param>
        /// <param name="w">Width of image</param>
        /// <param name="h">Width of height</param>
        /// <returns>Bitmap</returns>
        public static System.Drawing.Bitmap GetWinformBitmapFromBinary(byte[] binaryData, int w, int h)
        {
            int n = w * h * 4;
            if(binaryData.Length < n)
            {
                throw new Exception("Too small data size.");
            }

            System.Drawing.Bitmap b = new Bitmap(w, h);
            BitmapData data = b.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var ptr = data.Scan0;
            Marshal.Copy(binaryData, 0, ptr, n);
            b.UnlockBits(data);

            return b;
        }

        public static byte[] GetBinaryFromWpfBmpSource(System.Windows.Media.Imaging.BitmapSource srcBitmap)
        {
            
            int stride = (srcBitmap.PixelWidth * (srcBitmap.Format.BitsPerPixel / 8));
            byte[] bin = new byte[srcBitmap.PixelHeight * stride];
            srcBitmap.CopyPixels(bin, stride, 0);           
            return bin;
        }

        public static BitmapSource GetWpfBmpSourceFromBinary(byte[] binaryData, int w, int h)
        {
           System.Drawing.Bitmap bmp = GetWinformBitmapFromBinary(binaryData, w, h);
           BitmapSource bmpSource = GetWpfBmpSourceFromWinformBitmap/*GetWpfBmpSourceFromWinformBitmap*/(bmp);
           return bmpSource;
        }
        public static BitmapImage BmpSrc2BmpImg(BitmapSource bmp_src)
        {
           PngBitmapEncoder encoder = new PngBitmapEncoder();
           MemoryStream memoryStream = new MemoryStream();
           BitmapImage bImg = new BitmapImage();

           encoder.Frames.Add(BitmapFrame.Create(bmp_src));
           encoder.Save(memoryStream);

           memoryStream.Position = 0;
           bImg.BeginInit();
           bImg.StreamSource = memoryStream;
           bImg.EndInit();

           memoryStream.Close();
           return bImg;
        }
        
        public static void SaveBitmap(BitmapSource image, string filePath)
        {
           PngBitmapEncoder encoder = new PngBitmapEncoder();
           encoder.Frames.Add(BitmapFrame.Create(image));

           using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
           {
               encoder.Save(fileStream);
               fileStream.Dispose();
           }
        }

        public void SaveVisualUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
           RenderTargetBitmap bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
           bitmap.Render(visual);
           BitmapFrame frame = BitmapFrame.Create(bitmap);
           encoder.Frames.Add(frame);

           using (var stream = File.Create(fileName))
           {
               encoder.Save(stream);
           }
        }

        public void SaveVisualToBmp(FrameworkElement visual, string fileName)
        {
           var encoder = new BmpBitmapEncoder();
           SaveVisualUsingEncoder(visual, fileName, encoder);
        }

        public void SaveVisualToPng(FrameworkElement visual, string fileName)
        {
           var encoder = new PngBitmapEncoder();
           SaveVisualUsingEncoder(visual, fileName, encoder);
        }


        public static void HsvToRgb(double h, double S, double V, ref byte r, ref byte g, ref byte b)
        {
            // ######################################################################
            // T. Nathan Mundhenk
            // mundhenk@usc.edu
            // C/C++ Macro HSV to RGB

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        static byte Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return (byte)i;
        }

        static public BitmapImage LoadImage(string filename, int width = -1, int height = -1, int rot = 0)
        {
           Uri uri = new Uri(filename);
           BitmapImage bmp_ref = new BitmapImage(uri);
           if (width < 0)
           {
               width = bmp_ref.PixelWidth;
               height = bmp_ref.PixelHeight;
           }

           BitmapImage bmp = new BitmapImage();
           bmp.BeginInit();
           bmp.UriSource = uri;

           bmp.DecodePixelHeight = height;
           bmp.DecodePixelWidth = width;

           bmp.Rotation = (Rotation)rot;

           if(rot % 2 == 1)
           {
               bmp.DecodePixelHeight = width;
               bmp.DecodePixelWidth = height;
           }

           bmp.EndInit();
           return bmp;
        }
    }

    public class PixelUtil
    {
        Bitmap source = null;
        IntPtr Iptr = IntPtr.Zero;
        BitmapData bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Pixel marshaling class, use this to get and set pixels rapidly.
        /// </summary>
        /// <param name="source">The Bitmap to work with</param>
        public PixelUtil(Bitmap source)
        {
            this.source = source;
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // Get width and height of bitmap
                Width = source.Width;
                Height = source.Height;

                // get total locked pixels count
                int PixelCount = Width * Height;

                // Create rectangle to lock
                var rect = new Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = System.Drawing.Image.GetPixelFormatSize(source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                             source.PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[PixelCount * step];
                Iptr = bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                // Unlock bitmap data
                source.UnlockBits(bitmapData);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public System.Drawing.Color GetPixel(int x, int y)
        {
            System.Drawing.Color clr = System.Drawing.Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) //For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = System.Drawing.Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) //For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = System.Drawing.Color.FromArgb(r, g, b);
            }
            if (Depth == 8) //For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = System.Drawing.Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, System.Drawing.Color color)
        {
            //Get color components count
            int cCount = Depth / 8;

            //Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (Depth == 32) //For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) //For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8) //For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }
    }
}
