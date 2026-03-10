using AetherPlayer.Models;

namespace AetherPlayer.ViewModels;

public class TrackViewModel(Track model) : ViewModelBase
{
    public Track Model { get; } = model;

    public string Title => Model.Title;
    public string Artist => Model.Artist;
    public string Album => Model.Album;
    public string Genre => Model.Genre;
    public string MoodTagsDisplay => Model.MoodTagsDisplay;
    public string CoverPath => Model.CoverPath;
    public string DurationDisplay => Model.DurationDisplay;
    public double DurationSeconds => Model.Duration.TotalSeconds;
    public string Lyrics => Model.Lyrics;
    public DateTime AddedOn => Model.AddedOn;

    public bool IsFavorite
    {
        get => Model.IsFavorite;
        set
        {
            if (Model.IsFavorite == value)
            {
                return;
            }

            Model.IsFavorite = value;
            OnPropertyChanged();
        }
    }

    public int Rating
    {
        get => Model.Rating;
        set
        {
            var clamped = Math.Clamp(value, 0, 5);
            if (Model.Rating == clamped)
            {
                return;
            }

            Model.Rating = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RatingStars));
        }
    }

    public bool IsMissing
    {
        get => Model.IsMissing;
        set
        {
            if (Model.IsMissing == value)
            {
                return;
            }

            Model.IsMissing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AvailabilityText));
        }
    }

    public string AvailabilityText => IsMissing ? "Missing file" : "Available";
    public string RatingStars => new string('★', Rating).PadRight(5, '☆');
}
