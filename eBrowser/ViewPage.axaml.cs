using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using e621NET;
using e621NET.Data.Posts;

namespace eBrowser
{
    public partial class ViewPage : UserControl
    {
        public event Action? onBackPressed;
        
        public ePosts? Posts;
        public int Index;
        public ePost? CurrentPost;
        
        public string VideoHtml;
        public string ImageHtml;
        
        public string? FileUrl;
        public string FilePath => PostFileNamer.GetPath(CurrentPost!);

        public ViewPage()
        {
            InitializeComponent();
            VideoHtml = ToString(AssetLoader.Open(new Uri("avares://eBrowser/Assets/video-view.html")));
            ImageHtml = ToString(AssetLoader.Open(new Uri("avares://eBrowser/Assets/image-view.html")));
            
            webView.Navigated += WebViewOnNavigated;
            webView.Focusable = true;
        }
        
        async void WebViewOnNavigated(string url, string framename)
        {
            if (CurrentPost == null) return;
            if (MainWindow.mode != MenuMode.Viewer || !ListPage.Settings.AutoplayVideos) return;
            
            try
            {
                var ext = CurrentPost.File.Ext ?? "png";
                if (videoFormats.Contains(ext.ToLower()))
                {
                    webView.ExecuteScript("playVideo();");
                }
            } 
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public static string ToString(Stream stream)
        {
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void LoadPost(ePosts posts, int index)
        {
            var post = posts.Posts[index];
            Index = index;
            Posts = posts;
            CurrentPost = post;

            PageLabel.Text = $"{posts.Posts.IndexOf(CurrentPost) + 1}/{posts.Posts.Count}";
            TagsList.Items.Clear();
            ArtistsList.Items.Clear();
            CharactersList.Items.Clear();
            SourcesList.Items.Clear();;
            PoolsList.Items.Clear();

            foreach (var tag in post.Tags.Artist)
                ArtistsList.Items.Add(tag);
            foreach (var tag in post.Tags.Character)
                CharactersList.Items.Add(tag);
            foreach (var tag in post.Tags.Species)
                CharactersList.Items.Add(tag);
            foreach (var tag in post.Tags.Copyright)
                TagsList.Items.Add(tag);
            foreach (var tag in post.Tags.General)
                TagsList.Items.Add(tag);
            foreach (var tag in post.Tags.Invalid)
                TagsList.Items.Add(tag);
            foreach (var tag in post.Tags.Meta)
                TagsList.Items.Add(tag);
            foreach (var source in post.Sources)
                SourcesList.Items.Add(source);
            foreach (var id in post.Pools)
                PoolsList.Items.Add(id);
            
            ArtistsList.IsVisible = ArtistsList.Items.Count > 0;
            CharactersList.IsVisible = CharactersList.Items.Count > 0;
            TagsList.IsVisible = TagsList.Items.Count > 0;
            SourcesList.IsVisible = SourcesList.Items.Count > 0;
            PoolsList.IsVisible = PoolsList.Items.Count > 0;

            int width, height, size;
            
            var quality = 2;
            if (post.File.Ext != null && videoFormats.Contains(post.File.Ext)) {
                // Low
                if (quality == 0) {
                    if (post.Sample.Alternates!.Quality480 != null) {
                        FileUrl = post.Sample.Alternates.Quality480.Urls[0];
                        width = post.Sample.Alternates.Quality480.Width;
                        height = post.Sample.Alternates.Quality480.Height; 
                    } else {
                        FileUrl = post.File.Url ?? string.Empty;
                        width = post.File.Width;
                        height = post.File.Height;
                    }
                }
                // Medium
                else if (quality == 1) {
                    if (post.Sample.Alternates!.Quality720 != null) {
                        FileUrl = post.Sample.Alternates.Quality720.Urls[0];
                        width = post.Sample.Alternates.Quality720.Width;
                        height = post.Sample.Alternates.Quality720.Height;
                    } else if (post.Sample.Alternates!.Quality480 != null) {
                        FileUrl = post.Sample.Alternates.Quality480.Urls[0];
                        width = post.Sample.Alternates.Quality480.Width;
                        height = post.Sample.Alternates.Quality480.Height;
                    } else {
                        FileUrl = post.File.Url ?? string.Empty;
                        width = post.File.Width;
                        height = post.File.Height;
                    }
                }
                // High
                else {
                    FileUrl = post.File.Url ?? string.Empty;
                    width = post.File.Width;
                    height = post.File.Height;
                }
            } else {
                // Low
                if (quality == 0) {
                    FileUrl = post.Preview.Url ?? string.Empty;
                    width = post.Preview.Width;
                    height = post.Preview.Height;
                }
                // Medium
                else if (quality == 1) {
                    FileUrl = post.Sample.Url ?? string.Empty;
                    width = post.Sample.Width;
                    height = post.Sample.Height;
                }
                // High
                else {
                    FileUrl = post.File.Url ?? string.Empty;
                    width = post.File.Width;
                    height = post.File.Height;
                }
            }

            BasicInfoLabel.Content = $"{width} x {height} | {post.File.Ext} | {FormatBytes(post.File.Size)}";
            
            var ext = post.File.Ext ?? "png";
            if (videoFormats.Contains(ext.ToLower())) {
                if (File.Exists(FilePath))
                    OpenHTMLStringToFile(
                        VideoHtml
                            .Replace("{VIDEO_URL}", "file:///" + FilePath.Replace("\\", "/"))
                            .Replace("{ADDITIONAL_PARAM}", ListPage.Settings.AutomuteVideos ? " muted" : "")
                    );
                else
                    OpenHTMLStringToFile(
                        VideoHtml
                            .Replace("{VIDEO_URL}", FileUrl)
                            .Replace("{ADDITIONAL_PARAM}", ListPage.Settings.AutomuteVideos ? " muted" : ""));

                if (ListPage.Settings.AutoDownloadVideos) {
                    DownloadFile();
                }
            } else {
                if (File.Exists(FilePath))
                    OpenHTMLStringToFile(ImageHtml.Replace("{IMAGE_URL}", "file:///" + FilePath.Replace("\\", "/")));
                else
                    OpenHTMLStringToFile(ImageHtml.Replace("{IMAGE_URL}", FileUrl));

                if (ListPage.Settings.AutoDownloadImages) {
                    DownloadFile();
                }
            }
            
            if (webView.IsAttachedToVisualTree())
            {
                webView.Focus();
            }
            else
            {
                webView.AttachedToVisualTree += (s, e) =>
                {
                    webView.Focus();
                };
            }
        }
        
        List<string> videoFormats = new List<string>() {
            "mp4",
            "webm",
            "m4a"
        };
        
        public static string FormatBytes(long bytes) {
            string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            int i = 0;
            double dblSByte = bytes;

            if (bytes > 1024) {
                for (i = 0; (bytes / 1024) > 0; i++, bytes /= 1024) {
                    dblSByte = bytes / 1024.0;
                }
            }

            return string.Format("{0:0.##} {1}", dblSByte, sizeSuffixes[i]);
        }
        
        public void OpenHTMLStringToFile(string text) {
            File.WriteAllText("temporary.html".ToPersistPath(), text);
            webView.Address = "file://" + "temporary.html".ToPersistPath();
            webView.Reload();
        }
        
        public void BackButton_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (CurrentPost == null) return;
                var ext = CurrentPost.File.Ext ?? "png";
                if (videoFormats.Contains(ext.ToLower()))
                {
                    webView.ExecuteScript("pauseVideo();");
                }
            } 
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            onBackPressed?.Invoke();
        }

        void PreviousPageButton_OnClick(object? sender, RoutedEventArgs e) => PreviousPage();
        public void PreviousPage()
        {
            Index--;
            if (Index < 0)
            {
                Index = Posts!.Posts.Count - 1;
            }

            LoadPost(Posts!, Index);
        }

        void NextPageButton_OnClick(object? sender, RoutedEventArgs e) => NextPage();
        public void NextPage()
        {
            Index++;
            if (Index >= Posts!.Posts.Count)
            {
                Index = 0;
            }
            LoadPost(Posts!, Index);
        }

        public async void DownloadFile()
        {
            try
            {
                var filePath = FilePath;
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                    return;

                var bytes = await e621Client.HttpClient.GetByteArrayAsync(FileUrl);

                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                var directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
                await File.WriteAllBytesAsync(filePath, bytes);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to load image: " + FileUrl);
                Debug.WriteLine(e);
            }
        }
        
        void WebView_OnKeyDown(object? sender, KeyEventArgs e)
        {
            MainWindow.Instance.OnKeyDownHere(sender, e);
        }
        
        void WebView_OnInitialized(object? sender, EventArgs e)
        {
            var handler = new JsEventHandler();
            webView.RegisterJavascriptObject("dotNetHandler", handler);
        }

        private void OnBlacklistTag(object? sender, RoutedEventArgs e) {
            Console.WriteLine("[OnBlacklistTag] invoked");

            if (GetSelectedTag(sender) is string tag) {
                Console.WriteLine($"[OnBlacklistTag] Selected tag = {tag}");
                // Your logic to blacklist the tag
            }
            else {
                Console.WriteLine("[OnBlacklistTag] No tag selected (null)");
            }
        }

        private void OnSearch(object? sender, RoutedEventArgs e) {
            if (GetSelectedTag(sender) is string tag) {
                ListPage.Instance.Search = tag;
                ListPage.Instance.CommitSearch();
            }
            else {
                Console.WriteLine("[OnSearch] No tag selected (null)");
            }
        }

        private void OnAddToSearch(object? sender, RoutedEventArgs e) {
            if (GetSelectedTag(sender) is string tag) {
                ListPage.Instance.Search += " " + tag;
            }
            else {
                Console.WriteLine("[OnAddToSearch] No tag selected (null)");
            }
        }

        private void OnRemoveFromSearch(object? sender, RoutedEventArgs e) {
            if (GetSelectedTag(sender) is string tag) {
                ListPage.Instance.Search += $" -{tag}";
            }
            else {
                Console.WriteLine("[OnRemoveFromSearch] No tag selected (null)");
            }
        }

        private string? GetSelectedTag(object? sender) {
            if (sender is not MenuItem mi)
                return null;

            // climb the logical tree until we find the ContextMenu
            var current = mi as ILogical;
            ContextMenu? cm = null;

            while (current != null) {
                if (current is ContextMenu found) {
                    cm = found;
                    break;
                }
                current = current.LogicalParent;
            }

            if (cm == null)
                return null;

            // Identify which context menu we are in
            ListBox? lb = cm.Name switch {
                "ArtistsContextMenu" => ArtistsList,
                "CharactersContextMenu" => CharactersList,
                "TagsContextMenu" => TagsList,
                _ => null
            };

            return lb?.SelectedItem?.ToString();
        }

        public class JsEventHandler
        {
            public void Notify(string message)
            {
                var keyEvent = JsonSerializer.Deserialize<KeyEventData>(message);
                if (keyEvent != null)
                    Dispatcher.UIThread.Post(() => { MainWindow.Instance.OnKeyDown(keyEvent.Key); });
            }
        }
        
        public class KeyEventData
        {
            [JsonPropertyName("key")]
            public string Key { get; set; }
            [JsonPropertyName("code")]
            public string Code { get; set; }
            [JsonPropertyName("ctrl")]
            public bool Ctrl { get; set; }
            [JsonPropertyName("shift")]
            public bool Shift { get; set; }
            [JsonPropertyName("alt")]
            public bool Alt { get; set; }
            [JsonPropertyName("meta")]
            public bool Meta { get; set; }
        }
    }
}

