using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using e621NET.Data.Posts;

namespace eBrowser;

public partial class MainWindow : Window
{
    public static bool ForceClose = false;
    public static MainWindow Instance = null!;
    public static MenuMode mode = MenuMode.Home;
    readonly HomePage _homePage = new();
    readonly ListPage _listPage = new();
    readonly ViewPage _viewPage = new();
    readonly SettingsPage _settingsPage = new();

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        if (Design.IsDesignMode) return;
        
        Initialize();
        AddHandler(KeyDownEvent, OnKeyDownHere, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    async void Initialize()
    {
        try
        {
            var session = PostsSession.GetSession("posts.json".ToPersistPath());
            if (session != null)
            {
                session.Path = "posts.json".ToPersistPath();
                _listPage.session = session;
                var posts = session.LastPageFromSession < 1 ? await session.GetPageAsync(1) : await session.GetPageAsync(session.LastPageFromSession);
                if (posts != null)
                {
                    _listPage.SetPosts(posts);
                    Content = _listPage;
                    mode = MenuMode.Listing;
                }
                else
                {
                    Content = _homePage;
                }
            }
            else
            {
                Content = _homePage;
            }

            _homePage.onSearchFinished += HomePageSearchFinished;
            _listPage.PostClicked += ListPageOnPostClicked;
            _viewPage.onBackPressed += ViewPageOnBackPressed;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    void ViewPageOnBackPressed()
    {
        Content = _listPage;
        mode = MenuMode.Listing;
    }

    void ListPageOnPostClicked(object? sender, PostClickedArgs e)
    {
        _viewPage.LoadPost(e.Posts, e.Index);
        Content = _viewPage;
        mode = MenuMode.Viewer;
    }

    void HomePageSearchFinished(ePosts obj)
    {
        Content = _listPage;
        _listPage.InitializeNewState(obj);
        mode = MenuMode.Listing;
    }

    public void BackFromSettings()
    {
        Content = _listPage;
        mode = MenuMode.Listing;
    }
    
    public void OpenSettings()
    {
        Content = _settingsPage;
        _settingsPage.RefreshSettings();
        mode = MenuMode.Settings;
    }

    private static bool IsTextInputFocused(Visual? scope) {
        var focused = GetTopLevel(scope)?.FocusManager?.GetFocusedElement();

        return focused is TextBox or ComboBox;
    }

    public void OnKeyDownHere(object? sender, KeyEventArgs e) {
        if (IsTextInputFocused(this))
            return;

        switch (e.Key)
        {
            case Key.Right:
            {
                if (Equals(Content, _listPage) && _listPage.IsEnabled)
                {
                    e.Handled = true;
                    _listPage.NextPage();
                }
                else if (Equals(Content, _viewPage) && _viewPage.IsEnabled)
                {
                    e.Handled = true;
                    _viewPage.NextPage();
                }

                break;
            }
            case Key.Left:
            {
                if (Equals(Content, _listPage) && _listPage.IsEnabled)
                {
                    e.Handled = true;
                    _listPage.PreviousPage();
                }
                else if (Equals(Content, _viewPage) && _viewPage.IsEnabled)
                {
                    e.Handled = true;
                    _viewPage.PreviousPage();
                }

                break;
            }
            case Key.Back:
            case Key.Escape:
            {
                if (Equals(Content, _viewPage) && _viewPage.IsEnabled)
                {
                    e.Handled = true;
                    _viewPage.BackButton_OnClick(sender, e);
                }

                break;
            }
            case Key.Enter:
            {
                if (Equals(Content, _listPage) && _listPage is { IsEnabled: true, CurrentView.Items.Count: > 0 })
                {
                    e.Handled = true;
                    var item = _listPage.CurrentView.Items[0];
                    ListPageOnPostClicked(this, new PostClickedArgs(item.Posts, item.Index));
                }

                break;
            }
        }
    }

    public void OnKeyDown(string keyEvent) {
        if (IsTextInputFocused(this))
            return;

        Console.WriteLine($"[Browser] {keyEvent}");
        if (keyEvent.Contains("Right"))
        {
            if (Equals(Content, _listPage) && _listPage.IsEnabled)
            {
                _listPage.NextPage();
            } else if (Equals(Content, _viewPage) && _viewPage.IsEnabled)
            {
                _viewPage.NextPage();
            }
        }
        else if (keyEvent.Contains("Left"))
        {
            if (Equals(Content, _listPage) && _listPage.IsEnabled)
            {
                _listPage.PreviousPage();
            } else if (Equals(Content, _viewPage) && _viewPage.IsEnabled)
            {
                _viewPage.PreviousPage();
            }
        }
        else if (keyEvent.Contains("Escape") || keyEvent.Contains("Backspace"))
        {
            if (Equals(Content, _viewPage) && _viewPage.IsEnabled)
                _viewPage.BackButton_OnClick(this, new RoutedEventArgs());
        }
    }
    void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (ForceClose || !ListPage.Settings.HideToTray)
            return;

        e.Cancel = true;
        Hide();
    }
}

public enum MenuMode
{
    Home,
    Listing,
    Viewer,
    Settings
}
