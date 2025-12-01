using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using e621NET;
using e621NET.Data.Posts;

namespace eBrowser
{
    // TODO: Add proper status label for errors and stuff
    public partial class ListPage : UserControl
    {
        public static AppSettings Settings { get; set; } = new();
        public static ListPage Instance { get; set; } = null!;
        public PostsSession session = new("posts.json".ToPersistPath());
        public int Page { get; set; } = 1;
        public string? Search {
            get => SearchBox.Text;
            set => SearchBox.Text = value;
        }
        public ePosts? currentPosts;
        public event EventHandler<PostClickedArgs>? PostClicked;
        
        public List<PostsView> Views = new();
        public PostsView? CurrentView;
        
        public ListPage()
        {
            Instance = this;
            InitializeComponent();
            
            var settingsPath = "settings.json".ToPersistPath();
            if (!File.Exists(settingsPath))
                return;

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath));
            if (settings == null)
                return;

            Settings = settings;
            SortBox.SelectedIndex = settings.SortIndex;
            PostFileNamer.FileNameFormat = settings.NameScheme;
            if (settings.UseCustomPath && settings.CustomPath != null)
                LocalStorage.OverridePersistentPath(settings.CustomPath);
            
            if (Settings.Username == null || string.IsNullOrWhiteSpace(Settings.Username) || Settings.APIKey == null || string.IsNullOrWhiteSpace(Settings.APIKey))
                return;
            
            e621Client.Current.AddCredentials(new e621APICredentials(Settings.Username, Settings.APIKey));
        }
        
        public void SaveSettings()
        {
            if (SortBox == null) return;
            Settings.SortIndex = SortBox.SelectedIndex;
            File.WriteAllText("settings.json".ToPersistPath(), JsonSerializer.Serialize(Settings));
        }
        
        public void InitializeNewState(ePosts data)
        {
            PostPanel.Child = null;
            Views.Clear();
            session = new PostsSession("posts.json".ToPersistPath(), data);
            SetPosts(data);
            session.Save();
        }
        
        public async void SetPosts(ePosts data)
        {
            try
            {
                Page = data.Page;
                SessionDetails.Items.Clear();
                foreach (var page in session.Pages)
                {
                    SessionDetails.Items.Add(new Label()
                    {
                        Content = $"{page.Page} - {page.Status}"
                    });
                }
                
                currentPosts = data;
                SearchBox.Text = data.Query;
                PageLabel.Text = data.Page + "/" + data.MaxPage;
                NextPageButton.IsEnabled = data.Page < data.MaxPage;
                PreviousPageButton.IsEnabled = data.Page > 1;
                session.LastPageFromSession = data.Page;
                session.Save();
                
                var view = Views.Find(val => val.Page == data.Page);
                if (view != null)
                {
                    PostPanel.Child = view;
                    CurrentView = view;
                    CurrentView.UpdateSorting(SortBox.SelectedIndex);
                    return;
                }
                
                var newView = new PostsView(data, SortBox.SelectedIndex);
                Views.Add(newView);
                PostPanel.Child = newView;
                CurrentView = newView;
                await newView.LoadAll();
                
                SessionDetails.Items.Clear();
                foreach (var page in session.Pages)
                {
                    SessionDetails.Items.Add(new Label()
                    {
                        Content = $"{page.Page} - {page.Status}"
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        private void SearchButton_OnClick(object? sender, RoutedEventArgs e)
        {
            CommitSearch();
        }

        public async void CommitSearch() {
            try {
                if (SearchBox.Text == null || string.IsNullOrWhiteSpace(SearchBox.Text)) {
                    Console.WriteLine("Please enter a search query");
                    StatusLabel.IsVisible = true;
                    StatusLabel.Content = "Please enter a search query";
                    return;
                }

                IsEnabled = false;
                try {
                    var intPosts = await e621Client.Current.GetPostsAsync(SearchBox.Text);
                    if (intPosts != null)
                        InitializeNewState(intPosts);
                    StatusLabel.Content = null;
                }
                catch (Exception ex) {
                    Console.WriteLine(ex);
                    StatusLabel.Content = ex.Message;
                }
                IsEnabled = true;
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }
        
        void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_OnClick(sender, e);
            }
        }
        
        void SortBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            CurrentView?.UpdateSorting(SortBox.SelectedIndex);
            SaveSettings();
        }
        
        protected virtual void OnPostClicked(PostClickedArgs e)
        {
            PostClicked?.Invoke(this, e);
        }

        public void ItemClicked(PostClickedArgs args)
        {
            OnPostClicked(args);
        }
        
        void PreviousPageButton_OnClick(object? sender, RoutedEventArgs e) => PreviousPage();
        public async void PreviousPage()
        {
            if (Page == 1)
            {
                Console.WriteLine("You are on the first page");
                return;
            }
            
            IsEnabled = false;
            try
            {
                var previousPage = await session.GetPreviousPage(Page);
                if (previousPage == null)
                {
                    Console.WriteLine("No more pages");
                }
                else
                {
                    Page = previousPage.Page;
                    SetPosts(previousPage);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
            IsEnabled = true;
        }


        void NextPageButton_OnClick(object? sender, RoutedEventArgs e) => NextPage();
        public async void NextPage()
        {
            if (currentPosts == null)
            {
                Console.WriteLine("No posts");
                return;
            }
            
            if (Page == currentPosts.MaxPage)
            {
                Console.WriteLine("You are on the last page");
                return;
            }
            
            IsEnabled = false;
            try
            {
                var nextPage = await session.GetNextPage(Page);
                if (nextPage == null)
                {
                    Console.WriteLine("No more pages");   
                }
                else
                {
                    Page = nextPage.Page;
                    SetPosts(nextPage);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }
            IsEnabled = true;
        }
        
        void SettingsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenSettings();
        }
    }

    public class PostClickedArgs(ePosts posts, int index) : EventArgs
    {
        public ePosts Posts { get; set; } = posts;
        public int Index { get; set; } = index;
    }

    public class AppSettings
    {
        [JsonPropertyName("sort_index")]
        public int SortIndex { get; set; }
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        [JsonPropertyName("api_key")]
        public string? APIKey { get; set; }

        [JsonPropertyName("hide_to_tray")]
        public bool HideToTray { get; set; } = true;
        [JsonPropertyName("autoplay_videos")]
        public bool AutoplayVideos { get; set; } = true;
        [JsonPropertyName("automute_videos")]
        public bool AutomuteVideos { get; set; } = true;
        [JsonPropertyName("auto_download_images")]
        public bool AutoDownloadImages { get; set; } = true;
        [JsonPropertyName("auto_download_videos")]
        public bool AutoDownloadVideos { get; set; } = true;

        [JsonPropertyName("use_custom_path")]
        public bool UseCustomPath { get; set; } = false;
        [JsonPropertyName("custom_path")]
        public string? CustomPath { get; set; }
        [JsonPropertyName("name_scheme")]
        public string NameScheme { get; set; } = "{artist}-{id}{ext}";

        [JsonPropertyName("blacklisted_tags")]
        public List<string> BlacklistedTags { get; set; } = new();
    }
}

