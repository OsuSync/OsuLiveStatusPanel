using System;
using System.IO;

namespace OsuLiveStatusPanel.PPShow.Output
{
    public class DiskFileOutput : OutputBase
    {
        public DiskFileOutput(string path) : base(path)
        {
            if (!Path.IsPathRooted(this.path))
                this.path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.path);

            if (!Directory.Exists(Path.GetDirectoryName(this.path)))
                Directory.CreateDirectory(Path.GetDirectoryName(this.path));
        }

        public override void Output(string content)
        {
            File.WriteAllText(path, content);
        }
    }
}