using AlienBreach.Services;
using AlienBreach.Core.Persistence;

namespace AlienBreach.Views;

public sealed class RecordsPage : HudPage
{
    private readonly IGameRepository repository;
    private readonly Label bestWave = Theme.Text("—", 52, Theme.Mint);
    private readonly Label kills = Theme.Text("—", 52);
    private readonly Label deployments = Theme.Text("—", 52);
    private readonly Label error = Theme.Text("", 13, Theme.Amber);

    public RecordsPage(IGameRepository repository) : base(L.T("Records"))
    {
        this.repository = repository;
        var layout = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, RowSpacing = 12
        };
        var stats = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 14
        };
        var panels = new[]
        {
            Theme.Card(Theme.Metric(bestWave, L.T("BestWave"))),
            Theme.Card(Theme.Metric(kills, L.T("AliensDown"))),
            Theme.Card(Theme.Metric(deployments, L.T("Deployments")))
        };
        for (var i = 0; i < panels.Length; i++) stats.Add(panels[i], i);
        layout.Add(stats, 0, 0);
        error.IsVisible = false;
        layout.Add(error, 0, 1);
        Body.Content = layout;
        SizeChanged += (_, _) =>
        {
            var compact = Height < 480 || Width < 800;
            foreach (var value in new[] { bestWave, kills, deployments }) value.FontSize = compact ? 34 : 52;
            foreach (var panel in panels) panel.Padding = compact ? 10 : 18;
            stats.ColumnSpacing = compact ? 8 : 14;
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        error.IsVisible = false;
        try
        {
            var profile = await repository.GetProfileAsync();
            bestWave.Text = profile.BestWave.ToString("00");
            kills.Text = profile.TotalKills.ToString("N0");
            deployments.Text = profile.Runs.ToString("N0");
        }
        catch (Exception)
        {
            error.Text = L.T("LoadRecordsFailed");
            error.IsVisible = true;
        }
    }
}
