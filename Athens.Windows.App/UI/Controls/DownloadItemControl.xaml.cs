using Athens.Windows.App.Models;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// O modelo de item de Controle de Usuário está documentado em https://go.microsoft.com/fwlink/?LinkId=234236

namespace Athens.Windows.App.UI.Controls
{
    public sealed partial class DownloadItemControl : UserControl
    {
        public DownloadItem DownloadItem { get => this.DataContext as DownloadItem; }

        public DownloadItemControl()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
        }
    }
}
