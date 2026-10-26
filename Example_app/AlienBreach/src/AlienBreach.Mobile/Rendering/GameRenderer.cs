using AlienBreach.Services;
using System.Numerics;
using AlienBreach.Core.Entities;
using AlienBreach.Core.Game;
using AlienBreach.Core.Maps;
using AlienBreach.Core.Turrets;

namespace AlienBreach.Rendering;

public sealed class GameRenderer(GameEngine game) : IDrawable
{
    public Turret? SelectedTurret { get; set; }
    public TurretDefinition? BuildSelection { get; set; }
    public Vector2? PreviewPosition { get; set; }
    public bool PreviewValid { get; set; }
    public bool GameplayViewport { get; set; }
    public bool ShowMapCaption { get; set; } = true;
    public MapViewport GetViewport(float width, float height) =>
        GameplayViewport ? MapViewport.Gameplay(width, height) : MapViewport.Fit(width, height);

    public static Color ShotColor(ShotKind kind) => Color.FromArgb(kind switch
    {
        ShotKind.Repeater => "#FFCA76", ShotKind.Plasma => "#CF95F0",
        ShotKind.Cryo => "#79DFFD", ShotKind.Rail => "#F1DFAB", _ => "#65EFBA"
    });
    public static Color MapColor(MapTheme theme) => Color.FromArgb(theme switch
    {
        MapTheme.Reactor => "#FFCA76", MapTheme.Cryo => "#79DFFD", _ => "#65EFBA"
    });

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromArgb("#07131A");
        canvas.FillRectangle(dirtyRect);
        var view = GetViewport(dirtyRect.Width, dirtyRect.Height);
        if (view.Scale <= 0) return;
        canvas.SaveState();
        canvas.Translate(view.X, view.Y);
        canvas.Scale(view.ScaleX, view.ScaleY);
        DrawMap(canvas);
        if (SelectedTurret is not null) DrawRange(canvas, SelectedTurret.Position, SelectedTurret.Range, ShotColor(SelectedTurret.Definition.ShotKind));
        if (BuildSelection is not null && PreviewPosition is Vector2 point)
        {
            var color = PreviewValid ? ShotColor(BuildSelection.ShotKind) : Color.FromArgb("#FF6874");
            DrawRange(canvas, point, BuildSelection.Range, color);
            canvas.StrokeColor = color;
            canvas.StrokeSize = 3;
            canvas.DrawCircle(point.X, point.Y, 25);
            canvas.DrawLine(point.X - 8, point.Y, point.X + 8, point.Y);
            canvas.DrawLine(point.X, point.Y - 8, point.X, point.Y + 8);
        }
        foreach (var turret in game.Turrets) DrawTurret(canvas, turret);
        foreach (var alien in game.Aliens) DrawAlien(canvas, alien);
        foreach (var bullet in game.Projectiles)
        {
            canvas.FillColor = ShotColor(bullet.Kind);
            canvas.FillCircle(bullet.Position.X, bullet.Position.Y, bullet.Kind == ShotKind.Plasma ? 7 : 4);
        }
        foreach (var impact in game.Impacts)
        {
            canvas.Alpha = Math.Clamp(impact.Lifetime / .3f, 0, 1);
            canvas.StrokeColor = ShotColor(impact.Kind);
            canvas.StrokeSize = impact.Kind == ShotKind.Rail ? 5 : 3;
            if (impact.End is Vector2 end) canvas.DrawLine(impact.Position.X, impact.Position.Y, end.X, end.Y);
            else canvas.DrawCircle(impact.Position.X, impact.Position.Y, (1 - impact.Lifetime / .3f) * impact.Radius + 4);
        }
        canvas.Alpha = 1;
        canvas.RestoreState();
    }

    private void DrawMap(ICanvas canvas)
    {
        var accent = MapColor(game.Map.Theme);
        canvas.FillColor = Color.FromArgb(game.Map.Theme == MapTheme.Cryo ? "#10212D" : "#0C1C24");
        canvas.FillRectangle(0, 0, BattleMap.Width, BattleMap.Height);
        canvas.StrokeColor = Color.FromArgb("#19303B");
        canvas.StrokeSize = 1;
        for (var x = 0; x < BattleMap.Width; x += 55) canvas.DrawLine(x, 0, x, BattleMap.Height);
        for (var y = 0; y < BattleMap.Height; y += 55) canvas.DrawLine(0, y, BattleMap.Width, y);

        if (game.Map.Theme == MapTheme.Reactor)
        {
            canvas.StrokeColor = Color.FromArgb("#554A32");
            canvas.StrokeSize = 5;
            canvas.DrawCircle(550, 310, 210);
            canvas.DrawCircle(550, 310, 195);
            canvas.DrawLine(350, 25, 350, 595);
            canvas.DrawLine(740, 25, 740, 595);
        }
        else if (game.Map.Theme == MapTheme.Cryo)
        {
            canvas.StrokeColor = Color.FromArgb("#284554");
            canvas.StrokeSize = 2;
            for (var i = 0; i < 10; i++)
            {
                var x = 80 + i * 107 % 1000;
                var y = 50 + i * 173 % 520;
                canvas.DrawLine(x, y, x + 30, y + 22);
                canvas.DrawLine(x + 30, y + 22, x + 17, y + 48);
                canvas.DrawLine(x + 30, y + 22, x + 61, y + 7);
            }
        }
        else
        {
            canvas.FillColor = Color.FromArgb("#17313B");
            foreach (var p in new[] { new Vector2(80, 330), new Vector2(440, 80), new Vector2(980, 320) })
            {
                canvas.FillRoundedRectangle(p.X, p.Y, 60, 45, 4);
                canvas.StrokeColor = Color.FromArgb("#36525F");
                canvas.DrawRoundedRectangle(p.X + 5, p.Y + 5, 50, 35, 3);
            }
        }

        var points = game.Map.Path.Points;
        var path = new PathF();
        path.MoveTo(points[0].X, points[0].Y);
        foreach (var point in points.Skip(1)) path.LineTo(point.X, point.Y);
        canvas.StrokeColor = Color.FromArgb("#35515C");
        canvas.StrokeSize = EnemyPath.RoadWidth + 6;
        canvas.DrawPath(path);
        canvas.StrokeColor = Color.FromArgb(game.Map.Theme == MapTheme.Cryo ? "#1D3846" : "#1A3038");
        canvas.StrokeSize = EnemyPath.RoadWidth;
        canvas.DrawPath(path);
        foreach (var point in points.Skip(1).SkipLast(1))
        {
            canvas.FillColor = Color.FromArgb("#35515C");
            canvas.FillCircle(point.X, point.Y, EnemyPath.RoadWidth / 2 + 3);
            canvas.FillColor = Color.FromArgb(game.Map.Theme == MapTheme.Cryo ? "#1D3846" : "#1A3038");
            canvas.FillCircle(point.X, point.Y, EnemyPath.RoadWidth / 2);
        }
        canvas.StrokeSize = 2;
        canvas.StrokeColor = accent;
        canvas.Alpha = .55f;
        for (var distance = 75f; distance < game.Map.Path.Length - 70; distance += 110)
        {
            var p = game.Map.Path.PositionAt(distance);
            var direction = game.Map.Path.DirectionAt(distance);
            var normal = new Vector2(-direction.Y, direction.X);
            var tip = p + direction * 6;
            var left = p - direction * 5 + normal * 5;
            var right = p - direction * 5 - normal * 5;
            canvas.DrawLine(left.X, left.Y, tip.X, tip.Y);
            canvas.DrawLine(right.X, right.Y, tip.X, tip.Y);
        }
        canvas.Alpha = 1;
        var start = points[0];
        canvas.StrokeColor = Color.FromArgb("#FF6874");
        canvas.StrokeSize = 4;
        canvas.DrawCircle(start.X, start.Y, 26);
        var exit = points[^1];
        canvas.FillColor = Color.FromArgb("#254D49");
        canvas.FillCircle(exit.X, exit.Y, 33);
        canvas.StrokeColor = accent;
        canvas.DrawCircle(exit.X, exit.Y, 32);
        canvas.DrawCircle(exit.X, exit.Y, 22);
        canvas.FillColor = accent;
        canvas.FillRectangle(exit.X - 4, exit.Y - 13, 8, 26);
        canvas.FillRectangle(exit.X - 13, exit.Y - 4, 26, 8);
        canvas.StrokeColor = Color.FromArgb("#2D5960");
        canvas.StrokeSize = 3;
        canvas.DrawRectangle(2, 2, BattleMap.Width - 4, BattleMap.Height - 4);
        canvas.FontColor = Color.FromArgb("#64818F");
        canvas.FontSize = 14;
        if (ShowMapCaption)
            canvas.DrawString(L.F("DefenseGrid", L.MapName(game.Map.Id).ToUpper(L.Culture)), 16, BattleMap.Height - 30, 440, 24,
                HorizontalAlignment.Left, VerticalAlignment.Center);
    }

    private static void DrawRange(ICanvas canvas, Vector2 p, float range, Color color)
    {
        canvas.FillColor = color;
        canvas.Alpha = .08f;
        canvas.FillCircle(p.X, p.Y, range);
        canvas.Alpha = .55f;
        canvas.StrokeColor = color;
        canvas.StrokeSize = 2;
        canvas.DrawCircle(p.X, p.Y, range);
        canvas.Alpha = 1;
    }

    private void DrawTurret(ICanvas canvas, Turret turret)
    {
        var color = ShotColor(turret.Definition.ShotKind);
        canvas.SaveState();
        canvas.Translate(turret.Position.X, turret.Position.Y);
        canvas.FillColor = Color.FromArgb("#050E14");
        canvas.FillCircle(4, 6, 27);
        canvas.FillColor = Color.FromArgb("#233E49");
        canvas.FillRoundedRectangle(-23, -23, 46, 46, 9);
        canvas.StrokeColor = color;
        canvas.StrokeSize = ReferenceEquals(turret, SelectedTurret) ? 3 : 1;
        canvas.DrawRoundedRectangle(-23, -23, 46, 46, 9);
        canvas.Rotate((float)(Math.Atan2(turret.Facing.Y, turret.Facing.X) * 180 / Math.PI));
        canvas.FillColor = Color.FromArgb("#54717E");
        canvas.FillCircle(0, 0, 15);
        canvas.FillColor = color;
        switch (turret.Definition.Type)
        {
            case TurretType.Repeater:
                canvas.FillRoundedRectangle(3, -10, 28, 6, 2);
                canvas.FillRoundedRectangle(3, 4, 28, 6, 2);
                break;
            case TurretType.Cannon:
                canvas.FillRoundedRectangle(0, -10, 32, 20, 4);
                canvas.FillColor = Color.FromArgb("#273A4E");
                canvas.FillCircle(24, 0, 7);
                break;
            case TurretType.Cryo:
                canvas.StrokeColor = color;
                canvas.StrokeSize = 3;
                canvas.DrawCircle(0, 0, 16);
                for (var i = 0; i < 6; i++)
                {
                    canvas.Rotate(60);
                    canvas.DrawLine(0, 0, 20, 0);
                }
                break;
            case TurretType.Railgun:
                canvas.FillRoundedRectangle(-5, -8, 44, 5, 2);
                canvas.FillRoundedRectangle(-5, 3, 44, 5, 2);
                canvas.FillRectangle(0, -12, 12, 24);
                break;
            default:
                canvas.FillRoundedRectangle(0, -5, 32, 10, 2);
                break;
        }
        canvas.RestoreState();
        canvas.FillColor = color;
        for (var i = 0; i < turret.Level; i++)
            canvas.FillCircle(turret.Position.X - (turret.Level - 1) * 5 + i * 10, turret.Position.Y + 32, 3);
    }

    private static void DrawAlien(ICanvas canvas, Alien alien)
    {
        var color = alien is RedTitan ? Color.FromArgb("#FF5868") : alien is Brute ? Color.FromArgb("#FFCA76")
            : alien is Spitter ? Color.FromArgb("#D992EE") : Color.FromArgb("#65EFBA");
        var radius = alien.Radius;
        canvas.SaveState();
        canvas.Translate(alien.Position.X, alien.Position.Y);
        canvas.Rotate((float)(Math.Atan2(alien.Facing.Y, alien.Facing.X) * 180 / Math.PI));
        canvas.FillColor = Color.FromArgb("#050E14");
        canvas.FillCircle(3, 5, radius + 2);
        canvas.StrokeColor = color;
        canvas.StrokeSize = alien is RedTitan ? 6 : alien is Brute ? 5 : 3;
        for (var i = -1; i <= 1; i++)
        {
            canvas.DrawLine(i * radius * .65f, -radius * .5f, i * radius * .65f - 8, -radius - 6);
            canvas.DrawLine(i * radius * .65f, radius * .5f, i * radius * .65f - 8, radius + 6);
        }
        canvas.FillColor = alien is RedTitan ? Color.FromArgb("#872637") : alien is Brute ? Color.FromArgb("#73542F")
            : alien is Spitter ? Color.FromArgb("#5E386E") : Color.FromArgb("#246651");
        canvas.FillEllipse(-radius, -radius * .75f, radius * 2, radius * 1.5f);
        canvas.StrokeSize = alien is RedTitan ? 4 : 2;
        canvas.DrawEllipse(-radius, -radius * .75f, radius * 2, radius * 1.5f);
        canvas.FillColor = color;
        canvas.FillCircle(radius * .45f, -radius * .3f, alien is RedTitan ? 5 : 3);
        canvas.FillCircle(radius * .45f, radius * .3f, alien is RedTitan ? 5 : 3);
        if (alien is RedTitan)
        {
            canvas.StrokeSize = 4;
            canvas.DrawLine(-12, -22, -12, 22);
            canvas.DrawLine(-24, -18, -24, 18);
            canvas.DrawLine(radius * .5f, -radius * .5f, radius + 10, -radius * .9f);
            canvas.DrawLine(radius * .5f, radius * .5f, radius + 10, radius * .9f);
        }
        canvas.RestoreState();
        if (alien.Health < alien.MaxHealth || alien is RedTitan)
        {
            canvas.FillColor = Color.FromArgb("#283F46");
            canvas.FillRectangle(alien.Position.X - radius, alien.Position.Y - radius - 14, radius * 2, 5);
            canvas.FillColor = color;
            canvas.FillRectangle(alien.Position.X - radius, alien.Position.Y - radius - 14, radius * 2 * alien.Health / alien.MaxHealth, 5);
        }
        if (alien.IsSlowed)
        {
            canvas.StrokeColor = ShotColor(ShotKind.Cryo);
            canvas.StrokeSize = 2;
            canvas.DrawCircle(alien.Position.X, alien.Position.Y, radius + 4);
        }
    }
}
