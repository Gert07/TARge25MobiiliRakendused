namespace Example_app.Models;

public sealed record GameStats
{
    public int XWins { get; init; }
    public int OWins { get; init; }
    public int Draws { get; init; }
}
