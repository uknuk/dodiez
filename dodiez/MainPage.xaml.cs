using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dodiez
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private Handler _handler;
        //private MediaPlayer _player = new MediaPlayer();

        public MainPage()
        {
            this.Loaded += PageLoaded;
            this.InitializeComponent();
            
            ApplicationView.PreferredLaunchViewSize = new Size(1024, 768);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }


        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            var mus = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            _handler = new Handler(mus.SaveFolder.Path);

            var dirs = await KnownFolders.MusicLibrary.GetFoldersAsync();
            var buttons = new ObservableCollection<Button>();
            
            foreach (var dir in dirs)
            {
                _handler.Artists.Add(dir.Name);
                var button = new Button()
                {
                    Content = dir.Name,
                    Style = (Style) Resources["ArtStyle"]
                };
                button.Click += Artist_Click;
                buttons.Add(button);
            }

            VArtists.ItemsSource = buttons;
            
            var controls = Player.TransportControls;
            controls.IsZoomButtonVisible = false;
            controls.IsFullWindowButtonVisible = false;
            controls.BorderBrush = new SolidColorBrush(Colors.Black);
            controls.BorderThickness = new Thickness(1);

            _handler.Load();
            if (_handler.SelArtist != null)
                await SelectArtist(_handler.SelArtist);

            if (_handler.AlbumNum.HasValue)
            {
                _handler.Selected();
                await SelectAlbum(_handler.AlbumNum.Value, _handler.Position.Value);
            }

        }

        private async Task SelectArtist(string artist)
        {
            var dir = await KnownFolders.MusicLibrary.GetFolderAsync(artist);
            var items = await dir.GetItemsAsync();
            var buttons = new ObservableCollection<Button>();

            _handler.Albums = items.Select(s => s.Name)
                .OrderBy(n => _handler.Transform(n)).ToList();

            int i = 0;
            foreach (var album in _handler.Albums)
            {
                var btn = new Button()
                {
                    Content = Path.GetFileNameWithoutExtension(album),
                    Name = $"A{i}",
                    Style = (Style) Resources["AlbumStyle"],
                };
                btn.Click += Album_Click;
                buttons.Add(btn);
                i++;
            }
            VAlbums.ItemsSource = buttons;
            PControl.SelectedItem = IPlayer;
        }

        private async Task SelectAlbum(int albNum, int trackNum = 0)
        {
            TArtist.Text = _handler.Artist;
            TAlbum.Text = $"{_handler.Album} ";

            if (_handler.AlbumIsFile)
            {
                var file = await StorageFile.GetFileFromPathAsync(_handler.AlbumPath);
                _handler.Tracks = new List<string> { file.Name };
            }
            else
            {
                var dir = await StorageFolder.GetFolderFromPathAsync(_handler.AlbumPath);
                var tracks = await dir.GetFilesAsync();
                _handler.Tracks = tracks.Where(t => t.FileType == ".mp3")
                    .Select(t => t.Name).ToList();
            }

            var i = 0;
            var buttons = new ObservableCollection<Button>();

            foreach (var track in _handler.Tracks)
            {
                var btn = new Button
                {
                    Content = Path.GetFileNameWithoutExtension(track),
                    Style = (Style) Resources["TrackStyle"],
                    Name = i.ToString()
                };
                btn.Click += Track_Click;
                buttons.Add(btn);
                i++;
            }
            VTracks.ItemsSource = buttons;
            if (!_handler.Tracks.Any())
                // ffs: move to next album
                return;

            Paint(VAlbums, albNum);       
            await Play(trackNum);
        }

        private async Task Play(int idx)
        {
            Restore(VTracks, _handler.Position);
            _handler.Position = idx;
            var path = _handler.TrackPath();
            var file = await StorageFile.GetFileFromPathAsync(path);
            var stream = await file.OpenAsync(FileAccessMode.Read);
            Player.SetSource(stream, file.FileType);
            TSong.Text = _handler.Tracks[idx];
            _handler.Position = idx;
            Paint(VTracks, idx);
            _handler.Store();
            Player.Play();
        }

        private void Paint(GridView buttons, int idx)
        {
            var btn = (Button) buttons.Items[idx];
            btn.Foreground = new SolidColorBrush(Colors.Red);
        }

        private async void Artist_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var artist = button.Content.ToString();
            _handler.SelArtist = artist;
            await SelectArtist(artist);
        }

        private async void Album_Click(object sender, RoutedEventArgs e)
        {
            Restore(VAlbums, _handler.AlbumNum);
            var button = sender as Button;
            _handler.Selected(int.Parse(button.Name.Substring(1)));
           
            await SelectAlbum(_handler.AlbumNum.Value);
        }

        private async void Track_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            await Play(int.Parse(button.Name));
        }


        private async void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            var idx = _handler.Position.Value + 1;
            if (idx < _handler.Tracks.Count)
            {
                await Play(idx);
            }
            else
            {
                idx = _handler.AlbumNum.Value + 1;
                if (idx < _handler.Albums.Count)
                {
                    Restore(VAlbums, _handler.AlbumNum);
                    _handler.Selected(idx);
                    await SelectAlbum(idx);
                }
            }
        }

        public void Restore(GridView buttons, int? idx)
        {
            if (!idx.HasValue)
                return;

            bool IsAlbums = buttons.Name == "VAlbums";
            List<string> list = IsAlbums ? _handler.Albums : _handler.Tracks;

            if (idx.Value >= list.Count)
                return; // idx can be from previous selections

            var btn = (Button)buttons.Items[idx.Value];
            btn.Foreground = new SolidColorBrush(IsAlbums ? Colors.Green : Colors.Blue);

        }
    }
}
