using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.PPShow.Output
{
    class MemoryMappedFileOutput : OutputBase
    {
        public const string MMF_FORMAT= @"mmf://";
        public const int MMF_CAPACITY = 4096;
        public readonly static byte[] clear_buffer = new byte[MMF_CAPACITY];

        MemoryMappedFile mmf_handler;

        public MemoryMappedFileOutput(string path) : base(path)
        {
            string real_mmf_path = path.StartsWith(MMF_FORMAT) ? path.Remove(0, MMF_FORMAT.Length) : path;
            mmf_handler = MemoryMappedFile.CreateOrOpen(real_mmf_path, MMF_CAPACITY, MemoryMappedFileAccess.ReadWrite);
        }

        public override void Output(string content)
        {
            using (StreamWriter stream = new StreamWriter(mmf_handler.CreateViewStream()))
            {
                stream.Write(content);
                stream.Write('\0');
            }
        }
    }
}
