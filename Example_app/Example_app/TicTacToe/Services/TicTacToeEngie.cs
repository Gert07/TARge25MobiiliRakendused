using System;
using System.Collections.Generic;
using System.Text;

namespace Example_app.TicTacToe.Services
{
    public class TicTacToeEngine
    {
        public int BoardSize { get; private set; }
        public string[,] Board { get; private set; } = new string[0, 0];
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
            int bestScore = int.MinValue;
            (int row, int col)? bestMove = null;

            foreach (var move in GetAvailableMoves())
            {
                Board[move.row, move.col] = BotSymbol;
                int score = Minimax(false, 0, int.MinValue, int.MaxValue);
                Board[move.row, move.col] = string.Empty;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private int Minimax(bool isBotTurn, int depth, int alpha, int beta)
        {
            if (CheckWin(out string winner))
                return winner == BotSymbol ? 10 - depth : depth - 10;

            if (IsBoardFull())
                return 0;

            if (isBotTurn)
            {
                int bestScore = int.MinValue;

                foreach (var move in GetAvailableMoves())
                {
                    Board[move.row, move.col] = BotSymbol;
                    int score = Minimax(false, depth + 1, alpha, beta);
                    Board[move.row, move.col] = string.Empty;

                    bestScore = Math.Max(bestScore, score);
                    alpha = Math.Max(alpha, bestScore);
                    if (beta <= alpha) break;
                }

                return bestScore;
            }

            int worstScore = int.MaxValue;

            foreach (var move in GetAvailableMoves())
            {
                Board[move.row, move.col] = HumanSymbol;
                int score = Minimax(true, depth + 1, alpha, beta);
                Board[move.row, move.col] = string.Empty;

                worstScore = Math.Min(worstScore, score);
                beta = Math.Min(beta, worstScore);
                if (beta <= alpha) break;
            }

            return worstScore;
        }

        private List<(int row, int col)> GetAvailableMoves()
        {
            List<(int row, int col)> moves = new();

            // Eelista võrdse tulemusega käikude korral keskkohta ja seejärel nurki.
            int center = BoardSize / 2;
            AddMoveIfAvailable(moves, center, center);

            AddMoveIfAvailable(moves, 0, 0);
            AddMoveIfAvailable(moves, 0, BoardSize - 1);
            AddMoveIfAvailable(moves, BoardSize - 1, 0);
            AddMoveIfAvailable(moves, BoardSize - 1, BoardSize - 1);

            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                    AddMoveIfAvailable(moves, row, col);
            }

            return moves;
        }

        private void AddMoveIfAvailable(List<(int row, int col)> moves, int row, int col)
        {
            if (string.IsNullOrEmpty(Board[row, col]) && !moves.Contains((row, col)))
                moves.Add((row, col));
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
