using System.IO;
using System.Windows;
using System.Windows.Media;

namespace JuHuaLiPet;

internal sealed class PetSoundPlayer
{
    private static readonly IReadOnlyDictionary<PetSoundCue, string> ResourceFiles =
        new Dictionary<PetSoundCue, string>
        {
            [PetSoundCue.Greeting] = "juhuali-greeting.wav",
            [PetSoundCue.Tap] = "juhuali-tap.wav",
            [PetSoundCue.Curious] = "juhuali-curious.wav",
            [PetSoundCue.Stroll] = "juhuali-stroll.wav",
            [PetSoundCue.Happy] = "juhuali-happy.wav",
            [PetSoundCue.Nudge] = "juhuali-nudge.wav",
            [PetSoundCue.Confirm] = "juhuali-confirm.wav",
            [PetSoundCue.Reminder] = "juhuali-reminder-call.wav"
        };

    private readonly MediaPlayer _player = new();
    private readonly Dictionary<PetSoundCue, string> _soundPaths = [];
    private readonly string? _fallbackSoundPath;
    private double _volume = 0.9;

    public PetSoundPlayer()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JuHuaLiPet");
        Directory.CreateDirectory(directory);

        foreach (var (cue, fileName) in ResourceFiles)
        {
            var path = ResolveSoundPath(directory, fileName);
            if (path != null)
            {
                _soundPaths[cue] = path;
            }
        }

        _fallbackSoundPath = ResolveLegacySoundPath(directory)
            ?? (_soundPaths.TryGetValue(PetSoundCue.Greeting, out var greetingPath) ? greetingPath : null);
    }

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0, 1.0);
            _player.Volume = _volume;
        }
    }

    private static string? ResolveSoundPath(string appDataDirectory, string fileName)
    {
        var exeDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrWhiteSpace(exeDirectory))
        {
            var externalSound = Path.Combine(exeDirectory, fileName);
            if (File.Exists(externalSound))
            {
                return externalSound;
            }

            var externalSoundFolder = Path.Combine(exeDirectory, "Sounds", fileName);
            if (File.Exists(externalSoundFolder))
            {
                return externalSoundFolder;
            }
        }

        return CopyResourceToAppData(appDataDirectory, fileName);
    }

    private static string? ResolveLegacySoundPath(string appDataDirectory)
    {
        var exeDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrWhiteSpace(exeDirectory))
        {
            var externalMp4 = Path.Combine(exeDirectory, "juhuali-voice.mp4");
            if (File.Exists(externalMp4))
            {
                return externalMp4;
            }

            var externalWav = Path.Combine(exeDirectory, "juhuali-reminder.wav");
            if (File.Exists(externalWav))
            {
                return externalWav;
            }
        }

        return CopyResourceToAppData(appDataDirectory, "juhuali-voice.mp4")
            ?? CopyResourceToAppData(appDataDirectory, "juhuali-reminder.wav");
    }

    private static string? CopyResourceToAppData(string directory, string fileName)
    {
        var targetPath = Path.Combine(directory, fileName);

        using var source = OpenResourceStream($"Assets/Sounds/{fileName}")
            ?? OpenResourceStream($"Assets/{fileName}");
        if (source == null)
        {
            return File.Exists(targetPath) ? targetPath : null;
        }

        using var target = File.Create(targetPath);
        source.CopyTo(target);
        return targetPath;
    }

    private static Stream? OpenResourceStream(string resourcePath)
    {
        try
        {
            return Application.GetResourceStream(new Uri($"pack://application:,,,/{resourcePath}"))?.Stream;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Play(PetSoundCue cue = PetSoundCue.Greeting)
    {
        var soundPath = _soundPaths.TryGetValue(cue, out var cuePath) ? cuePath : _fallbackSoundPath;
        if (string.IsNullOrWhiteSpace(soundPath) || !File.Exists(soundPath))
        {
            return;
        }

        _player.Stop();
        _player.Open(new Uri(soundPath, UriKind.Absolute));
        _player.Volume = _volume;
        _player.Play();
    }
}
