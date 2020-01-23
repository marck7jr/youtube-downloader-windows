using YTDownloader.Windows.Models;
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

namespace YTDownloader.Windows
{
    /// <summary>
    /// Uma página vazia que pode ser usada isoladamente ou navegada dentro de um Quadro.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public SemaphoreSlim SemaphoreSlim { get; set; }
        public YoutubeClient YoutubeClient { get; set; }
        public ObservableCollection<DownloadItem> DownloadItems { get; set; }

        public MainPage()
        {
            this.SemaphoreSlim = new SemaphoreSlim(1, 1);
            this.YoutubeClient = new YoutubeClient();
            this.DownloadItems = new ObservableCollection<DownloadItem>();
            this.InitializeComponent();
        }

        private DownloadItem CreateItem(Video video, DownloadType type, StorageFile file)
        {
            var item = new DownloadItem
            {
                Video = video,
                Type = type,
            };
            item.Task = Task.Run(async () =>
            {
                try
                {
                    await SemaphoreSlim.WaitAsync();
                    var client = new YoutubeClient();
                    MediaStreamInfo mediaStream = null;
                    switch (type)
                    {
                        case DownloadType.Audio:
                            mediaStream = (await client.GetVideoMediaStreamInfosAsync(video.Id)).Audio.WithHighestBitrate();
                            break;
                        case DownloadType.Video:
                            mediaStream = (await client.GetVideoMediaStreamInfosAsync(video.Id)).Muxed.WithHighestVideoQuality();
                            break;
                    }

                    var fileName = $"{video.Title}.{mediaStream.Container.ToString().ToLower()}";
                    await file.RenameAsync(fileName, NameCollisionOption.GenerateUniqueName);
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        using (var stream = new MemoryStream())
                        {
                            await client.DownloadMediaStreamAsync(mediaStream, stream, new Progress<double>(value => item.Progress = value));
                            await FileIO.WriteBytesAsync(file, stream.ToArray());
                        }
                        item.IsCompleted = true;
                        SemaphoreSlim.Release();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(item.Video.Title);
                    SemaphoreSlim.Release();
                }
                finally
                {
                }
            });
            DownloadItems.Add(item);
            return item;
        }

        public Action<ICollection<DownloadItem>> ScheduleTask = (items) =>
        {
            Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async item =>
            {
                await item.Task;
            });
        };

        public async Task SetupAsync(DownloadType type)
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
                    CreateItem(video, type, file);
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
                var item = CreateItem(video, DownloadType.Video, file);
                await item.Task;
            }
        }

        private async void VideoButton_Click(object sender, RoutedEventArgs e)
        {
            await SetupAsync(DownloadType.Video);
        }

        private async void AudioButton_Click(object sender, RoutedEventArgs e)
        {
            await SetupAsync(DownloadType.Audio);
        }
    }
}