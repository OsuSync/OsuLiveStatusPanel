namespace OsuLiveStatusPanel.PPShow.Output
{
    public abstract class OutputBase
    {
        protected string path;

        public OutputBase(string path)
        {
            this.path = path;
        }

        public abstract void Output(string content);

        public static OutputBase Create(string path)
        {
            if (path.StartsWith(MemoryMappedFileOutput.MMF_FORMAT))
                return new MemoryMappedFileOutput(path);
            return new DiskFileOutput(path);
        }
    }
}