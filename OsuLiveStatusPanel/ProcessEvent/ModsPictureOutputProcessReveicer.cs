using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sync.Plugins;
using Sync.Tools;

namespace OsuLiveStatusPanel.ProcessEvent
{
    class ModsPictureOutputProcessReveicer : ProcessRecevierBase
    {
        ModsPictureGenerator generator;
        private readonly string mod_Output_Path;
        private readonly string modSkinPath;

        public int ModUnitPixel;
        public int ModUnitOffset;
        public bool ModIsHorizon;
        public bool ModUse2X;
        public bool ModSortReverse;
        public bool ModDrawReverse;

        public ModsPictureOutputProcessReveicer(string mod_output_path, string ModSkinPath, int ModUnitPixel, int ModUnitOffset,bool ModIsHorizon,bool ModUse2x, bool ModSortReverse,bool ModDrawReverse)
        {
            mod_Output_Path = mod_output_path;
            modSkinPath = ModSkinPath;
            this.ModUnitPixel = ModUnitPixel;
            this.ModUnitOffset = ModUnitOffset;
            this.ModIsHorizon = ModIsHorizon;
            ModUse2X = ModUse2x;
            this.ModSortReverse = ModSortReverse;
            this.ModDrawReverse = ModDrawReverse;
        }

        public override void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus)
        {
            EventBus.BindEvent<PackedMetadataProcessEvent>(OnGetModChanged);
            EventBus.BindEvent<ClearProcessEvent>(OnClear);
        }

        private void OnClear(ClearProcessEvent e)
        {
            if (File.Exists(mod_Output_Path))
            {
                File.Delete(mod_Output_Path);
            }
        }

        public async void OnGetModChanged(PackedMetadataProcessEvent e)
        {
            if (!e.OutputData.TryGetValue("mods_full",out string mods))
                return;

            if (generator == null)
                TryCreateModsPictureGenerator(out generator);

            await Task.Run(() =>
            {
                using (var result = generator.GenerateModsPicture(mods.Split(',')))
                {
                    result.Save(mod_Output_Path, ImageFormat.Png);
                }
            });
        }

        private void TryCreateModsPictureGenerator(out ModsPictureGenerator modsPictureGenerator)
        {
            Process process = Process.GetProcessesByName("osu!")?.First();
            if (process == null)
            {
                modsPictureGenerator = null;
                return;
            }

            string osu_path = Path.GetDirectoryName(process.MainModule.FileName);
            string osu_config_file = Path.Combine(osu_path, $"osu!.{Environment.UserName}.cfg");
            string using_skin_name = string.Empty;

            var lines = File.ReadLines(osu_config_file);
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Skin =") || line.Trim().StartsWith("Skin="))
                {
                    using_skin_name = line.Split('=')[1].Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(using_skin_name))
            {
                modsPictureGenerator = null;
                return;
            }

            string using_skin_path = Path.Combine(osu_path, "Skins", using_skin_name);

            IO.CurrentIO.WriteColor($"[MPG]using_skin_path={using_skin_path}", ConsoleColor.Cyan);

            modsPictureGenerator = new ModsPictureGenerator(using_skin_path, modSkinPath, ModUnitPixel, ModUnitOffset, ModIsHorizon, ModUse2X , ModSortReverse, ModDrawReverse);
        }
    }
}
