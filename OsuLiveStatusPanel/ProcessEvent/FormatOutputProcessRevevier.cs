using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sync.Plugins;

namespace OsuLiveStatusPanel.ProcessEvent
{
    class FormatOutputProcessRevevier:ProcessRecevierBase
    {
        struct Content
        {
            public bool is_var;
            public string value;
        }

        static Regex pattern = new Regex(@"\$\{(.+?)\}");

        Dictionary<string, string> recived_map;

        LinkedList<Content> format_content_list = new LinkedList<Content>();
        
        StringBuilder stringBuilder = new StringBuilder(4096);

        string output_file_path;

        OutputType output_type;

        bool is_play;

        string[] format_variable_request { get; set; }

        public FormatOutputProcessRevevier(string output_file_path, string format_content, bool is_play)
        {
            this.is_play = is_play;
            this.output_file_path = output_file_path;
            var result = pattern.Matches(format_content);
            var format_request_count = result.Count;

            recived_map = new Dictionary<string, string>(format_request_count);
            var request = new List<string>();

            int prev_position = 0;

            foreach (Match r in result)
            {
                string clip = format_content.Substring(prev_position, r.Index - prev_position);
                prev_position = r.Length + r.Index;
                var str = r.Groups[1].Value.Trim();

                if (!string.IsNullOrWhiteSpace(clip))
                {
                    format_content_list.AddLast(new Content()
                    {
                        is_var = false,
                        value = clip
                    });
                }

                if (!string.IsNullOrWhiteSpace(str))
                {
                    request.Add(str);
                    format_content_list.AddLast(new Content()
                    {
                        is_var = true,
                        value = str
                    });
                }
            }

            if (prev_position != format_content.Length)
            {
                //add remain
                var str = format_content.Substring(prev_position);
                if (!string.IsNullOrWhiteSpace(str))
                {
                    format_content_list.AddLast(new Content()
                    {
                        is_var = false,
                        value = str
                    });
                }
            }

            format_variable_request = request.ToArray();
        }

        public override void OnEventRegister(BaseEventDispatcher<IPluginEvent> EventBus)
        {
            EventBus.BindEvent<StatusWrapperProcessEvent>(e=>output_type=e.Beatmap.OutputType);
            EventBus.BindEvent<PackedMetadataProcessEvent>(OnGetPackedData);
        }   

        public void Clear()
        {
            recived_map.Clear();
            stringBuilder.Clear();
            File.WriteAllText(output_file_path, String.Empty);
        }

        public void OnGetPackedData(PackedMetadataProcessEvent e)
        {
            if ((is_play ? OutputType.Play : OutputType.Listen) != output_type)
                Clear();

            var check_result = (from request in format_variable_request
                               from data in e.OutputData
                               where request == data.Key
                               select data);

            foreach (var pair in check_result)
                recived_map[pair.Key] = pair.Value;

            if (recived_map.Count() >= format_variable_request.Length)
            {
                Output();
            }
        }

        public void OnGetData(MetadataProcessEvent e)
        {
            if (!format_variable_request.Contains(e.Name))
                return;

            recived_map[e.Name] = e.Value;

            if (recived_map.Count>=format_variable_request.Length)
                Output();
        }

        public void Output(Dictionary<string, string> recived_map=null)
        {
            recived_map = recived_map ?? this.recived_map;

            stringBuilder.Clear();

            foreach (var content in format_content_list)
            {
                string format_str = content.is_var ? (recived_map[content.value] ?? string.Empty) : content.value;
                stringBuilder.Append(format_str);
            }

            File.WriteAllText(output_file_path, stringBuilder.ToString());
        }
    }
}
