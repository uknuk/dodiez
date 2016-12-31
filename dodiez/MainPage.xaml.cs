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
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.PreferredLaunchViewSize = new Size(1024, 768);
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
                    Style = (Style) this.Resources["ArtStyle"]
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
        }

        private async void Artist_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var artist = button.Content.ToString();
            _handler.SelArtist = artist;

            var dir = await KnownFolders.MusicLibrary.GetFolderAsync(artist);
            var subdirs = await dir.GetFoldersAsync();
            var buttons = new ObservableCollection<Button>();

            var albums = subdirs.Select(s => s.Name)
                .OrderBy(n => _handler.Transform(n)).ToList();

            foreach (var album in albums)
            {
                _handler.Albums.Add(album);
                var btn = new Button() {
                    Content = album,
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
            _handler.Selected(album);
            TArtist.Text = _handler.Artist;
            TAlbum.Text = $"{_handler.Album} ";

            var dir = await StorageFolder.GetFolderFromPathAsync(_handler.AlbumPath());
            var tracks = await dir.GetFilesAsync();
            _handler.Tracks = tracks.Where(t => t.FileType == ".mp3")
                .Select(t => t.Name).ToList();
            var i = 0;
            var buttons = new ObservableCollection<Button>();

            foreach (var track in _handler.Tracks)
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
            var path = _handler.TrackPath(idx);
            var file = await StorageFile.GetFileFromPathAsync(path);
            var stream = await file.OpenAsync(FileAccessMode.Read);
            Player.SetSource(stream, file.FileType);
            TSong.Text = _handler.Tracks[idx];
            // Player.Source = MediaSource.CreateFromStorageFile(file);
            _handler.Position = idx;
            var btn = (Button) VTracks.Items[idx];
            btn.Foreground = new SolidColorBrush(Colors.Red);
            if (idx > 0)
            {
                btn = (Button)VTracks.Items[idx - 1];
                btn.Foreground = new SolidColorBrush(Colors.Blue);
            }
            Player.Play();
        }

        private async void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            var idx = _handler.Position + 1;
            if (idx < _handler.Tracks.Count)
            {
                await Play(idx);
            }
        }
    }
}
