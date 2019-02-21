using OppaiWNet.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel.PPShow.BeatmapInfoHanlder
{
    public class CompatibleOppaiJson:IDisposable
    {
        internal readonly Ezpp info;

        public CompatibleOppaiJson(Ezpp info)
        {
            this.info=info;
        }

        public void Dispose()
        {
            info.Dispose();
        }

        public string oppai_version => OppaiWNet.Oppai.oppai_version_str_str();
        public string artist => info.Artist;
        public string artist_unicode => info.ArtistUnicode;
        public string title => info.Title;
        public string title_unicode => info.TitleUnicode;
        public string creator => info.Creator;
        public string version => info.Version;
        public string mods_str => info.Mods.ToString();
        public int mods => (int)info.Mods;
        public float od => info.OD;
        public float ar => info.AR;
        public float hp => info.HP;
        public float cs => info.CS;
        public int combo => info.Combo;
        public int max_combo => info.MaxCombo;
        public int num_circles => info.CountCircles;
        public int num_sliders => info.CountSliders;
        public int num_spinners => info.CountSliders;
        public int misses => info.CountMiss;
        public int score_version => info.ScoreVersion;
        public float stars => info.Stars;
        public float speed_stars => info.SpeedStars;
        public float aim_stars => info.AimStars;
        public float aim_pp => info.AimPP;
        public float speed_pp => info.SpeedPP;
        public float acc_pp => info.AccPP;
        public float pp => info.PP;

        //public int nsingles => ???
        //public int nsingles_threshold => ???
    }
}
