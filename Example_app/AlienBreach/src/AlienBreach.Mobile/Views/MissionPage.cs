using AlienBreach.Services;
using System.Diagnostics;
using System.Numerics;
using AlienBreach.Controls;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Models;
using AlienBreach.Core.Persistence;
using AlienBreach.Core.Turrets;
using AlienBreach.PlatformUI;
using AlienBreach.Rendering;

namespace AlienBreach.Views;

public sealed class MissionPage : ContentPage
{
    private readonly IGameRepository repository;
    private readonly GameAudioService audio;
    private readonly GameEngine game;
    private readonly GraphicsView arena;
    private readonly GameRenderer renderer;
    private readonly Label health = Theme.Text("", 12, Theme.Mint);
    private readonly Label wave = Theme.Text("", 12, Theme.Amber);
    private readonly Label cash = Theme.Text("", 12);
    private readonly Button pause = Theme.Action("Ⅱ");
    private readonly Button nextWave = Theme.Action("▶ 01", true);
    private readonly Button speedButton = Theme.Action("1×");
    private readonly Button upgrade = Theme.Action("↑");
    private readonly Button sell = Theme.Action(L.T("Sell"));
    private readonly Label level = Theme.Text("", 12, Theme.Mint);
    private readonly Dictionary<TurretType, TurretIconButton> buildButtons = [];
    private readonly Border selectionPanel;
    private readonly Border toastPanel;
    private readonly Label toast = Theme.Text("", 12, Theme.Amber);
    private readonly Border pausePanel;
    private readonly Border resultPanel;
    private readonly BoxView scrim;
    private readonly Label resultTitle = Theme.Text("", 24, Theme.Mint);
    private readonly Label resultDetails = Theme.Text("", 15);
    private readonly Label saveStatus = Theme.Text(L.T("SavingRecords"), 13, Theme.Muted);
    private readonly Button returnButton = Theme.Action(L.T("ReturnToCommand"), true);
    private readonly Button retryButton = Theme.Action(L.T("RetrySave"));
    private IDispatcherTimer? timer;
    private long lastFrame;
    private long toastUntil;
    private double hudTime;
    private int speed = 1;
    private bool saving;
    private bool saved;
    private Vector2? touchPosition;

    public MissionPage(IGameRepository repository, PlayerProfile profile, BattleMap map, SettingsService settings, GameAudioService audio)
    {
        this.repository = repository;
        this.audio = audio;
        speed = settings.Current.GameSpeed;
        speedButton.Text = $"{speed}×";
        game = new GameEngine(profile, map);
        renderer = new GameRenderer(game) { GameplayViewport = true, ShowMapCaption = false };
        Title = L.MapName(map.Id);
        SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None;
        NavigationPage.SetHasNavigationBar(this, false);
        NavigationPage.SetHasBackButton(this, false);

        // The arena owns the entire surface. Every control is a floating sibling above it.
        var root = new Grid { SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.None };
        arena = new GraphicsView { Drawable = renderer };
        arena.StartInteraction += (_, args) => UpdateTouch(args);
        arena.DragInteraction += (_, args) => UpdateTouch(args);
        arena.EndInteraction += (_, args) =>
        {
            if (args.Touches.Length > 0) UpdateTouch(args);
            HandleMapTap();
        };
        arena.CancelInteraction += (_, _) => ClearPreview();
        SemanticProperties.SetDescription(arena, L.T("ArenaDescription"));
        root.Add(arena);

        var stats = new HorizontalStackLayout
        {
            Spacing = 6, Margin = new Thickness(12, 12, 70, 0),
            HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start
        };
        foreach (var label in new[] { wave, health, cash })
        {
            label.VerticalOptions = LayoutOptions.Center;
            label.MaxLines = 1;
            var chip = Theme.Card(label, new Thickness(10, 7));
            chip.MinimumHeightRequest = 38;
            stats.Add(chip);
        }
        root.Add(stats);
        pause.WidthRequest = pause.HeightRequest = 44;
        pause.Padding = 0;
        pause.HorizontalOptions = LayoutOptions.End;
        pause.VerticalOptions = LayoutOptions.Start;
        pause.Margin = new Thickness(0, 10, 12, 0);
        pause.Clicked += (_, _) => TogglePause();
        SemanticProperties.SetDescription(pause, L.T("PauseMission"));
        root.Add(pause);

        var shop = new HorizontalStackLayout { Spacing = 5 };
        foreach (var definition in TurretCatalog.All)
        {
            var icon = new TurretIconButton(definition);
            icon.Clicked += (_, _) => SelectBuild(definition);
            buildButtons[definition.Type] = icon;
            shop.Add(icon);
        }
        var shopPanel = Theme.Card(shop, new Thickness(6, 4));
        shopPanel.HorizontalOptions = LayoutOptions.Start;
        shopPanel.VerticalOptions = LayoutOptions.End;
        shopPanel.Margin = new Thickness(12, 0, 0, 8);
        root.Add(shopPanel);

        speedButton.WidthRequest = 46;
        speedButton.HeightRequest = 46;
        speedButton.Padding = 0;
        var changingSpeed = false;
        speedButton.Clicked += async (_, _) =>
        {
            if (changingSpeed) return;
            changingSpeed = true;
            try
            {
                await settings.SaveAsync(settings.Current with { GameSpeed = speed == 1 ? 2 : 1 });
                speed = settings.Current.GameSpeed;
                speedButton.Text = $"{speed}×";
            }
            catch (Exception) { ShowError(L.T("SettingsSaveFailed")); }
            finally { changingSpeed = false; }
        };
        nextWave.WidthRequest = 132;
        nextWave.HeightRequest = 46;
        nextWave.Padding = 6;
        nextWave.FontSize = 11;
        nextWave.Clicked += (_, _) =>
        {
            if (!game.StartNextWave()) return;
            ClearPreview();
            RefreshHud();
        };
        var waveControls = new HorizontalStackLayout
        {
            Spacing = 6, Children = { speedButton, nextWave },
            HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 12, 10)
        };
        root.Add(waveControls);

        var cancel = Theme.Action("×");
        cancel.WidthRequest = 40;
        cancel.Padding = 0;
        cancel.Clicked += (_, _) => { renderer.SelectedTurret = null; ClearPreview(); RefreshHud(); };
        SemanticProperties.SetDescription(cancel, L.T("DeselectTurret"));
        upgrade.FontSize = sell.FontSize = 11;
        upgrade.Padding = sell.Padding = new Thickness(9, 6);
        upgrade.Clicked += (_, _) =>
        {
            if (renderer.SelectedTurret is not Turret turret) return;
            var result = game.UpgradeTurret(turret);
            if (!result.Success) ShowBuildError(result);
            RefreshHud();
            arena.Invalidate();
        };
        sell.Clicked += (_, _) =>
        {
            if (renderer.SelectedTurret is not Turret turret) return;
            var result = game.SellTurret(turret);
            if (result.Success) renderer.SelectedTurret = null;
            else ShowBuildError(result);
            RefreshHud();
            arena.Invalidate();
        };
        level.VerticalOptions = LayoutOptions.Center;
        selectionPanel = Theme.Card(new HorizontalStackLayout
        { Spacing = 8, Children = { level, upgrade, sell, cancel } }, 7);
        selectionPanel.HorizontalOptions = LayoutOptions.Center;
        selectionPanel.VerticalOptions = LayoutOptions.End;
        selectionPanel.Margin = new Thickness(12, 0, 12, 94);
        selectionPanel.IsVisible = false;
        root.Add(selectionPanel);

        toast.MaxLines = 2;
        toastPanel = Theme.Card(toast, new Thickness(12, 8));
        toastPanel.MaximumWidthRequest = 400;
        toastPanel.HorizontalOptions = LayoutOptions.Center;
        toastPanel.VerticalOptions = LayoutOptions.Start;
        toastPanel.Margin = new Thickness(12, 64, 12, 0);
        toastPanel.IsVisible = false;
        root.Add(toastPanel);

        scrim = new BoxView { Color = Color.FromArgb("#88020A10"), IsVisible = false };
        root.Add(scrim);
        var resume = Theme.Action(L.T("Resume"), true);
        var retreat = Theme.Action(L.T("Retreat"));
        resume.Clicked += (_, _) => TogglePause();
        retreat.Clicked += (_, _) => game.Retreat();
        pausePanel = Overlay(new VerticalStackLayout
        { Spacing = 12, Children = { Theme.Text(L.T("Paused"), 24, Theme.Mint), resume, retreat } });
        root.Add(pausePanel);
        returnButton.IsEnabled = false;
        retryButton.IsVisible = false;
        returnButton.Clicked += async (_, _) =>
        {
            if (!saved) return;
            returnButton.IsEnabled = false;
            await Navigation.PopAsync();
        };
        retryButton.Clicked += async (_, _) => await SaveResultAsync();
        resultPanel = Overlay(new VerticalStackLayout { Spacing = 12, Children =
        { resultTitle, resultDetails, saveStatus, retryButton, returnButton } });
        root.Add(resultPanel);
        Content = root;
        game.RunFinished += OnRunFinished;
        Loaded += (_, _) =>
        {
            if (!IsVisible) return;
            GameScreenMode.SetActive(true);
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () =>
            {
                if (GameScreenMode.IsActive) GameScreenMode.Apply();
            });
        };
        SizeChanged += (_, _) =>
        {
            var compact = Width < 800 || Height < 450;
            foreach (var icon in buildButtons.Values)
            {
                icon.WidthRequest = compact ? 48 : 54;
                icon.HeightRequest = Math.Clamp(Height * .2 - 24, 44, 60);
            }
            foreach (var label in new[] { wave, health, cash }) label.FontSize = compact ? 11 : 12;
            nextWave.WidthRequest = compact ? 112 : 132;
            shop.Spacing = compact ? 4 : 5;
        };
        RefreshHud();
    }

    private static Border Overlay(View content)
    {
        var border = Theme.Card(new ScrollView { Content = content });
        border.MaximumWidthRequest = 430;
        border.MaximumHeightRequest = 340;
        border.HorizontalOptions = LayoutOptions.Center;
        border.VerticalOptions = LayoutOptions.Center;
        border.Margin = 12;
        border.IsVisible = false;
        return border;
    }

    private void SelectBuild(TurretDefinition definition)
    {
        if (game.State is GameState.Paused or GameState.Finished) return;
        renderer.SelectedTurret = null;
        renderer.BuildSelection = renderer.BuildSelection?.Type == definition.Type ? null : definition;
        ClearPreview();
        RefreshHud();
    }

    private void UpdateTouch(TouchEventArgs args)
    {
        if (game.State is GameState.Paused or GameState.Finished || args.Touches.Length == 0) return;
        var point = args.Touches[0];
        touchPosition = renderer.GetViewport((float)arena.Width, (float)arena.Height).ToWorld(new Vector2(point.X, point.Y));
        renderer.PreviewPosition = touchPosition;
        renderer.PreviewValid = touchPosition is Vector2 position && game.CanPlace(position) &&
            renderer.BuildSelection is TurretDefinition definition && game.Cash >= definition.Cost;
        arena.Invalidate();
    }

    private void HandleMapTap()
    {
        if (game.State is GameState.Paused or GameState.Finished) return;
        if (touchPosition is not Vector2 point) { ClearPreview(); return; }
        var existing = game.Turrets.FirstOrDefault(turret => Vector2.Distance(turret.Position, point) < turret.Radius + 12);
        if (existing is not null)
        {
            renderer.SelectedTurret = existing;
            renderer.BuildSelection = null;
        }
        else if (renderer.BuildSelection is TurretDefinition definition)
        {
            var result = game.PlaceTurret(definition.Type, point);
            if (result.Success) { renderer.SelectedTurret = result.Turret; renderer.BuildSelection = null; }
            else ShowBuildError(result);
        }
        else renderer.SelectedTurret = null;
        ClearPreview();
        RefreshHud();
    }

    private void ClearPreview()
    {
        touchPosition = null;
        renderer.PreviewPosition = null;
        arena.Invalidate();
    }

    private void ShowError(string text)
    {
        toast.Text = text;
        toastUntil = Stopwatch.GetTimestamp() + (long)(Stopwatch.Frequency * 2.5);
        toastPanel.IsVisible = true;
    }

    private void ShowBuildError(BuildResult result) =>
        ShowError(result.MessageKey is null ? result.Message : L.F(result.MessageKey, result.Amount));

    protected override void OnAppearing()
    {
        base.OnAppearing();
        game.SoundRequested -= audio.Play;
        game.SoundRequested += audio.Play;
        if (game.State != GameState.Paused) audio.Resume();
        GameScreenMode.SetActive(true);
        timer ??= Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1.0 / 60);
        timer.Tick -= Tick;
        timer.Tick += Tick;
        lastFrame = Stopwatch.GetTimestamp();
        timer.Start();
    }

    protected override void OnDisappearing()
    {
        timer?.Stop();
        if (timer is not null) timer.Tick -= Tick;
        PauseForBackground();
        game.SoundRequested -= audio.Play;
        audio.Suspend();
        base.OnDisappearing();
    }

    private void Tick(object? sender, EventArgs args)
    {
        var now = Stopwatch.GetTimestamp();
        var deltaTime = (float)Stopwatch.GetElapsedTime(lastFrame, now).TotalSeconds;
        lastFrame = now;
        if (toastPanel.IsVisible && now >= toastUntil) toastPanel.IsVisible = false;
        if (game.State != GameState.Running) return;
        game.Update(deltaTime * speed);
        arena.Invalidate();
        hudTime += deltaTime;
        if (hudTime >= .12 || game.State != GameState.Running) { hudTime = 0; RefreshHud(); }
    }

    private void TogglePause()
    {
        if (game.State == GameState.Finished) return;
        game.TogglePause();
        if (game.State == GameState.Paused) audio.Suspend();
        else audio.Resume();
        ClearPreview();
        toastPanel.IsVisible = false;
        pausePanel.IsVisible = scrim.IsVisible = game.State == GameState.Paused;
        pause.Text = game.State == GameState.Paused ? "▶" : "Ⅱ";
        lastFrame = Stopwatch.GetTimestamp();
        RefreshHud();
    }

    public void PauseForBackground()
    {
        if (game.State is GameState.Building or GameState.Running) TogglePause();
    }

    protected override bool OnBackButtonPressed()
    {
        if (game.State == GameState.Finished) { if (saved) _ = Navigation.PopAsync(); }
        else TogglePause();
        return true;
    }

    private void RefreshHud()
    {
        wave.Text = L.F("WaveHud", game.Waves.Wave);
        health.Text = L.F("HealthHud", game.Lives, game.MaxLives);
        health.TextColor = game.Lives <= 6 ? Color.FromArgb("#FF6874") : Theme.Mint;
        cash.Text = $"${game.Cash}";
        nextWave.Text = game.State == GameState.Running ? L.F("Inbound", game.Aliens.Count + game.Waves.PendingCount) : L.F("NextWave", game.Waves.Wave + 1);
        nextWave.IsEnabled = game.State == GameState.Building;
        speedButton.IsEnabled = game.State is GameState.Building or GameState.Running;
        var active = game.State is GameState.Building or GameState.Running;
        foreach (var (type, icon) in buildButtons)
        {
            var definition = TurretCatalog.Get(type);
            icon.Refresh(active, renderer.BuildSelection?.Type == type || renderer.SelectedTurret?.Definition.Type == type,
                game.Cash >= definition.Cost);
        }
        selectionPanel.IsVisible = active && renderer.SelectedTurret is not null;
        if (renderer.SelectedTurret is Turret turret)
        {
            level.Text = L.F("Level", turret.Level);
            upgrade.Text = turret.Level >= 3 ? L.T("Max") : $"↑ ${turret.UpgradeCost}";
            upgrade.IsEnabled = active && turret.Level < 3 && game.Cash >= turret.UpgradeCost;
            sell.Text = L.F("SellFormat", turret.SellValue);
            sell.IsEnabled = active;
        }
    }

    private void OnRunFinished(RunResult result)
    {
        timer?.Stop();
        pausePanel.IsVisible = false;
        scrim.IsVisible = resultPanel.IsVisible = true;
        toastPanel.IsVisible = false;
        pause.IsEnabled = false;
        resultTitle.Text = result.Outcome == "Retreated" ? L.T("Withdrawn") : L.T("OverrunTitle");
        resultTitle.TextColor = result.Outcome == "Overrun" ? Color.FromArgb("#FF6874") : Theme.Mint;
        resultDetails.Text = L.F("ResultDetails", L.MapName(game.Map.Id), game.CompletedWaves, result.Kills, result.Credits);
        RefreshHud();
        arena.Invalidate();
        _ = SaveResultAsync();
    }

    private async Task SaveResultAsync()
    {
        if (saving || saved || game.Result is null) return;
        saving = true;
        retryButton.IsVisible = false;
        saveStatus.Text = L.T("SavingRecords");
        try
        {
            await repository.SaveRunAsync(game.Result);
            saved = true;
            saveStatus.Text = L.T("RecordsSaved");
            returnButton.IsEnabled = true;
        }
        catch (Exception)
        {
            saveStatus.Text = L.T("RecordsSaveFailed");
            retryButton.IsVisible = true;
        }
        finally { saving = false; }
    }
}
