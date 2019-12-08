using ATL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.PPShow
{
    public static class AudioDurationHelper
    {
        public static int? GetAudioDuration(string audio_file_path)
        {
            try
            {
                if (!File.Exists(audio_file_path))
                    return null;

                var track = new Track(audio_file_path);

                return track.Duration * 1000;//convert to ms
            }
            catch (Exception e)
            {
                Log.Debug("Can't get audio duration." + e.Message);
                return null;
            }
        }
    }
}
