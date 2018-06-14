using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace OsuLiveStatusPanel
{
    public class ModsPictureGenerator
    {
        private static Dictionary<string, string> ModsFileNameTranslateMap { get; set; } =
            new Dictionary<string, string>(){
                //{ "None" , "" },
                {"NoFail" , "selection-mod-nofail" },
                {"Easy" , "selection-mod-easy" },
                {"TouchScreen", "TS" },
                {"Hidden","selection-mod-hidden" },
                {"HardRock" , "selection-mod-hardrock" },
                {"SuddenDeath" , "selection-mod-suddendeath" },
                {"DoubleTime" , "selection-mod-doubletime" },
                {"Relax" , "selection-mod-relax" },
                {"HalfTime" , "selection-mod-halftime" },
                {"Nightcore" , "selection-mod-nightcore" },
                {"Flashlight" , "selection-mod-flashlight" },
                {"Autoplay" , "selection-mod-autoplay" },
                {"SpunOut", "selection-mod-spunout" },
                {"AutoPilot" , "selection-mod-relax2" },
                {"Perfect" , "selection-mod-perfect" },
                {"Cinema" , "selection-mod-cinema" },
                {"ScoreV2" , "selection-mod-scorev2" }
            };

        private static string GetModFileName(string mod_name)
        {
            if (ModsFileNameTranslateMap.TryGetValue(mod_name, out string result))
                return result;
            return null;
        }

        private readonly string osu_current_skin_path;
        private readonly string mods_Skin_Folder_Path;
        private readonly bool is_extra_mod;
        private readonly int output_Pixel_Length;
        private readonly int pixel_Offset;
        private readonly bool is_2x;
        private readonly bool is_Sort_Reverse;
        private readonly bool is_Draw_Reverse;
        private readonly bool is_Horizon;
        private Dictionary<string, Bitmap> cache_mod_bitmap = new Dictionary<string, Bitmap>();

        #region Construction

        /// <summary>
        /// Mod图片生成
        /// </summary>
        /// <param name="current_using_skin_path">当前皮肤路径</param>
        /// <param name="mods_skin_folder_path">强制钦定含有mods皮肤文件夹(会优先从这里找对应mod图片),没有再去屙屎文件夹那里找</param>
        /// <param name="output_pixel_length">输出每个mod图片大小</param>
        /// <param name="pixel_offset">输出每个mod之间的像素空隙</param>
        /// <param name="is_horizon">输出的mods是否水平排列</param>
        public ModsPictureGenerator(string current_using_skin_path, string mods_skin_folder_path, int output_pixel_length, int pixel_offset, bool is_horizon, bool is_2x, bool is_sort_reverse, bool is_draw_reverse)
        {
            osu_current_skin_path = current_using_skin_path;
            mods_Skin_Folder_Path = mods_skin_folder_path;
            output_Pixel_Length = output_pixel_length;
            pixel_Offset = pixel_offset;
            is_Horizon = is_horizon;
            this.is_2x = is_2x;
            is_Sort_Reverse = is_sort_reverse;
            is_Draw_Reverse = is_draw_reverse;
            is_extra_mod = !string.IsNullOrWhiteSpace(mods_skin_folder_path);
        }

        #endregion Construction

        private Bitmap LoadBitmap(string file_path)
        {
            Bitmap result;
            using (Bitmap b = new Bitmap(file_path))
                result = new Bitmap(b, new Size(output_Pixel_Length, output_Pixel_Length));
            return result;
        }

        private Bitmap LoadModBitmapFromFile(string mod_name)
        {
            string mod_file_path, mod_file_name = GetModFileName(mod_name);

            if (mod_file_name == null)
            {
                return null;
            }

            mod_file_name += (is_2x ? "@2x" : string.Empty) + ".png";

            if (is_extra_mod)
            {
                mod_file_path = Path.Combine(mods_Skin_Folder_Path, mod_file_name);
                if (File.Exists(mod_file_path))
                {
                    return LoadBitmap(mod_file_path);
                }
            }

            mod_file_path = Path.Combine(osu_current_skin_path, mod_file_name);
            if (File.Exists(mod_file_path))
            {
                return LoadBitmap(mod_file_path);
            }

            return null;
        }

        private Bitmap GetModBitmap(string mod_name)
        {
            //检查一下缓存有没有
            if (cache_mod_bitmap.TryGetValue(mod_name, out Bitmap result))
            {
                return result;
            }

            var bitmap = LoadModBitmapFromFile(mod_name);

            if (bitmap != null)
            {
                //加入肯德基豪华缓存午餐
                cache_mod_bitmap[mod_name] = bitmap;
            }

            return bitmap;
        }

        public Bitmap GenerateModsPicture(string[] mods)
        {
            if (is_Sort_Reverse)
            {
                mods = mods.Reverse().ToArray();
            }

            int width, height;

            if (is_Horizon)
            {
                width = mods.Length * output_Pixel_Length + (mods.Length - 1) * pixel_Offset;
                height = output_Pixel_Length;
            }
            else
            {
                height = mods.Length * output_Pixel_Length + (mods.Length - 1) * pixel_Offset;
                width = output_Pixel_Length;
            }

            Bitmap result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics canvas = Graphics.FromImage(result);

            canvas.Clear(Color.FromArgb(0, 0, 0, 0));

            for (int i = is_Draw_Reverse ? mods.Length - 1 : 0; is_Draw_Reverse ? (i >= 0) : (i < mods.Length); i += (is_Draw_Reverse ? -1 : 1))
            {
                string mod_name = mods[i];

                var mod_bitmap = GetModBitmap(mod_name);
                if (mod_bitmap == null)
                {
                    continue;
                }

                int offset = (output_Pixel_Length + pixel_Offset) * i;
                int draw_x = is_Horizon ? offset : 0;
                int draw_y = is_Horizon ? 0 : offset;

                canvas.DrawImage(mod_bitmap, draw_x, draw_y);

#if DEBUG
                IO.CurrentIO.WriteColor($"[MPG]Draw {i}th {mod_name} at ({draw_x},{draw_y})", ConsoleColor.Cyan);
#endif
            }

            canvas.Dispose();

            return result;
        }

        ~ModsPictureGenerator()
        {
            //dispose
            foreach (var bitmap in cache_mod_bitmap)
            {
                bitmap.Value.Dispose();
            }
        }
    }
}