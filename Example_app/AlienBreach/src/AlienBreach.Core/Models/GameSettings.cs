namespace AlienBreach.Core.Models;

public sealed record GameSettings(string Language = "et", int GameSpeed = 1, bool SoundEnabled = true)
{
    public void Validate()
    {
        if (Language is not ("et" or "en"))
            throw new ArgumentOutOfRangeException(nameof(Language));
        if (GameSpeed is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(GameSpeed));
    }
}
