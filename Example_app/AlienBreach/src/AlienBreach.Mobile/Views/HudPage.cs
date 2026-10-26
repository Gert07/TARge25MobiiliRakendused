using AlienBreach.Services;
using AlienBreach.Rendering;
using AlienBreach.PlatformUI;

namespace AlienBreach.Views;

public abstract class HudPage : ContentPage
{
    protected ContentView Body { get; } = new();
    private bool leaving;
    private readonly Label heading;
    private readonly Button back;

    protected HudPage(string title, bool showHeader = true)
    {
        Title = title;
        SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None;
        NavigationPage.SetHasNavigationBar(this, false);
        NavigationPage.SetHasBackButton(this, false);
        var root = new Grid { SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None };
        root.Add(new GraphicsView { Drawable = new HudBackdrop(), InputTransparent = true });
        var frame = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            RowSpacing = 14, Padding = 20,
            HorizontalOptions = LayoutOptions.Fill
        };
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 14, IsVisible = showHeader
        };
        back = Theme.Action("‹");
        back.WidthRequest = 46;
        back.FontSize = 24;
        back.Padding = 0;
        SemanticProperties.SetDescription(back, L.T("BackDescription"));
        back.Clicked += async (_, _) =>
        {
            if (leaving) return;
            leaving = true;
            try { await Navigation.PopAsync(); }
            finally { leaving = false; }
        };
        header.Add(back, 0);
        heading = new Label { Text = title.ToUpper(L.Culture), FontSize = 19, CharacterSpacing = 2,
            FontAttributes = FontAttributes.Bold, TextColor = Theme.Mint, VerticalOptions = LayoutOptions.Center };
        header.Add(heading, 1);
        frame.Add(header, 0, 0);
        frame.Add(Body, 0, 1);
        root.Add(frame);
        Content = root;
        Loaded += (_, _) =>
        {
            if (!IsVisible) return;
            GameScreenMode.SetActive(true);
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), GameScreenMode.Apply);
        };
        SizeChanged += (_, _) =>
        {
            frame.Padding = Height < 480 ? 12 : 24;
            frame.RowSpacing = Height < 480 ? 8 : 16;
        };
    }

    protected void SetHeading(string title)
    {
        Title = title;
        heading.Text = title.ToUpper(L.Culture);
        SemanticProperties.SetDescription(back, L.T("BackDescription"));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameScreenMode.SetActive(true);
    }
}
