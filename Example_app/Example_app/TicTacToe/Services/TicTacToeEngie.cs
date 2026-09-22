using System;
using System.Collections.Generic;
using System.Text;

namespace Example_app.TicTacToe.Services
{
    public class TicTacToeEngine
    {
        public int BoardSize { get; private set; }
        public string[,] Board { get; private set; }
        public string CurrentPlayer { get; private set; } = "X";
        public bool IsVsBot { get; set; } = false;
        public string HumanSymbol { get; set; } = "X";
        public string BotSymbol => HumanSymbol == "X" ? "O" : "X";

        public TicTacToeEngine(int size = 3)
        {
            ResetGame(size);
        }

        public void ResetGame(int size)
        {
            BoardSize = size;
            Board = new string[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    Board[r, c] = string.Empty;
        }

        public bool MakeMove(int row, int col)
        {
            if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize) return false;
            if (!string.IsNullOrEmpty(Board[row, col])) return false;

            Board[row, col] = CurrentPlayer;
            return true;
        }

        public void SwitchPlayer()
        {
            CurrentPlayer = CurrentPlayer == "X" ? "O" : "X";
        }

        public void SetStartingPlayer(string symbol)
        {
            CurrentPlayer = symbol;
        }

        public (int row, int col)? GetBotMove()
        {
            List<(int r, int c)> emptyCells = new();
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (string.IsNullOrEmpty(Board[r, c]))
                        emptyCells.Add((r, c));
                }
            }

            if (emptyCells.Count == 0) return null;

            Random rand = new();
            return emptyCells[rand.Next(emptyCells.Count)];
        }

        public bool CheckWin(out string winner)
        {
            winner = string.Empty;

            // Ridade ja tulpade kontroll
            for (int i = 0; i < BoardSize; i++)
            {
                if (IsLineWinning(r => Board[i, r])) { winner = Board[i, 0]; return true; }
                if (IsLineWinning(r => Board[r, i])) { winner = Board[0, i]; return true; }
            }

            // Diagonaalide kontroll
            if (IsLineWinning(r => Board[r, r])) { winner = Board[0, 0]; return true; }
            if (IsLineWinning(r => Board[r, BoardSize - 1 - r])) { winner = Board[0, BoardSize - 1]; return true; }

            return false;
        }

        public bool IsBoardFull()
        {
            foreach (var cell in Board)
            {
                if (string.IsNullOrEmpty(cell)) return false;
            }
            return true;
        }

        private bool IsLineWinning(Func<int, string> getCell)
        {
            string first = getCell(0);
            if (string.IsNullOrEmpty(first)) return false;

            for (int i = 1; i < BoardSize; i++)
            {
                if (getCell(i) != first) return false;
            }
            return true;
        }
    }
}
