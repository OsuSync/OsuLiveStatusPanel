using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuLiveStatusPanel
{
    public class OppaiJson
    {
        public string oppai_version{ get; set; }
        public string artist{ get; set; }
        public string title{ get; set; }
        public string version{ get; set; }
        public string creator{ get; set; }
        public string mods_str{ get; set; }
        public float od{ get; set; }
        public float ar{ get; set; }
        public float cs{ get; set; }
        public float hp { get; set; }
        public int combo{ get; set; }
        public int max_combo{ get; set; }
        public int num_circles{ get; set; }
        public int num_spinners{ get; set; }
        public int misses{ get; set; }
        public int score_version{ get; set; }
        public float stars{ get; set; }
        public float speed_stars{ get; set; }
        public float aim_stars{ get; set; }
        public float pp{ get; set; }

        public float accuracy;
        public string filepath;

        public int beatmap_id;
        public int beatmap_setid;
        public float min_bpm;
        public float max_bpm;
        public string title_unicode;
        public string artist_unicode;
        public string source;


        public string beatmap_setlink { get => beatmap_setid > 0 ? (@"https://osu.ppy.sh/s/" + beatmap_setid) : string.Empty; }
        public string beatmap_link { get => beatmap_id > 0 ? (@"https://osu.ppy.sh/b/" + beatmap_id) : string.Empty; }

        public string title_avaliable { get => title_unicode ?? title; }
        public string artist_avaliable { get => artist_unicode?? artist; }  
    }
}
