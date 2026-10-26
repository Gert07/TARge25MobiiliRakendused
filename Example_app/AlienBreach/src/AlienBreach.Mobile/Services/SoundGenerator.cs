using System.Text;
using AlienBreach.Core.Game;

namespace AlienBreach.Services;

// Original, short mono PCM effects. No downloads, codecs, or licensed samples are needed.
public static class SoundGenerator
{
    public const int SampleRate = 22050;

    public static byte[] Create(SoundCue cue)
    {
        var (duration, startHz, endHz, noise) = cue switch
        {
            SoundCue.Shot => (.075, 950.0, 180.0, .12),
            SoundCue.Blast => (.20, 120.0, 40.0, .65),
            SoundCue.Hit => (.055, 380.0, 100.0, .45),
            SoundCue.Build => (.15, 540.0, 1080.0, 0.0),
            SoundCue.WaveStart => (.30, 440.0, 880.0, 0.0),
            SoundCue.WaveCleared => (.36, 523.25, 1046.5, 0.0),
            SoundCue.BaseHit => (.24, 180.0, 65.0, .25),
            SoundCue.Retreat => (.28, 660.0, 330.0, 0.0),
            SoundCue.Defeat => (.50, 260.0, 55.0, .18),
            _ => throw new ArgumentOutOfRangeException(nameof(cue))
        };
        var count = (int)(duration * SampleRate);
        using var stream = new MemoryStream(44 + count * 2);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + count * 2);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16); // PCM format block length
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(count * 2);
        var random = new Random(730 + (int)cue);
        double phase = 0;
        for (var i = 0; i < count; i++)
        {
            var progress = i / (double)(count - 1);
            var frequency = startHz + (endHz - startHz) * progress;
            if (cue is SoundCue.WaveStart or SoundCue.WaveCleared)
                frequency = startHz * (progress < .33 ? 1 : progress < .66 ? 1.25 : 1.5);
            phase += 2 * Math.PI * frequency / SampleRate;
            var tone = Math.Sin(phase) * (1 - noise) + (random.NextDouble() * 2 - 1) * noise;
            // Smooth both ends to avoid clicks; keep rapid-fire effects quieter.
            var envelope = Math.Min(1, progress / .05) * Math.Pow(1 - progress, 1.5);
            var gain = cue is SoundCue.Shot or SoundCue.Hit ? .22 : .38;
            writer.Write((short)(tone * envelope * gain * short.MaxValue));
        }
        return stream.ToArray();
    }
}
