using AlienBreach.Services;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;
using AlienBreach.Rendering;

namespace AlienBreach.Views;

public sealed class MapDetailsPage : HudPage
{
    private readonly IGameRepository repository;
    private readonly BattleMap map;
    private readonly Label bestWave = Theme.Text("—", 24, Theme.Mint);
    private readonly Label deployments = Theme.Text("—", 24);
    private readonly Label kills = Theme.Text("—", 24);
    private readonly Button deploy = Theme.Action(L.T("Deploy"), true);
    private readonly Label error = Theme.Text("", 12, Theme.Amber);
    private PlayerProfile profile = PlayerProfile.New;
    private bool loaded;
    private bool navigating;

    public MapDetailsPage(IGameRepository repository, BattleMap map, SettingsService settings, GameAudioService audio) : base(L.MapName(map.Id))
    {
        this.repository = repository;
        this.map = map;
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(300) }, ColumnSpacing = 16
        };
        var preview = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, RowSpacing = 12
        };
        preview.Add(new GraphicsView { Drawable = new GameRenderer(new GameEngine(PlayerProfile.New, map)), InputTransparent = true }, 0, 0);
        var description = Theme.Text(L.T("Description_" + map.Id), 14, Theme.Muted);
        description.MaxLines = 3;
        preview.Add(description, 0, 1);
        var previewPanel = Theme.Card(preview);
        layout.Add(previewPanel, 0);

        var metrics = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
            RowSpacing = 8, ColumnSpacing = 8
        };
        metrics.Add(Theme.Metric(Theme.Text($"${map.StartingCash}", 24, Theme.Amber), L.T("Supplies")), 0, 0);
        metrics.Add(Theme.Metric(bestWave, L.T("BestWave")), 1, 0);
        metrics.Add(Theme.Metric(deployments, L.T("Runs")), 0, 1);
        metrics.Add(Theme.Metric(kills, L.T("BestKills")), 1, 1);
        var info = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) }, RowSpacing = 12
        };
        info.Add(Theme.Text(L.T("Difficulty_" + map.Difficulty), 11, GameRenderer.MapColor(map.Theme)), 0, 0);
        info.Add(metrics, 0, 1);
        error.IsVisible = false;
        info.Add(error, 0, 2);
        deploy.IsEnabled = false;
        deploy.Clicked += async (_, _) =>
        {
            if (!loaded || navigating) return;
            navigating = true;
            deploy.IsEnabled = false;
            try { await Navigation.PushAsync(new MissionPage(repository, profile, map, settings, audio)); }
            finally { navigating = false; deploy.IsEnabled = loaded; }
        };
        info.Add(deploy, 0, 3);
        var infoPanel = Theme.Card(info);
        layout.Add(infoPanel, 1);
        Body.Content = layout;
        SizeChanged += (_, _) =>
        {
            var compact = Height < 480 || Width < 800;
            layout.ColumnDefinitions[1].Width = compact ? 224 : 300;
            layout.ColumnSpacing = compact ? 10 : 16;
            infoPanel.Padding = previewPanel.Padding = compact ? 12 : 18;
            info.RowSpacing = compact ? 7 : 12;
            description.FontSize = compact ? 12 : 14;
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        deploy.IsEnabled = false;
        error.IsVisible = false;
        try
        {
            profile = await repository.GetProfileAsync();
            var records = await repository.GetMapRecordsAsync();
            var record = records.FirstOrDefault(record => record.MapId == map.Id);
            bestWave.Text = (record?.BestWave ?? 0).ToString("00");
            deployments.Text = (record?.Runs ?? 0).ToString("N0");
            kills.Text = (record?.BestKills ?? 0).ToString("N0");
            loaded = true;
        }
        catch (Exception)
        {
            loaded = false;
            error.Text = L.T("LoadSectorFailed");
            error.IsVisible = true;
        }
        finally { deploy.IsEnabled = loaded && !navigating; }
    }
}
