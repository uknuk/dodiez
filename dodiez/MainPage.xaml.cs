using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
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

        private Player _player = new Player();

        public MainPage()
        {
            this.Loaded += PageLoaded;
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.PreferredLaunchViewSize = new Size(1024, 768);

        }

        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<StorageFolder> dirs = await KnownFolders.MusicLibrary.GetFoldersAsync();
            var artists = new StringBuilder();
            foreach (var dir in dirs)
            {
                _player.Artists.Add(dir.Name);
                var button = new Button() {Content = dir.Name};
                button.Click += Artist_Click;
                PArtists.Children.Add(button);

            }
        }

        private async void Artist_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var artist = button.Content.ToString();
            _player.SelArtist = artist;

            var dir = await KnownFolders.MusicLibrary.GetFolderAsync(artist);
            var albums = await dir.GetFoldersAsync();
            PAlbums.Children.Clear();
            foreach (var album in albums)
            {
                _player.Albums.Add(album.Name);
                var btn = new Button() { Content = album.Name,
                    HorizontalAlignment = HorizontalAlignment.Stretch};
                btn.Click += Album_Click;
                PAlbums.Children.Add(btn);
            }

        }

        private async void Album_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var album = button.Content.ToString();
            _player.Selected(album);

            var root = await MusicPath(); 
            var path = $"{root}\\{_player.Artist}\\{album}";
            var dir = await StorageFolder.GetFolderFromPathAsync(path);
            var tracks = await dir.GetFilesAsync();
            _player.Tracks = tracks.Where(t => t.FileType == ".mp3")
                .Select(t => t.Name).ToList();
            var i = 0;
            PTracks.Children.Clear();
            foreach (var track in _player.Tracks)
            {
                var btn = new Button()
                {
                    Content = Path.GetFileNameWithoutExtension(track),
                    Name = i.ToString()
                };
                btn.Click += Track_Click;
                PTracks.Children.Add(btn);
                i++;
            }
        }

        private async void Track_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var idx = Int32.Parse(button.Name);
            var root = await MusicPath();
            var path = $"{root}\\{_player.TrackPath(idx)}";
            var file = await StorageFile.GetFileFromPathAsync(path);
            var stream = await file.OpenAsync(FileAccessMode.Read);
            Player.SetSource(stream, file.FileType);
            Player.Play();
        }

        private async Task<string> MusicPath()
        {
            var mus = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            return mus.SaveFolder.Path;
        }

    }
}
