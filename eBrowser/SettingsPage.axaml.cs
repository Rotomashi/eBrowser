using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using e621NET;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace eBrowser
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        public void RefreshSettings()
        {
            UsernameBox.Text = ListPage.Settings.Username;
            ApiKeyBox.Text = ListPage.Settings.APIKey;
            
            HideToTrayBox.IsChecked = ListPage.Settings.HideToTray;
            AutoplayVideosBox.IsChecked = ListPage.Settings.AutoplayVideos;
            AutomuteVideosBox.IsChecked = ListPage.Settings.AutomuteVideos;
            AutoDownloadImagesBox.IsChecked = ListPage.Settings.AutoDownloadImages;
            AutoDownloadVideosBox.IsChecked = ListPage.Settings.AutoDownloadVideos;

            UseCustomPathBox.IsChecked = ListPage.Settings.UseCustomPath;
            CustomPathBox.Text = ListPage.Settings.UseCustomPath ? ListPage.Settings.CustomPath : LocalStorage.persistentPath;
            NamingSchemeBox.Text = ListPage.Settings.NameScheme;
        }

        void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        {
            ListPage.Settings.Username = UsernameBox.Text;
            ListPage.Settings.APIKey = ApiKeyBox.Text;
            ListPage.Instance.SaveSettings();
            
            if (ListPage.Settings.Username == null || string.IsNullOrWhiteSpace(ListPage.Settings.Username) || ListPage.Settings.APIKey == null || string.IsNullOrWhiteSpace(ListPage.Settings.APIKey))
                return;
            
            e621Client.Current.AddCredentials(new e621APICredentials(ListPage.Settings.Username, ListPage.Settings.APIKey));
        }
        
        void BackButton_OnClick(object? sender, RoutedEventArgs e)
        {
            MainWindow.Instance.BackFromSettings();
        }
        
        void HideToTrayBox_OnClick(object? sender, RoutedEventArgs e)
        {
            ListPage.Settings.HideToTray = HideToTrayBox.IsChecked.HasValue && HideToTrayBox.IsChecked.Value;
            ListPage.Instance.SaveSettings();
        }
        
        void AutoplayVideosBox_OnClick(object? sender, RoutedEventArgs e)
        {
            ListPage.Settings.AutoplayVideos = AutoplayVideosBox.IsChecked.HasValue && AutoplayVideosBox.IsChecked.Value;
            ListPage.Instance.SaveSettings();
        }
        
        void AutomuteVideosBox_OnClick(object? sender, RoutedEventArgs e)
        {
            ListPage.Settings.AutomuteVideos = AutomuteVideosBox.IsChecked.HasValue && AutomuteVideosBox.IsChecked.Value;
            ListPage.Instance.SaveSettings();
        }
        
        void AutoDownloadImagesBox_OnClick(object? sender, RoutedEventArgs e)
        {
            ListPage.Settings.AutoDownloadImages = AutoDownloadImagesBox.IsChecked.HasValue && AutoDownloadImagesBox.IsChecked.Value;
            ListPage.Instance.SaveSettings();
        }
        
        void AutoDownloadVideosBox_OnClick(object? sender, RoutedEventArgs e)
        {
            ListPage.Settings.AutoDownloadVideos = AutoDownloadVideosBox.IsChecked.HasValue && AutoDownloadVideosBox.IsChecked.Value;
            ListPage.Instance.SaveSettings();
        }

        private void CheckBox_Click(object? sender, RoutedEventArgs e) {
            ListPage.Settings.UseCustomPath = UseCustomPathBox.IsChecked.HasValue && UseCustomPathBox.IsChecked.Value;
            CustomPathBox.IsEnabled = ListPage.Settings.UseCustomPath;
            if (ListPage.Settings.UseCustomPath) {
                if (ListPage.Settings.CustomPath != null && Directory.Exists(ListPage.Settings.CustomPath))
                    LocalStorage.OverridePersistentPath(ListPage.Settings.CustomPath);
            } else {
                LocalStorage.PopPersistentPath();
            }
            ListPage.Instance.SaveSettings();
        }

        private void CustomPathBox_Changed(object? sender, TextChangedEventArgs e) {
            ListPage.Settings.CustomPath = CustomPathBox.Text;
            if (ListPage.Settings.CustomPath != null && Directory.Exists(ListPage.Settings.CustomPath))
                LocalStorage.OverridePersistentPath(ListPage.Settings.CustomPath);
            ListPage.Instance.SaveSettings();
        }

        private void NamingSchemeBox_Changed(object? sender, TextChangedEventArgs e) {
            ListPage.Settings.NameScheme = NamingSchemeBox.Text ?? "{artist}-{id}{ext}";
            ListPage.Instance.SaveSettings();
        }

        private void OpenButton_Click(object? sender, RoutedEventArgs e) {
            string folderPath = LocalStorage.persistentPath;

            if (!Directory.Exists(folderPath)) {
                ShowErrorDialog("Directory does not exist:\n" + folderPath);
                return;
            }

            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    Process.Start(folderPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", folderPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", folderPath);
                }
                else {
                    ShowErrorDialog("Unsupported operating system.");
                }
            }
            catch (System.Exception ex) {
                ShowErrorDialog("Error opening directory:\n" + ex.Message);
            }
        }

        private void ShowErrorDialog(string message) {
            var dialog = new Window {
                Width = 400,
                Height = 200,
                Content = new TextBlock {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
            dialog.Show();
        }
    }
}

