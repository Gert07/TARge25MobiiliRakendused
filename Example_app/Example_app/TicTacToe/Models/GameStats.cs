using System;
using System.Collections.Generic;
using System.Text;

namespace Example_app.TicTacToe.Models
{
    public static class GameStats
    {
        private const string KeyXWins = "stats_x_wins";
        private const string KeyOWins = "stats_o_wins";
        private const string KeyDraws = "stats_draws";

        public static int XWins
        {
            get => Preferences.Default.Get(KeyXWins, 0);
            set => Preferences.Default.Set(KeyXWins, value);
        }

        public static int OWins
        {
            get => Preferences.Default.Get(KeyOWins, 0);
            set => Preferences.Default.Set(KeyOWins, value);
        }

        public static int Draws
        {
            get => Preferences.Default.Get(KeyDraws, 0);
            set => Preferences.Default.Set(KeyDraws, value);
        }

        public static void Reset()
        {
            Preferences.Default.Remove(KeyXWins);
            Preferences.Default.Remove(KeyOWins);
            Preferences.Default.Remove(KeyDraws);
        }
    }
}
