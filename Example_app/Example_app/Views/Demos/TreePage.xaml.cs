using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Example_app;

public partial class TreePage : ContentPage
{
    public TreePage()
    {
        InitializeComponent();
    }

    // --- PEAMINE TEGEVUSTE KÄIVITAMINE ---
    private async void OnRunButtonClicked(object? sender, EventArgs e)
    {
        if (ActionPicker == null ||
            InfoLabel == null ||
            SpeedStepper == null ||
            TreeContainer == null ||
            Leaves == null)
        {
            return;
        }

        if (ActionPicker.SelectedItem is not string selectedAction)
        {
            InfoLabel.Text = "Palun vali tegevus!";
            InfoLabel.TextColor = Colors.Red;

            await DisplayAlertAsync(
                "Viga",
                "Palun vali tegevus rippmenüüst!",
                "OK");

            return;
        }

        uint duration = (uint)SpeedStepper.Value;

        InfoLabel.TextColor = Colors.DarkSlateGray;

        switch (selectedAction)
        {
            case "Kasva":
                {
                    InfoLabel.Text = "Puu kasvab!";

                    // Alati alustame normaalsest suurusest
                    TreeContainer.Scale = 1.0;

                    // Kasvab natuke suuremaks
                    await TreeContainer.ScaleToAsync(
                        1.2,
                        duration,
                        Easing.CubicOut);

                    break;
                }

            case "Õitse":
                {
                    InfoLabel.Text = "Puu õitseb!";

                    Leaves.BackgroundColor = Colors.HotPink;

                    await Leaves.ScaleToAsync(
                        1.15,
                        duration / 2,
                        Easing.CubicOut);

                    await Leaves.ScaleToAsync(
                        1.0,
                        duration / 2,
                        Easing.CubicIn);

                    break;
                }

            case "Värise":
                {
                    InfoLabel.Text = "Puu väriseb ja õunad kukuvad!";

                    double originalX = TreeContainer.TranslationX;

                    uint stepDuration = Math.Max(50, duration / 6);

                    // Puu väriseb
                    Task treeShake = ShakeTreeAsync(
                        TreeContainer,
                        originalX,
                        stepDuration);

                    // Õunad kukuvad
                    Task appleFall = DropApplesAsync(
                        Apple1,
                        Apple2,
                        duration);

                    await Task.WhenAll(
                        treeShake,
                        appleFall);

                    break;
                }

            case "Langeta":
                {
                    DateTime selectedDate =
                        SeasonDatePicker?.Date ?? DateTime.Now;

                    TimeSpan selectedTime =
                        TimeOfDayPicker?.Time ?? TimeSpan.Zero;

                    int month = selectedDate.Month;

                    bool isWinter =
                        month == 12 ||
                        month == 1 ||
                        month == 2;

                    int hour = selectedTime.Hours;

                    bool isDaytime =
                        hour >= 8 &&
                        hour < 17;

                    if (!isWinter || !isDaytime)
                    {
                        InfoLabel.Text =
                            "Langetada saab ainult talvel ja valgel ajal (08:00–17:00).";

                        InfoLabel.TextColor = Colors.Red;

                        await DisplayAlertAsync(
                            "Keelatud",
                            "Puid tohib langetada ainult talvel ja valgel ajal!",
                            "Aru saadud");

                        return;
                    }

                    InfoLabel.Text = "Puu langeb!";

                    TreeContainer.AnchorX = 0.5;
                    TreeContainer.AnchorY = 1.0;

                    await TreeContainer.RotateToAsync(
                        90,
                        duration,
                        Easing.CubicIn);

                    break;
                }
        }
    }
    //--- PUU VÄRISEMINE ----
    private async Task ShakeTreeAsync(
    View tree,
    double originalX,
    uint stepDuration)
    {
        await tree.TranslateToAsync(
            originalX - 15,
            0,
            stepDuration);

        await tree.TranslateToAsync(
            originalX + 15,
            0,
            stepDuration);

        await tree.TranslateToAsync(
            originalX - 12,
            0,
            stepDuration);

        await tree.TranslateToAsync(
            originalX + 12,
            0,
            stepDuration);

        await tree.TranslateToAsync(
            originalX - 6,
            0,
            stepDuration);

        await tree.TranslateToAsync(
            originalX,
            0,
            stepDuration);
    }
    //--- ÕUNTE KUKKUMINE ---
    private async Task DropApplesAsync(
    View apple1,
    View apple2,
    uint duration)
    {
        await Task.Delay((int)(duration * 0.25));

        Task fall1 = DropAppleAsync(
            apple1,
            -20);

        Task fall2 = DropAppleAsync(
            apple2,
            20);

        await Task.WhenAll(
            fall1,
            fall2);
    }

    private async Task DropAppleAsync(
    View apple,
    double horizontalOffset)
    {
        await Task.WhenAll(
            apple.TranslateToAsync(
                horizontalOffset,
                220,
                700,
                Easing.BounceOut),

            apple.RotateToAsync(
                360,
                700),

            apple.FadeToAsync(
                0,
                700));
    }

    // --- ÕUNA KLÕPSAMINE ---
    private async void OnAppleTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (sender is not View apple ||
            SpeedStepper == null)
        {
            return;
        }

        uint duration = (uint)SpeedStepper.Value;

        await apple.TranslateToAsync(
            apple.TranslationX,
            220,
            duration,
            Easing.BounceOut);

        await apple.FadeToAsync(0, 250);

        apple.IsVisible = false;
    }

    // --- PÄIKESE JA KUU LIIKUMINE ---
    private async void OnTimeOfDayChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != TimePicker.TimeProperty.PropertyName)
            return;

        if (Sun == null ||
            Moon == null ||
            SkyLayout == null ||
            InfoLabel == null ||
            TimeOfDayPicker == null)
        {
            return;
        }

        TimeSpan time = TimeOfDayPicker.Time ?? TimeSpan.Zero;

        double totalHours = time.TotalHours;

        if (totalHours >= 6 && totalHours <= 18)
        {
            Sun.IsVisible = true;
            Moon.IsVisible = false;
            SkyLayout.BackgroundColor = Colors.LightBlue;

            double progress = (totalHours - 6) / 12.0;

            // Päike liigub kõrgemal kaarega
            double x = progress;
            double y = 0.15 - (0.12 * Math.Sin(progress * Math.PI));

            Rect newBounds = new Rect(
                x,
                y,
                50,
                50);

            // Kui päike oli juba nähtav, liiguta sujuvalt uude kohta
            if (Sun.IsVisible)
            {
                Rect oldBounds =
                    AbsoluteLayout.GetLayoutBounds(Sun);

                double oldX = oldBounds.X;
                double oldY = oldBounds.Y;

                const int steps = 20;

                for (int i = 1; i <= steps; i++)
                {
                    double t = (double)i / steps;

                    double animatedX =
                        oldX + ((x - oldX) * t);

                    double animatedY =
                        oldY + ((y - oldY) * t);

                    AbsoluteLayout.SetLayoutBounds(
                        Sun,
                        new Rect(
                            animatedX,
                            animatedY,
                            50,
                            50));

                    await Task.Delay(15);
                }
            }
            else
            {
                AbsoluteLayout.SetLayoutBounds(
                    Sun,
                    newBounds);
            }
        }
        else
        {
            Sun.IsVisible = false;
            Moon.IsVisible = true;
            SkyLayout.BackgroundColor = Colors.Navy;

            double nightProgress =
                totalHours > 18
                    ? (totalHours - 18) / 12.0
                    : (totalHours + 6) / 12.0;

            double x = nightProgress;
            double y =
                0.15 -
                (0.12 * Math.Sin(nightProgress * Math.PI));

            AbsoluteLayout.SetLayoutBounds(
                Moon,
                new Rect(
                    x,
                    y,
                    45,
                    45));
        }

        InfoLabel.Text =
            $"Määratud kellaaeg: {time:hh\\:mm}";
    }

    // --- MUUD SÜNDMUSED ---
    private void OnSpeedStepperValueChanged(
        object? sender,
        ValueChangedEventArgs e)
    {
        if (SpeedLabel != null)
        {
            SpeedLabel.Text =
                $"Kiirus: {e.NewValue} ms";
        }
    }

    private void OnSeasonDateSelected(
        object? sender,
        DateChangedEventArgs e)
    {
        if (InfoLabel != null)
        {
            InfoLabel.Text =
                $"Määratud kuupäev: {e.NewDate:dd.MM.yyyy}";
        }
    }

    // ---- RESET BUTTON ---
    private async void OnResetButtonClicked(object? sender, EventArgs e)
    {
        if (TreeContainer == null ||
            Leaves == null ||
            Apple1 == null ||
            Apple2 == null)
        {
            return;
        }

        InfoLabel.Text = "Puu on lähtestatud.";
        InfoLabel.TextColor = Colors.DarkSlateGray;

        // Taasta puu
        await Task.WhenAll(
            TreeContainer.ScaleToAsync(
                1.0,
                300,
                Easing.CubicOut),

            TreeContainer.RotateToAsync(
                0,
                300,
                Easing.CubicOut),

            TreeContainer.TranslateToAsync(
                0,
                0,
                300,
                Easing.CubicOut),

            Leaves.ScaleToAsync(
                1.0,
                300,
                Easing.CubicOut)
        );

        // Taasta võra
        Leaves.BackgroundColor = Colors.ForestGreen;

        // Taasta Apple 1
        Apple1.CancelAnimations();
        Apple1.TranslationX = 0;
        Apple1.TranslationY = 0;
        Apple1.Opacity = 1;
        Apple1.IsVisible = true;

        // Taasta Apple 2
        Apple2.CancelAnimations();
        Apple2.TranslationX = 0;
        Apple2.TranslationY = 0;
        Apple2.Opacity = 1;
        Apple2.IsVisible = true;

    }

    // ---- AASTAAJAD ---
    private void OnSeasonChanged(object? sender, EventArgs e)
    {
        if (SeasonPicker == null ||
            Leaves == null ||
            InfoLabel == null)
        {
            return;
        }

        if (SeasonPicker.SelectedItem is not string season)
            return;

        switch (season)
        {
            case "Kevad":
                Leaves.BackgroundColor = Colors.LightGreen;
                InfoLabel.Text = "Kevad – puu läheb roheliseks.";
                break;

            case "Suvi":
                Leaves.BackgroundColor = Colors.ForestGreen;
                InfoLabel.Text = "Suvi – puu on täies roheluses.";
                break;

            case "Sügis":
                Leaves.BackgroundColor = Colors.OrangeRed;
                InfoLabel.Text = "Sügis – puu lehed muutuvad oranžiks.";
                break;

            case "Talv":
                Leaves.BackgroundColor = Colors.LightGray;
                InfoLabel.Text = "Talv – puu puhkab.";
                break;
        }
    }
}
