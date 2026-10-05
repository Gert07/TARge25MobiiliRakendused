using System;
using Microsoft.Maui.Controls;
using Example_app.TicTacToe.Models;
using Example_app.TicTacToe.Services;

namespace Example_app.TicTacToe
{
    public partial class TripsuPage : ContentPage
    {
        // 1. Lisatud puuduvad väljad
        private TicTacToeEngine _engine;
        private Button[,] _boardButtons;
        private bool _isBotThinking;

        public TripsuPage()
        {
            InitializeComponent();
            _engine = new TicTacToeEngine(3);
            BuildBoardUI();
            ModePicker.SelectedIndex = 0;
        }

        private void BuildBoardUI()
        {
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            int size = _engine.BoardSize;
            _boardButtons = new Button[size, size];

            for (int i = 0; i < size; i++)
            {
                BoardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    Button btn = new Button
                    {
                        FontSize = 32,
                        FontAttributes = FontAttributes.Bold,
                        Margin = 2,
                        BackgroundColor = Colors.LightGray,
                        TextColor = Colors.Black
                    };

                    int row = r;
                    int col = c;
                    btn.Clicked += (s, e) => OnCellClicked(row, col);

                    _boardButtons[r, c] = btn;
                    BoardGrid.Add(btn, col, row);
                }
            }

            UpdateStatusLabel();
        }

        private async void OnCellClicked(int row, int col)
        {
            if (_isBotThinking ||
                (_engine.IsVsBot && _engine.CurrentPlayer != _engine.HumanSymbol))
                return;

            if (!_engine.MakeMove(row, col)) return;

            UpdateCellUI(row, col);

            if (CheckGameEnd()) return;

            _engine.SwitchPlayer();
            UpdateStatusLabel();

            await MakeBotMoveAsync();
        }

        private async Task MakeBotMoveAsync()
        {
            if (!_engine.IsVsBot || _engine.CurrentPlayer != _engine.BotSymbol)
                return;

            _isBotThinking = true;
            StatusLabel.Text = "Bot mõtleb...";

            try
            {
                await System.Threading.Tasks.Task.Delay(400); // Väike viivitus parema tunnetuse jaoks
                var botMove = _engine.GetBotMove();
                if (botMove.HasValue)
                {
                    _engine.MakeMove(botMove.Value.row, botMove.Value.col);
                    UpdateCellUI(botMove.Value.row, botMove.Value.col);

                    if (!CheckGameEnd())
                    {
                        _engine.SwitchPlayer();
                        UpdateStatusLabel();
                    }
                }
            }
            finally
            {
                _isBotThinking = false;
            }
        }

        private void UpdateCellUI(int row, int col)
        {
            string symbol = _engine.Board[row, col];
            Button btn = _boardButtons[row, col];
            btn.Text = symbol;

            // Värvide määramine sümbolite järgi
            if (symbol == "X")
            {
                btn.TextColor = Colors.Red;
                btn.BackgroundColor = Color.FromArgb("#FFEBEE");
            }
            else if (symbol == "O")
            {
                btn.TextColor = Colors.Blue;
                btn.BackgroundColor = Color.FromArgb("#E3F2FD");
            }
        }

        private bool CheckGameEnd()
        {
            if (_engine.CheckWin(out string winner))
            {
                if (winner == "X") GameStats.XWins++;
                else if (winner == "O") GameStats.OWins++;

                DisplayAlert("Mäng läbi!", $"{winner} võitis! Kas soovid veel mängida?", "Jah");
                ResetGame();
                return true;
            }

            if (_engine.IsBoardFull())
            {
                GameStats.Draws++;
                DisplayAlert("Mäng läbi!", "Tekkis viik! Kas soovid veel mängida?", "Jah");
                ResetGame();
                return true;
            }

            return false;
        }

        private void ResetGame()
        {
            _engine.ResetGame(_engine.BoardSize);
            _engine.SetStartingPlayer("X");
            for (int r = 0; r < _engine.BoardSize; r++)
            {
                for (int c = 0; c < _engine.BoardSize; c++)
                {
                    _boardButtons[r, c].Text = string.Empty;
                    _boardButtons[r, c].BackgroundColor = Colors.LightGray;
                }
            }
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            StatusLabel.Text = $"Mängija {_engine.CurrentPlayer} kord";
        }

        private void OnNewGameClicked(object sender, EventArgs e) => ResetGame();

        private async void OnWhoStartsClicked(object sender, EventArgs e)
        {
            ResetGame();
            Random rand = new();
            string starter = rand.Next(2) == 0 ? "X" : "O";
            _engine.SetStartingPlayer(starter);
            await DisplayAlertAsync("Kes alustab?", $"Loosiga alustab mängija: {starter}", "Selge");
            UpdateStatusLabel();
            await MakeBotMoveAsync();
        }

        private void OnModeChanged(object sender, EventArgs e)
        {
            if (ModePicker.SelectedIndex == -1) return;
            _engine.IsVsBot = ModePicker.SelectedIndex == 1;
            ResetGame();
        }

        private void OnToggleThemeClicked(object sender, EventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Dark
                    ? AppTheme.Light
                    : AppTheme.Dark;
            }
        }

        // 2. Parandatud lehtedele navigeerimise kood
        private async void OnRulesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RulesPage());
        }

        private async void OnStatsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new StatsPage());
        }
    }
}
