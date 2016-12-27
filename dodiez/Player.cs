using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dodiez
{
    public class Player
    {
        public List<string> Artists { get; set; } = new List<string>();
        public List<string> Albums { get; set; } = new List<string>();
        public List<String> Tracks;

        public string SelArtist { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        public void Selected(string album)
        {
            Album = album;
            Artist = SelArtist;
        }

        public string TrackPath(int idx)
        {
            return $"{Artist}\\{Album}\\{Tracks[idx]}";
        }
    }
}
