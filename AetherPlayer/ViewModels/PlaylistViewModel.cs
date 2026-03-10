using AetherPlayer.Models;

namespace AetherPlayer.ViewModels;

public class PlaylistViewModel(Playlist model) : ViewModelBase
{
    public Playlist Model { get; } = model;
    public string Name => Model.Name;
    public string Description => Model.Description;
    public string CoverPath => Model.CoverPath;
    public int TrackCount => Model.TrackCount;
    public string DurationDisplay => Model.DurationDisplay;
    public IReadOnlyList<Track> Tracks => Model.Tracks;

    public void NotifyTrackCollectionChanged()
    {
        OnPropertyChanged(nameof(TrackCount));
        OnPropertyChanged(nameof(DurationDisplay));
        OnPropertyChanged(nameof(Tracks));
    }
}
