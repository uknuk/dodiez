using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dodiez
{
    public class Handler
    {
        public List<string> Artists { get; set; } = new List<string>();
        public List<string> Albums { get; set; } = new List<string>();
        public List<String> Tracks;

        public string SelArtist { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public int Position { get; set; }

        private string _root;

        public Handler(string root)
        {
            _root = root;
        }

        public void Selected(string album)
        {
            Album = album;
            Artist = SelArtist;
        }

        public string TrackPath(int idx)
        {
            return $"{_root}\\{Artist}\\{Album}\\{Tracks[idx]}";
        }

        public string AlbumPath()
        {
            return $"{_root}\\{Artist}\\{Album}";
        }

        public string Transform(string alb)
        {
            if (alb.Length < 3)
                return alb;

            const string re = @"^\d{2}[\s+|_|-]";
            if (alb.Substring(0,2) == "M0")
                return alb.Replace("M0", "200");

            var year = Regex.Matches(alb, re);
            if (year.Count == 0)
                return alb;

            var compare = string.Compare(year[0].ToString().Substring(0, 2), "30");
            return (compare < 0 ? "20" : "19") + alb;
            // works until 2030
        }
    }
}
