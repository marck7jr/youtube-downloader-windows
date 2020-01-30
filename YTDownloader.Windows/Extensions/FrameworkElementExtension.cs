using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace YTDownloader.Windows.Extensions
{
    public static class FrameworkElementExtension
    {
        public static void AddExtendedView(this FrameworkElement element)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            AddTitleBarCustomColors(element);
        }

        public static void AddTitleBarCustomColors(this FrameworkElement element)
        {
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            var foregroundColor = (Color)element.Resources["SystemBaseHighColor"];

            // Normal
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = foregroundColor;

            // Hover
            titleBar.ButtonHoverBackgroundColor = (Color)element.Resources["SystemListLowColor"];
            titleBar.ButtonHoverForegroundColor = foregroundColor;

            // Inactive
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveForegroundColor = foregroundColor;

            // Pressed
            titleBar.ButtonPressedBackgroundColor = (Color)element.Resources["SystemListMediumColor"];
            titleBar.ButtonPressedForegroundColor = foregroundColor;
        }
    }
}
