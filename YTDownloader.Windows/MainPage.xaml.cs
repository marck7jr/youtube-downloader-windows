using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using YTDownloader.Windows.Models;

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
            var tokenSource = new CancellationTokenSource();
            var item = new DownloadItem
            {
                Video = video,
                Type = type,
            };
            item.Task = new Task(async () =>
            {
                try
                {
                    await SemaphoreSlim.WaitAsync();
                    while (NetworkInformation.GetInternetConnectionProfile() is null) ; ;
                    var client = new YoutubeClient();
                    var fileName = $"{video.Title}";
                    MediaStreamInfo mediaStream = null;
                    switch (type)
                    {
                        case DownloadType.Audio:
                            mediaStream = (await client.GetVideoMediaStreamInfosAsync(video.Id)).Audio.WithHighestBitrate();
                            fileName += $".mp3";
                            break;
                        case DownloadType.Video:
                            mediaStream = (await client.GetVideoMediaStreamInfosAsync(video.Id)).Muxed.WithHighestVideoQuality();
                            fileName += $".{mediaStream.Container}";
                            break;
                    }

                    await file.RenameAsync(fileName, NameCollisionOption.GenerateUniqueName);
                    NetworkInformation.NetworkStatusChanged += (_) =>
                    {
                        if (NetworkInformation.GetInternetConnectionProfile() is null)
                        {
                            tokenSource.Cancel();
                        }
                    };
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        using (var stream = new MemoryStream())
                        {
                            await client.DownloadMediaStreamAsync(mediaStream, stream, new Progress<double>(value => item.Progress = value));
                            if (type == DownloadType.Audio)
                            {
                                var profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto);
                                var transcoder = new MediaTranscoder();
                                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                                {
                                    var prepareOp = await transcoder.PrepareStreamTranscodeAsync(stream.AsRandomAccessStream(), fileStream, profile);

                                    if (prepareOp.CanTranscode)
                                    {
                                        await prepareOp.TranscodeAsync();
                                    }
                                }
                            }
                            else
                            {
                                await FileIO.WriteBytesAsync(file, stream.ToArray());
                            }
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

            }, tokenSource.Token);
            DownloadItems.Add(item);
            return item;
        }

        public Action<ICollection<DownloadItem>> ScheduleTask = (items) =>
        {
            Parallel.ForEach(items, new ParallelOptions { MaxDegreeOfParallelism = 1 }, item =>
            {
                item.Task.Start();
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
                item.Task.Start();
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