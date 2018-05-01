using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sync.Plugins;
using Sync.Tools;
using static OsuLiveStatusPanel.Languages;

namespace OsuLiveStatusPanel.ProcessEvent
{
    class BeatmapPictureOutputProcessRevevices : ProcessRecevierBase
    {
        string bg_output_path;
        private string EnableListenOutputImageFile;
        private string EnableScaleClipOutputImageFile;
        private int Width;
        private int Height;

        public BeatmapPictureOutputProcessRevevices(string output_path,string EnableListenOutputImageFile,string EnableScaleClipOutputImageFile
            ,int Width,int Height)
        {
            bg_output_path = output_path;
            this.EnableListenOutputImageFile = EnableListenOutputImageFile;
            this.EnableScaleClipOutputImageFile = EnableScaleClipOutputImageFile;
            this.Width = Width;
            this.Height = Height;
        }

        public override void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus)
        {
            EventBus.BindEvent<BeatmapChangedProcessEvent>(OnChangeBeatmap);
            EventBus.BindEvent<ClearProcessEvent>(OnClear);
        }

        public void OnClear(ClearProcessEvent e)
        {
            if (File.Exists(bg_output_path))
            {
                File.Delete(bg_output_path);
            }
        }

        public async void OnChangeBeatmap(BeatmapChangedProcessEvent beatmap)
        {
            string osuFileContent = File.ReadAllText(beatmap.Beatmap.OsuFilePath);
            var match = Regex.Match(osuFileContent, @"\""((.+?)\.((jpg)|(png)|(jpeg)))\""", RegexOptions.IgnoreCase);
            string beatmap_folder = Directory.GetParent(beatmap.Beatmap.OsuFilePath).FullName;
            string bgPath = Path.Combine(beatmap_folder , match.Groups[1].Value);

            if (!File.Exists(bgPath))
            {
                IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin::OutputImage]{IMAGE_NOT_FOUND}{bgPath}", ConsoleColor.Yellow);
            }

            await Task.Run(() =>
            {
                try
                {
                    if (EnableScaleClipOutputImageFile == "1")
                    {

                        using (Bitmap bitmap = GetFixedResolutionBitmap(bgPath, Width, Height))
                        using (var fp = File.Open(bg_output_path, FileMode.Create, FileAccess.Write, FileShare.Read))
                            bitmap.Save(fp, ImageFormat.Png);
                    }
                    else
                    {
                        //Copy image file.
                        using (var dst = File.Open(bg_output_path, FileMode.Create, FileAccess.Write, FileShare.Read))
                        using (var src = File.Open(bgPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            src.CopyTo(dst);
                    }
                }
                catch (Exception e)
                {
                    IO.CurrentIO.WriteColor($"[OsuLiveStatusPanelPlugin]{CANT_PROCESS_IMAGE}:{e.Message}", ConsoleColor.Red);
                }
            });
        }
        
        private Bitmap GetFixedResolutionBitmap(string file, int dstw, int dsth)
        {
            float r = dstw / (float)dsth;
            var dbitmap = new Bitmap(dstw, dsth);

            using (var sbitmap = new Bitmap(file))
            {
                float w = 0, h = 0;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                w = sbitmap.Width * r;
                if (w > sbitmap.Width)
                {
                    w = sbitmap.Width;
                    h = sbitmap.Width / r;
                }
                if (h > sbitmap.Height)
                {
                    w = sbitmap.Height * r;
                    h = sbitmap.Height;
                }

                Rectangle rectangle = new Rectangle();
                rectangle.Width = (int)w;
                rectangle.Height = (int)h;
                rectangle.X = (sbitmap.Width - rectangle.Width) / 2;
                rectangle.Y = (sbitmap.Height - rectangle.Height) / 2;

                var sdata = sbitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                var ddata = dbitmap.LockBits(new Rectangle(0, 0, dstw, dsth), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                float scalex = sdata.Width / (float)ddata.Width;
                float scaley = sdata.Height / (float)ddata.Height;

                unsafe
                {
                    byte* sptr = (byte*)(sdata.Scan0);
                    byte* dptr = (byte*)(ddata.Scan0);
                    byte* sp_up, sp_down, sp_left, sp_right;
                    int si = 0, sj = 0;

                    float t;
                    float u, v, omu, omv;
                    Vector4 abcd;
                    Vector4 g1, g2, g3;

                    for (int i = 0; i < ddata.Height; i++, dptr += ddata.Stride - ddata.Width * 3)
                    {
                        t = i * scaley;
                        si = (int)(t);
                        v = t - si;

                        if ((si + 1) == sdata.Height) continue;

                        for (int j = 0; j < ddata.Width; j++, dptr += 3)
                        {
                            t = j * scalex;
                            sj = (int)(t);
                            u = t - sj;
                            if ((sj + 1) == sdata.Width) continue;

                            omu = 1 - u;
                            omv = 1 - v;

                            abcd.X = omu * omv; abcd.Y = u * v; abcd.Z = omu * v; abcd.W = omv * u;

                            sp_up = sptr + ((si - 0) * sdata.Stride + (sj - 0) * 3);//left up 0,0
                            sp_down = sptr + ((si + 1) * sdata.Stride + (sj + 1) * 3);//right down 1,1
                            sp_left = sptr + ((si + 1) * sdata.Stride + (sj - 0) * 3);//left down 0,1
                            sp_right = sptr + ((si - 0) * sdata.Stride + (sj + 1) * 3);//rigth up 1,0

                            g1.X = sp_up[0]; g1.Y = sp_down[0]; g1.Z = sp_left[0]; g1.W = sp_right[0];
                            g2.X = sp_up[1]; g2.Y = sp_down[1]; g2.Z = sp_left[1]; g2.W = sp_right[1];
                            g3.X = sp_up[2]; g3.Y = sp_down[2]; g3.Z = sp_left[2]; g3.W = sp_right[2];

                            dptr[0] = (byte)(Vector4.Dot(g1, abcd));
                            dptr[1] = (byte)(Vector4.Dot(g2, abcd));
                            dptr[2] = (byte)(Vector4.Dot(g3, abcd));
                        }
                    }
                }

                sbitmap.UnlockBits(sdata);
                dbitmap.UnlockBits(ddata);

                stopwatch.Stop();
                IO.CurrentIO.Write($"[OLSP]线性插值:{stopwatch.ElapsedMilliseconds}ms");
            }
            return dbitmap;
        }
    }
}
