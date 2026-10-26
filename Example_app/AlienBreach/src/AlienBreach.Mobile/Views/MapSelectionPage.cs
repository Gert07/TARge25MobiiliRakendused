using AlienBreach.Services;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;
using AlienBreach.Rendering;

namespace AlienBreach.Views;

public sealed class MapSelectionPage : HudPage
{
    private bool navigating;

    public MapSelectionPage(IGameRepository repository, SettingsService settings, GameAudioService audio) : base(L.T("SelectSector"))
    {
        var maps = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 14
        };
        var titles = new List<Label>();
        var buttons = new List<Button>();
        var panels = new List<Border>();
        foreach (var map in MapCatalog.All)
        {
            var title = Theme.Text(L.MapName(map.Id), 24);
            title.MaxLines = 1;
            title.LineBreakMode = LineBreakMode.TailTruncation;
            titles.Add(title);
            var heading = new VerticalStackLayout { Spacing = 5, Children =
            { Theme.Text(L.T("Difficulty_" + map.Difficulty), 10, GameRenderer.MapColor(map.Theme)), title } };
            var select = Theme.Action(L.T("Select"), true);
            buttons.Add(select);
            select.Clicked += async (_, _) =>
            {
                if (navigating) return;
                navigating = true;
                foreach (var button in buttons) button.IsEnabled = false;
                try { await Navigation.PushAsync(new MapDetailsPage(repository, map, settings, audio)); }
                finally
                {
                    navigating = false;
                    foreach (var button in buttons) button.IsEnabled = true;
                }
            };
            var card = new Grid
            {
                RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
                RowSpacing = 12
            };
            card.Add(heading, 0, 0);
            card.Add(new GraphicsView { Drawable = new GameRenderer(new GameEngine(PlayerProfile.New, map)), InputTransparent = true }, 0, 1);
            card.Add(select, 0, 2);
            var panel = Theme.Card(card, 18);
            panels.Add(panel);
            maps.Add(panel, panels.Count - 1);
        }
        Body.Content = maps;
        SizeChanged += (_, _) =>
        {
            var compact = Height < 480 || Width < 800;
            maps.ColumnSpacing = compact ? 8 : 14;
            foreach (var title in titles) title.FontSize = compact ? 17 : 24;
            foreach (var panel in panels) panel.Padding = compact ? 10 : 18;
            foreach (var button in buttons) button.Padding = compact ? new Thickness(6) : new Thickness(12, 10);
        };
    }
}
