using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
