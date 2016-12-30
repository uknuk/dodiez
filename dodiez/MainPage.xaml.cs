using System;
using System.Collections.Generic;
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

        private Player _player;
        private MediaPlayer _mPlayer = new MediaPlayer();

        public MainPage()
        {
            this.Loaded += PageLoaded;
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.PreferredLaunchViewSize = new Size(1024, 768);
        }


        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            var mus = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            _player = new Player(mus.SaveFolder.Path);

            var dirs = await KnownFolders.MusicLibrary.GetFoldersAsync();
            var buttons = new ObservableCollection<Button>();
            
            foreach (var dir in dirs)
            {
                _player.Artists.Add(dir.Name);
                var button = new Button()
                {
                    Content = dir.Name,
                    Style = (Style)this.Resources["ArtStyle"]
                };
                button.Click += Artist_Click;
                buttons.Add(button);
            }

            VArtists.ItemsSource = buttons;
            Player.SetMediaPlayer(_mPlayer);
            var controls = Player.TransportControls;
            controls.IsCompact = true;
            _mPlayer.MediaEnded += OnMediaEnded;
        }

        private async void Artist_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var artist = button.Content.ToString();
            _player.SelArtist = artist;

            var dir = await KnownFolders.MusicLibrary.GetFolderAsync(artist);
            var albums = await dir.GetFoldersAsync();
            var buttons = new ObservableCollection<Button>();

            foreach (var album in albums)
            {
                _player.Albums.Add(album.Name);
                var btn = new Button() {
                    Content = album.Name,
                    Style = (Style) this.Resources["AlbumStyle"],
                    };
                btn.Click += Album_Click;
                buttons.Add(btn);
            }
            VAlbums.ItemsSource = buttons;
            PControl.SelectedItem = IPlayer;
        }

        private async void Album_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var album = button.Content.ToString();
            _player.Selected(album);

            var dir = await StorageFolder.GetFolderFromPathAsync(_player.AlbumPath());
            var tracks = await dir.GetFilesAsync();
            _player.Tracks = tracks.Where(t => t.FileType == ".mp3")
                .Select(t => t.Name).ToList();
            var i = 0;
            var buttons = new ObservableCollection<Button>();

            foreach (var track in _player.Tracks)
            {
                var btn = new Button()
                {
                    Content = Path.GetFileNameWithoutExtension(track),
                    Style = (Style) this.Resources["TrackStyle"],
                    Name = i.ToString()
                };
                btn.Click += Track_Click;
                buttons.Add(btn);
                i++;
            }
            VTracks.ItemsSource = buttons;
            await Play(0);
        }

        private async void Track_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            await Play(int.Parse(button.Name));
        }

        private async Task Play(int idx)
        {
            var path = _player.TrackPath(idx);
            var file = await StorageFile.GetFileFromPathAsync(path);
            _mPlayer.Source = MediaSource.CreateFromStorageFile(file);
            _player.Position = idx;
            var btn = (Button) VTracks.Items[idx];
            btn.Foreground = new SolidColorBrush(Colors.Red);
            if (idx > 0)
            {
                btn = (Button)VTracks.Items[idx - 1];
                btn.Foreground = new SolidColorBrush(Colors.Blue);
            }
            _mPlayer.Play();
        }

        private async void OnMediaEnded(MediaPlayer player, object sender)
        {
            var idx = _player.Position + 1;
            if (idx < _player.Tracks.Count)
            {
                await Play(idx);
            }
        }
    }
}
