using System.IO;

namespace OsuLiveStatusPanel.PPShow.Output
{
    public class DiskFileOutput : OutputBase
    {
        public DiskFileOutput(string path) : base(path)
        {
        }

        public override void Output(string content)
        {
            File.WriteAllText(path, content);
        }
    }
}