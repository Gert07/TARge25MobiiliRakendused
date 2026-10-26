using Example_app.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Example_app.Views.Game;

public partial class TripsuPage : ContentPage
{
    private readonly SettingsService _settings;
    private readonly AppDatabase _database;
    private readonly IServiceProvider _services;
    private readonly LanguageService _text = LanguageService.Instance;
    private readonly TicTacToeEngine _engine = new(3);
    private readonly Button[,] _boardButtons = new Button[3, 3];
    private bool _updatingSettings;
    private bool _busy;
    private int _roundVersion;

    public TripsuPage(SettingsService settings, AppDatabase database, IServiceProvider services)
    {
        _settings = settings;
        _database = database;
        _services = services;
        InitializeComponent();
        for (int i = 0; i < 3; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int r = row, c = col;
                var button = new Button
                {
                    FontSize = 32, FontAttributes = FontAttributes.Bold, Margin = 2,
                    BackgroundColor = Colors.LightGray, TextColor = Colors.Black,
                    Padding = 0
                };
                button.Clicked += async (_, _) => await OnCellClickedAsync(r, c);
                _boardButtons[row, col] = button;
                BoardGrid.Add(button, col, row);
            }
        }
        ApplySettings();
        ResetGame();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _settings.Changed += ApplySettings;
        ApplySettings();
        await MakeBotMoveAsync();
    }

    protected override void OnDisappearing()
    {
        _settings.Changed -= ApplySettings;
        _roundVersion++; // Tühistab võimaliku ootel botikäigu, kui lehelt lahkutakse.
        SetBusy(false);
        base.OnDisappearing();
    }

    private void ApplySettings()
    {
        _updatingSettings = true;
        try
        {
            ModePicker.ItemsSource = new[] { _text["TwoPlayers"], _text["VsBot"] };
            ModePicker.SelectedIndex = _settings.Current.GameMode;
            bool vsBot = _settings.Current.GameMode == 1;
            if (_engine.IsVsBot != vsBot)
            {
                _engine.IsVsBot = vsBot;
                ResetGame();
            }
            UpdateStatus();
        }
        finally { _updatingSettings = false; }
    }

    private async Task OnCellClickedAsync(int row, int col)
    {
        if (_busy || (_engine.IsVsBot && _engine.CurrentPlayer != _engine.HumanSymbol)) return;
        if (!_engine.MakeMove(row, col)) return;
        UpdateCell(row, col);
        if (await CheckGameEndAsync()) return;
        _engine.SwitchPlayer();
        UpdateStatus();
        await MakeBotMoveAsync();
    }

    private async Task MakeBotMoveAsync()
    {
        if (_busy || !_engine.IsVsBot || _engine.CurrentPlayer != _engine.BotSymbol) return;
        int version = _roundVersion;
        SetBusy(true);
        StatusLabel.Text = _text["BotThinking"];
        try
        {
            await Task.Delay(400);
            if (version != _roundVersion) return;
            var move = _engine.GetBotMove();
            if (move is { } cell)
            {
                _engine.MakeMove(cell.row, cell.col);
                UpdateCell(cell.row, cell.col);
                if (!await CheckGameEndAsync())
                {
                    _engine.SwitchPlayer();
                    UpdateStatus();
                }
            }
        }
        finally
        {
            if (version == _roundVersion) SetBusy(false);
        }
    }

    private void UpdateCell(int row, int col)
    {
        string symbol = _engine.Board[row, col];
        var button = _boardButtons[row, col];
        button.Text = symbol;
        button.TextColor = symbol == "X" ? Colors.Red : Colors.Blue;
        button.BackgroundColor = Color.FromArgb(symbol == "X" ? "#FFEBEE" : "#E3F2FD");
    }

    private async Task<bool> CheckGameEndAsync()
    {
        bool won = _engine.CheckWin(out string winner);
        if (!won && !_engine.IsBoardFull()) return false;
        int version = _roundVersion;
        SetBusy(true);
        try
        {
            _database.RecordResult(won ? winner : "");
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync(_text["Error"], _text["SaveFailed"], _text["OK"]);
        }
        await DisplayAlertAsync(_text["GameOver"],
            won ? _text.Format("WinnerMessage", winner) : _text["DrawMessage"], _text["Yes"]);
        if (version == _roundVersion) ResetGame();
        return true;
    }

    private void ResetGame()
    {
        _roundVersion++;
        _engine.ResetGame(3);
        _engine.SetStartingPlayer("X");
        foreach (var button in _boardButtons)
        {
            if (button is null) continue;
            button.Text = string.Empty;
            button.BackgroundColor = Colors.LightGray;
        }
        SetBusy(false);
        UpdateStatus();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        GameControls.IsEnabled = !busy;
    }

    private void UpdateStatus() => StatusLabel.Text = _text.Format("PlayerTurn", _engine.CurrentPlayer);
    private void OnNewGameClicked(object sender, EventArgs e) => ResetGame();

    private async void OnWhoStartsClicked(object sender, EventArgs e)
    {
        ResetGame();
        string starter = Random.Shared.Next(2) == 0 ? "X" : "O";
        _engine.SetStartingPlayer(starter);
        SetBusy(true);
        await DisplayAlertAsync(_text["WhoStarts"], _text.Format("StarterMessage", starter), _text["OK"]);
        SetBusy(false);
        UpdateStatus();
        await MakeBotMoveAsync();
    }

    private async void OnModeChanged(object sender, EventArgs e)
    {
        if (_updatingSettings || ModePicker.SelectedIndex < 0) return;
        try { _settings.SetGameMode(ModePicker.SelectedIndex); }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            ApplySettings();
            await DisplayAlertAsync(_text["Error"], _text["SaveFailed"], _text["OK"]);
        }
    }

    private async void OnToggleThemeClicked(object sender, EventArgs e)
    {
        try { _settings.SetTheme(_settings.Current.Theme == "Dark" ? "Light" : "Dark"); }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync(_text["Error"], _text["SaveFailed"], _text["OK"]);
        }
    }

    private async void OnRulesClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<RulesPage>());

    private async void OnStatsClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<StatsPage>());

    private async void OnSettingsClicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());
}

