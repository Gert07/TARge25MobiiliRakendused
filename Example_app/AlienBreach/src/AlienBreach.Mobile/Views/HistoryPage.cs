using AlienBreach.Services;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;

namespace AlienBreach.Views;

public sealed class HistoryPage : HudPage
{
    private readonly IGameRepository repository;
    private readonly Label mapName = Theme.Text("", 20, Theme.Mint);
    private readonly Label date = Theme.Text("", 12, Theme.Muted);
    private readonly Label wave = Theme.Text(L.T("Loading"), 44);
    private readonly Label outcome = Theme.Text("", 12, Theme.Muted);
    private readonly Label kills = Theme.Text("—", 24);
    private readonly Label credits = Theme.Text("—", 24, Theme.Amber);
    private readonly Label duration = Theme.Text("—", 24);
    private readonly Label counter = Theme.Text("—", 12, Theme.Muted);
    private readonly Button previous = Theme.Action(L.T("Previous"));
    private readonly Button next = Theme.Action(L.T("Next"));
    private readonly Grid metrics;
    private IReadOnlyList<RunResult> runs = [];
    private int index;

    public HistoryPage(IGameRepository repository) : base(L.T("MissionLog"))
    {
        this.repository = repository;
        var layout = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Star), new(GridLength.Auto) }, RowSpacing = 12
        };
        var info = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            RowSpacing = 12
        };
        info.Add(new VerticalStackLayout { Spacing = 4, Children = { mapName, date } }, 0, 0);
        wave.HorizontalTextAlignment = TextAlignment.Center;
        outcome.HorizontalTextAlignment = TextAlignment.Center;
        var summary = new VerticalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center, Children = { wave, outcome } };
        info.Add(summary, 0, 1);
        metrics = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8
        };
        metrics.Add(Theme.Metric(kills, L.T("Kills")), 0);
        metrics.Add(Theme.Metric(credits, L.T("Credits")), 1);
        metrics.Add(Theme.Metric(duration, L.T("Time")), 2);
        info.Add(metrics, 0, 2);
        var panel = Theme.Card(info, 24);
        layout.Add(panel, 0, 0);
        var pager = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 12
        };
        counter.HorizontalTextAlignment = TextAlignment.Center;
        counter.VerticalOptions = LayoutOptions.Center;
        previous.IsEnabled = next.IsEnabled = false;
        previous.Clicked += (_, _) => { if (index > 0) { index--; RenderRun(); } };
        next.Clicked += (_, _) => { if (index + 1 < runs.Count) { index++; RenderRun(); } };
        pager.Add(previous, 0);
        pager.Add(counter, 1);
        pager.Add(next, 2);
        layout.Add(pager, 0, 1);
        Body.Content = layout;
        SizeChanged += (_, _) =>
        {
            var compact = Height < 480 || Width < 800;
            wave.FontSize = compact ? 24 : 44;
            mapName.FontSize = compact ? 14 : 20;
            date.FontSize = outcome.FontSize = compact ? 10 : 12;
            panel.Padding = compact ? 10 : 24;
            info.RowSpacing = compact ? 4 : 12;
            layout.RowSpacing = compact ? 8 : 12;
            summary.Spacing = compact ? 4 : 6;
            foreach (var value in new[] { kills, credits, duration }) value.FontSize = compact ? 18 : 24;
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            runs = await repository.GetRecentRunsAsync();
            index = Math.Clamp(index, 0, Math.Max(0, runs.Count - 1));
            RenderRun();
        }
        catch (Exception)
        {
            runs = [];
            RenderRun();
            wave.Text = L.T("LogUnavailable");
            outcome.Text = L.T("LoadHistoryFailed");
        }
    }

    private void RenderRun()
    {
        previous.IsEnabled = index > 0;
        next.IsEnabled = index + 1 < runs.Count;
        metrics.IsVisible = runs.Count > 0;
        if (runs.Count == 0)
        {
            mapName.Text = "";
            date.Text = "";
            wave.Text = L.T("NoMissions");
            outcome.Text = "";
            counter.Text = "0 / 0";
            return;
        }
        var run = runs[index];
        mapName.Text = L.MapName(run.MapId).ToUpper(L.Culture);
        date.Text = run.FinishedAt.ToLocalTime().ToString("dd MMM yyyy  /  HH:mm", L.Culture);
        wave.Text = L.F("WaveFormat", run.Wave);
        outcome.Text = L.Outcome(run.Outcome);
        outcome.TextColor = run.Outcome == "Overrun" ? Color.FromArgb("#FF6874") : Theme.Mint;
        kills.Text = run.Kills.ToString("N0");
        credits.Text = $"+{run.Credits:N0}";
        var elapsed = TimeSpan.FromSeconds(run.Seconds);
        duration.Text = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        counter.Text = $"{index + 1:00} / {runs.Count:00}";
    }
}
