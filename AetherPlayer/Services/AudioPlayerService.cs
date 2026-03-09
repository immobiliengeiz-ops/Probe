using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using AetherPlayer.Models;

namespace AetherPlayer.Services;

public class AudioPlayerService : IAudioPlayerService
{
    private readonly MediaPlayer _player = new();
    private readonly DispatcherTimer _positionTimer;
    private bool _isPlaying;
    private Track? _currentTrack;
    private TimeSpan _duration;

    public AudioPlayerService()
    {
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _positionTimer.Tick += (_, _) => PositionChanged?.Invoke(this, EventArgs.Empty);

        _player.MediaOpened += (_, _) =>
        {
            _duration = _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan : _currentTrack?.Duration ?? TimeSpan.Zero;
            PositionChanged?.Invoke(this, EventArgs.Empty);
        };

        _player.MediaEnded += (_, _) =>
        {
            _isPlaying = false;
            _positionTimer.Stop();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        };

        _player.MediaFailed += (_, _) =>
        {
            _isPlaying = false;
            _positionTimer.Stop();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler? PlaybackStateChanged;
    public event EventHandler? PositionChanged;
    public event EventHandler? PlaybackEnded;
    public event EventHandler<Track?>? TrackChanged;

    public bool IsPlaying => _isPlaying;
    public TimeSpan Position => _player.Position;
    public TimeSpan Duration => _duration;
    public Track? CurrentTrack => _currentTrack;

    public double Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0.0, 1.0);
    }

    public void LoadTrack(Track track)
    {
        _currentTrack = track;
        _duration = track.Duration;

        var fullPath = ResolveAudioPath(track.AudioPath);
        if (fullPath is not null && File.Exists(fullPath))
        {
            _player.Open(new Uri(fullPath));
        }

        TrackChanged?.Invoke(this, _currentTrack);
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        if (_currentTrack is null)
        {
            return;
        }

        _player.Play();
        _isPlaying = true;
        _positionTimer.Start();
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        _player.Pause();
        _isPlaying = false;
        _positionTimer.Stop();
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        _player.Stop();
        _isPlaying = false;
        _positionTimer.Stop();
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Seek(TimeSpan position)
    {
        _player.Position = position;
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string? ResolveAudioPath(string audioPath)
    {
        if (string.IsNullOrWhiteSpace(audioPath))
        {
            return null;
        }

        if (Path.IsPathRooted(audioPath))
        {
            return audioPath;
        }

        return Path.Combine(AppContext.BaseDirectory, audioPath);
    }
}
