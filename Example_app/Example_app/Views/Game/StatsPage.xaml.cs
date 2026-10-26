using Example_app.Services;
using Microsoft.Data.Sqlite;

namespace Example_app.Views.Game;

public partial class StatsPage : ContentPage
{
    private readonly AppDatabase _database;
    private readonly LanguageService _text = LanguageService.Instance;

    public StatsPage(AppDatabase database)
    {
        _database = database;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { LoadStats(); }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync(_text["Error"], _text["SaveFailed"], _text["OK"]);
        }
    }

    private void LoadStats()
    {
        var stats = _database.LoadStats();
        XWinsLabel.Text = _text.Format("XWins", stats.XWins);
        OWinsLabel.Text = _text.Format("OWins", stats.OWins);
        DrawsLabel.Text = _text.Format("Draws", stats.Draws);
    }

    private async void OnResetStatsClicked(object sender, EventArgs e)
    {
        try
        {
            _database.ResetStats();
            LoadStats();
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync(_text["Error"], _text["SaveFailed"], _text["OK"]);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();
}

