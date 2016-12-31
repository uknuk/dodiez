using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
        public int? AlbumNum { get; set; }
        public int? Position { get; set; }

        private string _root;
        private StorageFolder _dir = ApplicationData.Current.LocalFolder;
        private const string _datafile = "data.txt";

        public Handler(string root)
        {
            _root = root;
        }

        public void Selected(int? idx = null)
        {
            if (idx.HasValue)
                AlbumNum = idx.Value;
                
            Album = Albums[AlbumNum.Value];
            Artist = SelArtist;
        }

        public string TrackPath()
        {
            return $"{_root}\\{Artist}\\{Album}\\{Tracks[Position.Value]}";
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

        public async void Store()
        {
           

            var file = await _dir.CreateFileAsync(_datafile, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, $"{Artist}\n{AlbumNum}\n{Position}");
        }

        public async void Load()
        {
            IStorageFile file;
            try
            {
                file = await _dir.GetFileAsync(_datafile);
            }
            catch (FileNotFoundException ex)
            {
                return;
            }

            var data = await FileIO.ReadTextAsync(file);
            var vals = data.Split('\n');
            SelArtist = vals[0];
            AlbumNum = int.Parse(vals[1]);
            Position = int.Parse(vals[2]);

        }


        public void Restore(GridView buttons, int? idx, Color color)
        {
            if (!idx.HasValue || idx.Value >= Tracks.Count)
                return; // idx can be from previous album
            var btn = (Button) buttons.Items[idx.Value];
            btn.Foreground = new SolidColorBrush(color);
        }
    }
}
