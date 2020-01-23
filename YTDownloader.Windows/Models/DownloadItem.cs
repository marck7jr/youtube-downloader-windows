using System;
using System.ComponentModel;
using System.Threading.Tasks;
using YoutubeExplode.Models;

namespace YTDownloader.Windows.Models
{
    public enum DownloadType
    {
        Audio,
        Video
    }

    public class DownloadItem : ObservableObject
    {
        private bool isCompleted;
        private double progress;

        public DownloadItem()
        {
            IsCompleted = false;
        }

        public bool IsCompleted { get => isCompleted; set => Set(ref isCompleted, value); }
        public double Progress { get => (progress * 100); set => Set(ref progress, value); }
        public Video Video { get; set; }
        public DownloadType Type { get; set; }
        public Task Task { get; set; }
    }
}
