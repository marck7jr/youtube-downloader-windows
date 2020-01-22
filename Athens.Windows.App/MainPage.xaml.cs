using Athens.Windows.App.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;

// O modelo de item de Página em Branco está documentado em https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x416

namespace Athens.Windows.App
{
    /// <summary>
    /// Uma página vazia que pode ser usada isoladamente ou navegada dentro de um Quadro.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public YoutubeClient YoutubeClient { get; set; }
        public ObservableCollection<DownloadItem> DownloadItems { get; set; }

        public MainPage()
        {
            this.YoutubeClient = new YoutubeClient();
            this.DownloadItems = new ObservableCollection<DownloadItem>();
            this.InitializeComponent();
        }

        public Func<Video, DownloadType, StorageFile, ICollection<DownloadItem>, DownloadItem> CreateItem = (video, type, file, items) =>
        {
            var item = new DownloadItem
            {
                Video = video,
                Type = type,
            };
            item.Func = async () =>
            {
                try
                {
                    var client = new YoutubeClient();
                    MediaStreamInfo mediaStream = null;
                    switch (item.Type)
                    {
                        case DownloadType.Audio:
                            mediaStream = (await client.GetVideoMediaStreamInfosAsync(item.Video.Id)).Audio.WithHighestBitrate();
                            break;
                        case DownloadType.Video:
                            mediaStream = (await client.GetVideoMediaStreamInfosAsync(item.Video.Id)).Muxed.WithHighestVideoQuality();
                            break;
                    }

                    var fileName = $"{item.Video.Title}.{mediaStream.Container.ToString().ToLower()}";
                    await file.RenameAsync(fileName, NameCollisionOption.GenerateUniqueName);
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        using (var stream = new MemoryStream())
                        {
                            await client.DownloadMediaStreamAsync(mediaStream, stream, new Progress<double>(value => item.Progress = value));
                            await FileIO.WriteBytesAsync(file, stream.ToArray());
                        }
                        item.IsCompleted = true;
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(item.Video.Title);
                }
            };
            items.Add(item);
            return item;
        };

        public Action<ICollection<DownloadItem>> ScheduleTask = async (items) =>
        {
            foreach (var item in items)
            {
                await item.Func();
            }
        };

        private async void VideoButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)PlaylistButton.IsChecked)
            {
                if (!YoutubeClient.TryParsePlaylistId(AutoSuggestBox.Text, out string playlistId))
                {

                }

                var playlist = await YoutubeClient.GetPlaylistAsync(playlistId);
                var folder = await DownloadsFolder.CreateFolderAsync(playlist.Title, CreationCollisionOption.GenerateUniqueName);
                foreach (var video in playlist.Videos)
                {
                    var file = await folder.CreateFileAsync(Guid.NewGuid().ToString());
                    CreateItem(video, DownloadType.Video, file, DownloadItems);
                }
                ScheduleTask(DownloadItems);
            }
            else
            {
                if (!YoutubeClient.TryParseVideoId(AutoSuggestBox.Text, out string videoId))
                {

                }

                var video = await YoutubeClient.GetVideoAsync(videoId);
                var file = await DownloadsFolder.CreateFileAsync(Guid.NewGuid().ToString());
                var item = CreateItem(video, DownloadType.Video, file, DownloadItems);
                await item.Func();
            }
        }

        private void AudioButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}