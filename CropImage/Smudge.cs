// Smudge class with various functionalities
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

namespace CropImage
{
    public class Smudge
    {
        const int B = 0;
        const int G = 1;
        const int R = 2;
        const int A = 3;
        public static double Distance(System.Windows.Point a, System.Windows.Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static System.Windows.Point Lerp(System.Windows.Point a, System.Windows.Point b, double t)
        {
            double x = a.X + t * (b.X - a.X);
            double y = a.Y + t * (b.Y - a.Y);
            return new System.Windows.Point(x, y);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }

        public static unsafe ImageSource smudge_image(ImageSource image, System.Windows.Point start, System.Windows.Point end, double Radiusd)
        {
            
            byte[] buffer = PCKLIB.ImgUtility.GetBinaryFromWpfBmpSource(image as BitmapSource);
            int width = (int)image.Width;
            int height = (int)image.Height;

            //Bitmap bmp = PCKLIB.ImgUtility.GetWinformBitmapFromBinary(buffer, width, height);
            //bmp.Save("out.png");

            //File.WriteAllText("out.txt", $"{start}->{end}");

            start = new System.Windows.Point((int)(start.X * width), (int)(start.Y * height));
            end = new System.Windows.Point((int)(end.X * width), (int)(end.Y * height));

            /*
            int sx = (int)start.X;
            int sy = (int)start.Y;
            int ex = (int)end.X;
            int ey = (int)end.Y;

            buffer[(sy * width + sx) * 4 + R] = 0xFF;
            buffer[(sy * width + sx) * 4 + G] = 0x0;
            buffer[(sy * width + sx) * 4 + B] = 0x0;
            buffer[(ey * width + ex) * 4 + R] = 0xFF;
            buffer[(ey * width + ex) * 4 + G] = 0x0;
            buffer[(ey * width + ex) * 4 + B] = 0x0;


            var result = PCKLIB.ImgUtility.GetWpfBmpSourceFromBinary(buffer, width, height);
            return result;
            */

            double dist = Distance(start, end);
            double spacing = 1;

            float[] brush;
            float[] strengthmask;            

            int radius = (int)(Radiusd * width);

            int brushwidth = radius * 2;
            int brushheight = radius * 2;
            int brusharea = brushwidth * brushheight;
            strengthmask = new float[brusharea];
            fixed (float* _strmask = strengthmask)
            {
                float* strmask = _strmask;
                for(int i = 0; i < brushheight; i++)
                {
                    for(int j = 0; j < brushwidth; j++)
                    {
                        double ctr = radius;
                        double d = Math.Sqrt((i - ctr) * (i - ctr) + (j - ctr) * (j - ctr));
                        float a = 0; ;
                        if (d < radius)
                            a = (float)(1 - d / radius);                            
                        *strmask = a;
                        ++strmask;
                    }
                }
            }

            brush = new float[brusharea * 4];
            int srcX = (int)start.X - radius;
            int srcY = (int)start.Y - radius;

            fixed (float* brshptrpin = brush)
            {
                float* brshptr = brshptrpin;

                
                for (int y = 0; y < brushheight; ++y)
                {
                    int srcy = Clamp(srcY + y, 0, height - 1);
                    int ptrrow = width * srcy;
                    for (int x = 0; x < brushwidth; ++x)
                    {
                        int srcx = Clamp(srcX + x, 0, width - 1);
                        int ptr = ptrrow + srcx;

                        brshptr[B] = buffer[ptr * 4 + B];
                        brshptr[G] = buffer[ptr * 4 + G];
                        brshptr[R] = buffer[ptr * 4 + R];
                        brshptr[A] = buffer[ptr * 4 + A];
                        /*
                        buffer[ptr * 4 + B] = (byte)(strengthmask[y*brushwidth + x]*255);
                        buffer[ptr * 4 + G] = (byte)(strengthmask[y * brushwidth + x] * 255);
                        buffer[ptr * 4 + R] = (byte)(strengthmask[y * brushwidth + x] * 255);
                        buffer[ptr * 4 + A] = 0xFF;
                        */
                        brshptr += 4;
                    }
                }
            }

           

            for (double f = 0; f < dist; f += spacing)
            {
                System.Windows.Point currentpoint = Lerp(start, end, f / dist);

                int dstX = (int)currentpoint.X - radius;
                int dstY = (int)currentpoint.Y - radius;
                int brushSize = radius * 2;                

                fixed (float* brshpin = brush, _strmask = strengthmask)
                {
                    float* brshptr = brshpin, strmask = _strmask;

                    int maxy = dstY + brushSize;
                    int maxx = dstX + brushSize;
                    for (int y = dstY; y < maxy; ++y)
                    {
                        int dsty = Clamp(y, 0, height - 1);
                        int ptrrow = width * dsty;
                        for (int x = dstX; x < maxx; ++x)
                        {
                            int dstx = Clamp(x, 0, width - 1);
                            int srfcptr = ptrrow + dstx;

                            float invstr = 1.0f - *strmask;

                            //eliminate fringing from 0-alpha
                            //this officially makes us awesomer than Photoshop
                            if (brshptr[A] == 0.0f)
                            {
                                brshptr[B] = buffer[srfcptr * 4 + B];
                                brshptr[G] = buffer[srfcptr * 4 + G];
                                brshptr[R] = buffer[srfcptr * 4 + R];
                            }
                            else if (buffer[srfcptr * 4 + A] == 0)
                            {
                                buffer[srfcptr * 4 + B] = (byte)(brshptr[B]);
                                buffer[srfcptr * 4 + G] = (byte)(brshptr[G]);
                                buffer[srfcptr * 4 + R] = (byte)(brshptr[R]);
                            }

                            //blend the surface into the brush buffer, then copy it back onto the surface
                            brshptr[B] = (brshptr[B] * *strmask + buffer[srfcptr * 4 + B] * invstr);
                            buffer[srfcptr * 4 + B] =  (byte)(brshptr[B] + 0.5f);                            

                            brshptr[G] = (brshptr[G] * *strmask + buffer[srfcptr * 4 + G] * invstr);
                            buffer[srfcptr * 4 + G] =  (byte)(brshptr[G] + 0.5f);

                            brshptr[R] = (brshptr[R] * *strmask + buffer[srfcptr * 4 + R] * invstr);
                            buffer[srfcptr * 4 + R] =  (byte)(brshptr[R] + 0.5f);

                            brshptr[A] = (brshptr[A] * *strmask + buffer[srfcptr * 4 + A] * invstr);
                            buffer[srfcptr * 4 + A] =  (byte)(brshptr[A] + 0.5f);

                            brshptr += 4;
                            ++strmask;
                        }
                    }
                }                
            }

            var result = PCKLIB.ImgUtility.GetWpfBmpSourceFromBinary(buffer, width, height);
            return result;
        }
    }
}
