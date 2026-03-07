using AetherPlayer.Models;

namespace AetherPlayer.Services;

public interface IAudioPlayerService
{
    event EventHandler? PlaybackStateChanged;
    event EventHandler? PositionChanged;
    event EventHandler<Track?>? TrackChanged;

    bool IsPlaying { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }
    double Volume { get; set; }
    Track? CurrentTrack { get; }

    void LoadTrack(Track track);
    void Play();
    void Pause();
    void Stop();
    void Seek(TimeSpan position);
}
