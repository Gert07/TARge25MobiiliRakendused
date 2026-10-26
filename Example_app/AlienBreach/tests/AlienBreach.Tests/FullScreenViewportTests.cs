using System.Numerics;
using AlienBreach.Core.Maps;

namespace AlienBreach.Tests;

public sealed class FullScreenViewportTests
{
    [Theory]
    [InlineData(1080, 480)]
    [InlineData(760, 340)]
    [InlineData(640, 320)]
    [InlineData(1100, 620)]
    public void FullScreenViewportFillsAllEdgesWithoutCroppingTheRoute(float width, float height)
    {
        var view = MapViewport.Fill(width, height);
        Assert.Equal(Vector2.Zero, view.ToScreen(Vector2.Zero));
        Assert.Equal(new Vector2(width, height), view.ToScreen(new(BattleMap.Width, BattleMap.Height)));
        foreach (var map in MapCatalog.All)
            foreach (var point in map.Path.Points)
            {
                var screen = view.ToScreen(point);
                Assert.InRange(screen.X, 0, width);
                Assert.InRange(screen.Y, 0, height);
                Assert.True(Vector2.Distance(point, view.ToWorld(screen)!.Value) < .001f);
            }
    }

    [Theory]
    [InlineData(1080, 480)]
    [InlineData(640, 320)]
    public void TouchPlacementUsesTheSameTransformAsFullScreenDrawing(float width, float height)
    {
        var view = MapViewport.Fill(width, height);
        var world = new Vector2(700, 375);
        var screen = view.ToScreen(world);
        Assert.True(Vector2.Distance(world, view.ToWorld(screen)!.Value) < .001f);
        Assert.Equal(new Vector2(BattleMap.Width / 2, BattleMap.Height / 2), view.ToWorld(new(width / 2, height / 2)));
        Assert.Null(view.ToWorld(new(-1, 0)));
        Assert.Null(view.ToWorld(new(width + 1, height)));
    }

    [Fact]
    public void ZeroSizedViewportsCannotPlaceTurrets()
    {
        Assert.Null(MapViewport.Fill(0, 300).ToWorld(Vector2.Zero));
        Assert.Null(MapViewport.Fill(500, 0).ToWorld(Vector2.Zero));
        Assert.Null(MapViewport.Gameplay(0, 300).ToWorld(Vector2.Zero));
        Assert.Null(MapViewport.Gameplay(500, 0).ToWorld(Vector2.Zero));
    }

    [Theory]
    [InlineData(914, 411)]
    [InlineData(640, 320)]
    [InlineData(1100, 620)]
    public void ZoomedGameplayKeepsRoutesAboveTheFixedControls(float width, float height)
    {
        var view = MapViewport.Gameplay(width, height);
        var full = MapViewport.Fill(width, height);
        Assert.Equal(full.ScaleX * .8f, view.ScaleX, 5);
        Assert.Equal(full.ScaleY * .8f, view.ScaleY, 5);
        Assert.Equal(new Vector2(width * .1f, 0), view.ToScreen(Vector2.Zero));
        Assert.Equal(height * .8f, view.ToScreen(new(BattleMap.Width, BattleMap.Height)).Y, 3);
        foreach (var map in MapCatalog.All)
            foreach (var point in map.Path.Points)
            {
                var screen = view.ToScreen(point);
                Assert.InRange(screen.X, width * .1f - .001f, width * .9f + .001f);
                Assert.InRange(screen.Y, 0, height * .8f + .001f);
                Assert.True(Vector2.Distance(point, view.ToWorld(screen)!.Value) < .001f);
            }
        Assert.Null(view.ToWorld(new(width / 2, height * .9f)));
        Assert.Null(view.ToWorld(new(width * .05f, height / 2)));
        Assert.Null(view.ToWorld(new(width * .95f, height / 2)));
    }
}
