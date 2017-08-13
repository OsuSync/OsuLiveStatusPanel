/*
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace OsuLiveStatusPanel
{
    [Serializable]
    public enum BlurType
    {
        Both,
        HorizontalOnly,
        VerticalOnly,
    }

    [Serializable]
    public class GaussianBlur
    {
        private int _radius = 1;
        private int[] _kernel;
        private int _kernelSum;
        private int[,] _multable;
        private BlurType _blurType;

        public GaussianBlur()
        {
            PreCalculateSomeStuff();
        }

        public GaussianBlur(int radius)
        {
            _radius = radius;
            PreCalculateSomeStuff();
        }

        private void PreCalculateSomeStuff()
        {
            int sz = _radius * 2 + 1;
            _kernel = new int[sz];
            _multable = new int[sz, 256];
            for (int i = 1; i <= _radius; i++)
            {
                int szi = _radius - i;
                int szj = _radius + i;
                _kernel[szj] = _kernel[szi] = (szi + 1) * (szi + 1);
                _kernelSum += (_kernel[szj] + _kernel[szi]);
                for (int j = 0; j < 256; j++)
                {
                    _multable[szj, j] = _multable[szi, j] = _kernel[szj] * j;
                }
            }
            _kernel[_radius] = (_radius + 1) * (_radius + 1);
            _kernelSum += _kernel[_radius];
            for (int j = 0; j < 256; j++)
            {
                _multable[_radius, j] = _kernel[_radius] * j;
            }
        }

        public long t1;
        public long t2;
        public long t3;
        public long t4;

        public Bitmap ProcessImage(Image inputImage)
        {
            Bitmap origin = new Bitmap(inputImage);
            Bitmap blurred = new Bitmap(inputImage.Width, inputImage.Height);

            using (RawBitmap src = new RawBitmap(origin))
            {
                using (RawBitmap dest = new RawBitmap(blurred))
                {
                    int pixelCount = src.Width * src.Height;
                    //Stopwatch sw = new Stopwatch();
                    //sw.Start();
                    int[] b = new int[pixelCount];
                    int[] g = new int[pixelCount];
                    int[] r = new int[pixelCount];

                    int[] b2 = new int[pixelCount];
                    int[] g2 = new int[pixelCount];
                    int[] r2 = new int[pixelCount];
                    //sw.Stop();
                    //t1 = sw.ElapsedMilliseconds;

                    int offset = src.GetOffset();
                    int index = 0;
                    unsafe
                    {
                        //sw.Reset();
                        //sw.Start();

                        byte* ptr = src.Begin;
                        for (int i = 0; i < src.Height; i++)
                        {
                            for (int j = 0; j < src.Width; j++)
                            {
                                b[index] = *ptr;
                                ptr++;
                                g[index] = *ptr;
                                ptr++;
                                r[index] = *ptr;
                                ptr++;

                                ++index;
                            }
                            ptr += offset;
                        }

                        //sw.Stop();
                        //t2 = sw.ElapsedMilliseconds;

                        int bsum;
                        int gsum;
                        int rsum;
                        int read;
                        int start = 0;
                        index = 0;

                        //sw.Reset();
                        //sw.Start();

                        if (_blurType != BlurType.VerticalOnly)
                        {
                            for (int i = 0; i < src.Height; i++)
                            {
                                for (int j = 0; j < src.Width; j++)
                                {
                                    bsum = gsum = rsum = 0;
                                    read = index - _radius;

                                    for (int z = 0; z < _kernel.Length; z++)
                                    {
                                        //if (read >= start && read < start + src.Width)
                                        //{
                                        //    bsum += _multable[z, b[read]];
                                        //    gsum += _multable[z, g[read]];
                                        //    rsum += _multable[z, r[read]];
                                        //    sum += _kernel[z];
                                        //}

                                        if (read < start)
                                        {
                                            bsum += _multable[z, b[start]];
                                            gsum += _multable[z, g[start]];
                                            rsum += _multable[z, r[start]];
                                        }
                                        else if (read > start + src.Width - 1)
                                        {
                                            int idx = start + src.Width - 1;
                                            bsum += _multable[z, b[idx]];
                                            gsum += _multable[z, g[idx]];
                                            rsum += _multable[z, r[idx]];
                                        }
                                        else
                                        {
                                            bsum += _multable[z, b[read]];
                                            gsum += _multable[z, g[read]];
                                            rsum += _multable[z, r[read]];
                                        }
                                        ++read;
                                    }

                                    //b2[index] = (bsum / sum);
                                    //g2[index] = (gsum / sum);
                                    //r2[index] = (rsum / sum);

                                    b2[index] = (bsum / _kernelSum);
                                    g2[index] = (gsum / _kernelSum);
                                    r2[index] = (rsum / _kernelSum);

                                    if (_blurType == BlurType.HorizontalOnly)
                                    {
                                        //byte* pcell = dest[j, i];
                                        //*pcell = (byte)(bsum / sum);
                                        //pcell++;
                                        //*pcell = (byte)(gsum / sum);
                                        //pcell++;
                                        //*pcell = (byte)(rsum / sum);
                                        //pcell++;

                                        byte* pcell = dest[j, i];
                                        *pcell = (byte)(bsum / _kernelSum);
                                        pcell++;
                                        *pcell = (byte)(gsum / _kernelSum);
                                        pcell++;
                                        *pcell = (byte)(rsum / _kernelSum);
                                        pcell++;
                                    }

                                    ++index;
                                }
                                start += src.Width;
                            }
                        }
                        if (_blurType == BlurType.HorizontalOnly)
                        {
                            return blurred;
                        }

                        //sw.Stop();
                        //t3 = sw.ElapsedMilliseconds;

                        //sw.Reset();
                        //sw.Start();

                        int tempy;
                        for (int i = 0; i < src.Height; i++)
                        {
                            int y = i - _radius;
                            start = y * src.Width;
                            for (int j = 0; j < src.Width; j++)
                            {
                                bsum = gsum = rsum = 0;
                                read = start + j;
                                tempy = y;
                                for (int z = 0; z < _kernel.Length; z++)
                                {
                                    //if (tempy >= 0 && tempy < src.Height)
                                    //{
                                    //    if (_blurType == BlurType.VerticalOnly)
                                    //    {
                                    //        bsum += _multable[z, b[read]];
                                    //        gsum += _multable[z, g[read]];
                                    //        rsum += _multable[z, r[read]];
                                    //    }
                                    //    else
                                    //    {
                                    //        bsum += _multable[z, b2[read]];
                                    //        gsum += _multable[z, g2[read]];
                                    //        rsum += _multable[z, r2[read]];
                                    //    }
                                    //    sum += _kernel[z];
                                    //}

                                    if (_blurType == BlurType.VerticalOnly)
                                    {
                                        if (tempy < 0)
                                        {
                                            bsum += _multable[z, b[j]];
                                            gsum += _multable[z, g[j]];
                                            rsum += _multable[z, r[j]];
                                        }
                                        else if (tempy > src.Height - 1)
                                        {
                                            int idx = pixelCount - (src.Width - j);
                                            bsum += _multable[z, b[idx]];
                                            gsum += _multable[z, g[idx]];
                                            rsum += _multable[z, r[idx]];
                                        }
                                        else
                                        {
                                            bsum += _multable[z, b[read]];
                                            gsum += _multable[z, g[read]];
                                            rsum += _multable[z, r[read]];
                                        }
                                    }
                                    else
                                    {
                                        if (tempy < 0)
                                        {
                                            bsum += _multable[z, b2[j]];
                                            gsum += _multable[z, g2[j]];
                                            rsum += _multable[z, r2[j]];
                                        }
                                        else if (tempy > src.Height - 1)
                                        {
                                            int idx = pixelCount - (src.Width - j);
                                            bsum += _multable[z, b2[idx]];
                                            gsum += _multable[z, g2[idx]];
                                            rsum += _multable[z, r2[idx]];
                                        }
                                        else
                                        {
                                            bsum += _multable[z, b2[read]];
                                            gsum += _multable[z, g2[read]];
                                            rsum += _multable[z, r2[read]];
                                        }
                                    }


                                    read += src.Width;
                                    ++tempy;
                                }

                                byte* pcell = dest[j, i];

                                //pcell[0] = (byte)(bsum / sum);
                                //pcell[1] = (byte)(gsum / sum);
                                //pcell[2] = (byte)(rsum / sum);

                                pcell[0] = (byte)(bsum / _kernelSum);
                                pcell[1] = (byte)(gsum / _kernelSum);
                                pcell[2] = (byte)(rsum / _kernelSum);
                            }
                        }
                        //sw.Stop();
                        //t4 = sw.ElapsedMilliseconds;
                    }
                }
            }

            return blurred;
        }

        public int Radius
        {
            get { return _radius; }
            set
            {
                if (value < 1)
                {
                    throw new InvalidOperationException("Radius must be greater then 0");
                }
                _radius = value;

            }
        }

        public BlurType BlurType
        {
            get { return _blurType; }
            set
            {
                _blurType = value;
            }
        }
    }

    public unsafe class RawBitmap : IDisposable
    {
        private Bitmap _originBitmap;
        private BitmapData _bitmapData;
        private byte* _begin;

        public RawBitmap(Bitmap originBitmap)
        {
            _originBitmap = originBitmap;
            _bitmapData = _originBitmap.LockBits(new Rectangle(0, 0, _originBitmap.Width, _originBitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            _begin = (byte*)(void*)_bitmapData.Scan0;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _originBitmap.UnlockBits(_bitmapData);
        }

        #endregion

        public unsafe byte* Begin
        {
            get { return _begin; }
        }

        public unsafe byte* this[int x, int y]
        {
            get
            {
                return _begin + y * (_bitmapData.Stride) + x * 3;
            }
        }

        public unsafe byte* this[int x, int y, int offset]
        {
            get
            {
                return _begin + y * (_bitmapData.Stride) + x * 3 + offset;
            }
        }

        //public unsafe void SetColor(int x, int y, int color)
        //{
        //    *(int*)(_begin + y * (_bitmapData.Stride) + x * 3) = color;
        //}

        public int Stride
        {
            get { return _bitmapData.Stride; }
        }

        public int Width
        {
            get { return _bitmapData.Width; }
        }

        public int Height
        {
            get { return _bitmapData.Height; }
        }

        public int GetOffset()
        {
            return _bitmapData.Stride - _bitmapData.Width * 3;
        }

        public Bitmap OriginBitmap
        {
            get { return _originBitmap; }
        }
    }
}
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    public class GaussianBlur
    {
        private readonly int[] _red;
        private readonly int[] _green;
        private readonly int[] _blue;

        private readonly int _width;
        private readonly int _height;

        private readonly ParallelOptions _pOptions = new ParallelOptions { MaxDegreeOfParallelism = 16 };

        public GaussianBlur(Bitmap image)
        {
            var rct = new Rectangle(0, 0, image.Width, image.Height);
            var source = new int[rct.Width * rct.Height];
            var bits = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(bits.Scan0, source, 0, source.Length);
            image.UnlockBits(bits);

            _width = image.Width;
            _height = image.Height;

            _red = new int[_width * _height];
            _green = new int[_width * _height];
            _blue = new int[_width * _height];

            Parallel.For(0, source.Length, _pOptions, i =>
            {
                _red[i] = (source[i] & 0xff0000) >> 16;
                _green[i] = (source[i] & 0x00ff00) >> 8;
                _blue[i] = (source[i] & 0x0000ff);
            });
        }

        public Bitmap Process(int radial)
        {
            var newRed = new int[_width * _height];
            var newGreen = new int[_width * _height];
            var newBlue = new int[_width * _height];
            var dest = new int[_width * _height];

            Parallel.Invoke(
                () => gaussBlur_4(_red, newRed, radial),
                () => gaussBlur_4(_green, newGreen, radial),
                () => gaussBlur_4(_blue, newBlue, radial));

            Parallel.For(0, dest.Length, _pOptions, i =>
            {
                if (newRed[i] > 255) newRed[i] = 255;
                if (newGreen[i] > 255) newGreen[i] = 255;
                if (newBlue[i] > 255) newBlue[i] = 255;

                if (newRed[i] < 0) newRed[i] = 0;
                if (newGreen[i] < 0) newGreen[i] = 0;
                if (newBlue[i] < 0) newBlue[i] = 0;

                dest[i] = (int)(0xff000000u | (uint)(newRed[i] << 16) | (uint)(newGreen[i] << 8) | (uint)newBlue[i]);
            });

            var image = new Bitmap(_width, _height);
            var rct = new Rectangle(0, 0, image.Width, image.Height);
            var bits2 = image.LockBits(rct, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(dest, 0, bits2.Scan0, dest.Length);
            image.UnlockBits(bits2);
            return image;
        }

        private void gaussBlur_4(int[] source, int[] dest, int r)
        {
            var bxs = boxesForGauss(r, 3);
            boxBlur_4(source, dest, _width, _height, (bxs[0] - 1) / 2);
            boxBlur_4(dest, source, _width, _height, (bxs[1] - 1) / 2);
            boxBlur_4(source, dest, _width, _height, (bxs[2] - 1) / 2);
        }

        private int[] boxesForGauss(int sigma, int n)
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
            var wl = (int)Math.Floor(wIdeal);
            if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);

            var sizes = new List<int>();
            for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
            return sizes.ToArray();
        }

        private void boxBlur_4(int[] source, int[] dest, int w, int h, int r)
        {
            for (var i = 0; i < source.Length; i++) dest[i] = source[i];
            boxBlurH_4(dest, source, w, h, r);
            boxBlurT_4(source, dest, w, h, r);
        }

        private void boxBlurH_4(int[] source, int[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, h, _pOptions, i =>
            {
                var ti = i * w;
                var li = ti;
                var ri = ti + r;
                var fv = source[ti];
                var lv = source[ti + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri++] - fv;
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = r + 1; j < w - r; j++)
                {
                    val += source[ri++] - dest[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
                for (var j = w - r; j < w; j++)
                {
                    val += lv - source[li++];
                    dest[ti++] = (int)Math.Round(val * iar);
                }
            });
        }

        private void boxBlurT_4(int[] source, int[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, w, _pOptions, i =>
            {
                var ti = i;
                var li = ti;
                var ri = ti + r * w;
                var fv = source[ti];
                var lv = source[ti + w * (h - 1)];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j * w];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri] - fv;
                    dest[ti] = (int)Math.Round(val * iar);
                    ri += w;
                    ti += w;
                }
                for (var j = r + 1; j < h - r; j++)
                {
                    val += source[ri] - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ri += w;
                    ti += w;
                }
                for (var j = h - r; j < h; j++)
                {
                    val += lv - source[li];
                    dest[ti] = (int)Math.Round(val * iar);
                    li += w;
                    ti += w;
                }
            });
        }
    }
}
