using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using AetherPlayer.Commands;
using AetherPlayer.MockData;
using AetherPlayer.Models;
using AetherPlayer.Services;
using Microsoft.Win32;

namespace AetherPlayer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private const int RecentHistoryLimit = 50;

    private readonly IAudioPlayerService _audioService;
    private readonly IAppStateService _appStateService;
    private readonly IMusicImportService _musicImportService;
    private readonly ISelectionDialogService _selectionDialogService;
    private readonly List<TrackViewModel> _allTracks;
    private readonly List<string> _recentPlayedTrackIds = [];
    private readonly Random _random = new();

    private readonly RelayCommand _playPauseCommand;
    private readonly RelayCommand _nextCommand;

    private string _searchText = string.Empty;
    private string _selectedGenreFilter = "All";
    private string _selectedMoodFilter = "All";
    private bool _favoritesOnly;
    private bool _recentOnly;
    private NavSection _activeSection = NavSection.Home;
    private TrackViewModel? _selectedTrack;
    private double _volume = 0.75;
    private bool _isSeeking;
    private SortOption _selectedSortOption = SortOption.RecentlyAdded;
    private UserCollection? _selectedCollection;
    private string _newCollectionName = string.Empty;
    private TrackViewModel? _selectedQueueTrack;
    private bool _shuffleEnabled;
    private RepeatMode _repeatMode = RepeatMode.Off;
    private bool _isLyricsCollapsed;

    public MainViewModel()
    {
        _audioService = new AudioPlayerService();
        _appStateService = new AppStateService();
        _musicImportService = new MusicImportService();
        _selectionDialogService = new SelectionDialogService();

        _allTracks = DemoLibrary.Tracks.Select(t => new TrackViewModel(t)).ToList();
        Tracks = new ObservableCollection<TrackViewModel>(_allTracks);
        TracksView = CollectionViewSource.GetDefaultView(Tracks);
        TracksView.Filter = FilterTracks;

        Playlists = new ObservableCollection<PlaylistViewModel>(DemoLibrary.Playlists.Select(p => new PlaylistViewModel(p)));
        Albums = new ObservableCollection<Album>(DemoLibrary.Albums);
        Genres = new ObservableCollection<string>();
        Moods = new ObservableCollection<string>();
        SortOptions = new ObservableCollection<SortOption>(Enum.GetValues<SortOption>());
        LibraryFolders = new ObservableCollection<string>();
        UserCollections = new ObservableCollection<UserCollection>();
        SelectedTracks = new ObservableCollection<TrackViewModel>();
        QueueTracks = new ObservableCollection<TrackViewModel>();

        SelectTrackCommand = new RelayCommand(param => SelectTrack(param as TrackViewModel));
        PlayTrackCommand = new RelayCommand(param => PlayTrack(param as TrackViewModel));
        ToggleFavoriteCommand = new RelayCommand(param => ToggleFavorite(param as TrackViewModel));
        NavigateCommand = new RelayCommand(param => Navigate(param));
        PreviousCommand = new RelayCommand(_ => PlayPrevious(), _ => QueueTracks.Count > 0 || _allTracks.Any(t => !t.IsMissing));
        _nextCommand = new RelayCommand(_ => PlayNext(), _ => QueueTracks.Count > 0 || _allTracks.Any(t => !t.IsMissing));
        NextCommand = _nextCommand;
        _playPauseCommand = new RelayCommand(_ => TogglePlayPause(), _ => CurrentTrack is not null || SelectedTrack is not null);
        PlayPauseCommand = _playPauseCommand;

        AddToQueueCommand = new RelayCommand(param => AddToQueue(param as TrackViewModel));
        AddSelectionToQueueCommand = new RelayCommand(_ => AddSelectionToQueue(), _ => SelectedTracks.Count > 0);
        ClearQueueCommand = new RelayCommand(_ => ClearQueue(), _ => QueueTracks.Count > 0);
        RemoveQueueTrackCommand = new RelayCommand(param => RemoveQueueTrack(param as TrackViewModel), _ => SelectedQueueTrack is not null || QueueTracks.Count > 0);
        MoveQueueUpCommand = new RelayCommand(param => MoveQueue(param as TrackViewModel, -1), _ => QueueTracks.Count > 1);
        MoveQueueDownCommand = new RelayCommand(param => MoveQueue(param as TrackViewModel, 1), _ => QueueTracks.Count > 1);

        FocusSearchCommand = new RelayCommand(_ => RequestSearchFocus?.Invoke(this, EventArgs.Empty));
        PickLibraryFolderCommand = new RelayCommand(_ => PickLibraryFolder());
        RemoveLibraryFolderCommand = new RelayCommand(param => RemoveLibraryFolder(param as string));

        CreateCollectionCommand = new RelayCommand(_ => CreateCollection());
        AddSelectedToCollectionCommand = new RelayCommand(_ => AddSelectedToCollection(), _ => SelectedTracks.Count > 0 && SelectedCollection is not null);
        RemoveSelectedFromCollectionCommand = new RelayCommand(_ => RemoveSelectedFromCollection(), _ => SelectedTracks.Count > 0 && SelectedCollection is not null);

        SetRatingCommand = new RelayCommand(param => SetRating(param));
        BulkToggleFavoriteCommand = new RelayCommand(_ => BulkToggleFavorite(), _ => SelectedTracks.Count > 0);

        ToggleShuffleCommand = new RelayCommand(_ => ToggleShuffle());
        CycleRepeatModeCommand = new RelayCommand(_ => CycleRepeatMode());
        ToggleLyricsCommand = new RelayCommand(_ => IsLyricsCollapsed = !IsLyricsCollapsed);
        RescanLibraryCommand = new RelayCommand(_ => RescanLibrary());
        RefreshMetadataCommand = new RelayCommand(_ => RefreshMetadataForLibrary());
        ValidateLibraryCommand = new RelayCommand(_ => ValidateLibrary());
        RelinkTrackCommand = new RelayCommand(param => RelinkTrack(param as TrackViewModel));
        HandleDropItemsCommand = new RelayCommand(param => HandleDropItems(param as IEnumerable<string>));

        ContextPlayCommand = new RelayCommand(param => PlayTrack(param as TrackViewModel));
        ContextPlayNextCommand = new RelayCommand(param => AddToQueue(param as TrackViewModel));
        ContextAddToPlaylistCommand = new RelayCommand(param => AddToPlaylist(param as TrackViewModel));
        ContextAddToCollectionCommand = new RelayCommand(param => AddTrackToCollection(param as TrackViewModel));
        ContextToggleFavoriteCommand = new RelayCommand(param => ToggleFavorite(param as TrackViewModel));
        ContextRemoveFromLibraryCommand = new RelayCommand(param => RemoveFromLibrary(param as TrackViewModel));
        ContextOpenFileLocationCommand = new RelayCommand(param => OpenFileLocation(param as TrackViewModel));

        _audioService.PlaybackStateChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsPlaying));
            _playPauseCommand.RaiseCanExecuteChanged();
        };

        _audioService.PlaybackEnded += (_, _) =>
        {
            PlayNext();
        };

        _audioService.TrackChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentTrack));
            OnPropertyChanged(nameof(CurrentTrackTitle));
            OnPropertyChanged(nameof(CurrentTrackArtist));
            OnPropertyChanged(nameof(CurrentCoverPath));
            OnPropertyChanged(nameof(CurrentDurationDisplay));
            OnPropertyChanged(nameof(CurrentDurationSeconds));
            SaveAppState();
        };

        _audioService.PositionChanged += (_, _) =>
        {
            if (!_isSeeking)
            {
                OnPropertyChanged(nameof(CurrentPositionSeconds));
            }

            OnPropertyChanged(nameof(CurrentPositionDisplay));
            OnPropertyChanged(nameof(CurrentDurationDisplay));
            OnPropertyChanged(nameof(CurrentDurationSeconds));
        };

        RefreshFilterOptions();

        var state = _appStateService.Load();
        foreach (var folder in state.LibraryFolders.Where(Directory.Exists))
        {
            LibraryFolders.Add(folder);
            ImportFolder(folder);
        }

        // Merge persisted playlists (new or existing)
        foreach (var savedPlaylist in state.Playlists)
        {
            var existing = Playlists.FirstOrDefault(p => p.Name.Equals(savedPlaylist.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                foreach (var trackId in savedPlaylist.TrackIds)
                {
                    var track = _allTracks.FirstOrDefault(t => t.Model.Id == trackId)?.Model;
                    if (track is not null && existing.Model.Tracks.All(x => x.Id != trackId))
                    {
                        existing.Model.Tracks.Add(track);
                    }
                }
            }
            else
            {
                var tracks = savedPlaylist.TrackIds
                    .Select(id => _allTracks.FirstOrDefault(t => t.Model.Id == id)?.Model)
                    .Where(t => t is not null)
                    .Cast<Track>()
                    .ToList();

                Playlists.Add(new PlaylistViewModel(new Playlist
                {
                    Name = savedPlaylist.Name,
                    Description = "Custom playlist",
                    CoverPath = tracks.FirstOrDefault()?.CoverPath ?? "Assets/Covers/cover1.png",
                    Tracks = tracks
                }));
            }
        }

        foreach (var kv in state.RatingsByTrackId)
        {
            var track = _allTracks.FirstOrDefault(t => t.Model.Id == kv.Key);
            if (track is not null)
            {
                track.Rating = kv.Value;
            }
        }

        foreach (var recent in state.RecentlyPlayedTrackIds.Where(id => _allTracks.Any(t => t.Model.Id == id)))
        {
            _recentPlayedTrackIds.Add(recent);
        }

        foreach (var collection in state.Collections)
        {
            UserCollections.Add(collection);
        }

        foreach (var queueId in state.QueueTrackIds)
        {
            var track = _allTracks.FirstOrDefault(t => t.Model.Id == queueId);
            if (track is not null)
            {
                QueueTracks.Add(track);
            }
        }

        ValidateLibrary();

        ShuffleEnabled = state.ShuffleEnabled;
        RepeatMode = state.RepeatMode;

        SelectedCollection = UserCollections.FirstOrDefault();

        Volume = state.Volume;
        ActiveSection = state.LastSection;
        SelectedTrack = _allTracks.FirstOrDefault(t => t.Model.Id == state.LastTrackId) ?? _allTracks.FirstOrDefault();

        ApplySort();
        RefreshTracksView();
        OnPropertyChanged(nameof(QueueCount));
    }

    public event EventHandler? RequestSearchFocus;

    public ObservableCollection<TrackViewModel> Tracks { get; }
    public ICollectionView TracksView { get; }
    public ObservableCollection<PlaylistViewModel> Playlists { get; }
    public ObservableCollection<Album> Albums { get; }
    public ObservableCollection<string> Genres { get; }
    public ObservableCollection<string> Moods { get; }
    public ObservableCollection<SortOption> SortOptions { get; }
    public ObservableCollection<string> LibraryFolders { get; }
    public ObservableCollection<UserCollection> UserCollections { get; }
    public ObservableCollection<TrackViewModel> SelectedTracks { get; }
    public ObservableCollection<TrackViewModel> QueueTracks { get; }

    public RelayCommand SelectTrackCommand { get; }
    public RelayCommand PlayTrackCommand { get; }
    public RelayCommand ToggleFavoriteCommand { get; }
    public RelayCommand NavigateCommand { get; }
    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand AddToQueueCommand { get; }
    public RelayCommand AddSelectionToQueueCommand { get; }
    public RelayCommand ClearQueueCommand { get; }
    public RelayCommand RemoveQueueTrackCommand { get; }
    public RelayCommand MoveQueueUpCommand { get; }
    public RelayCommand MoveQueueDownCommand { get; }
    public RelayCommand FocusSearchCommand { get; }
    public RelayCommand PickLibraryFolderCommand { get; }
    public RelayCommand RemoveLibraryFolderCommand { get; }
    public RelayCommand CreateCollectionCommand { get; }
    public RelayCommand AddSelectedToCollectionCommand { get; }
    public RelayCommand RemoveSelectedFromCollectionCommand { get; }
    public RelayCommand SetRatingCommand { get; }
    public RelayCommand BulkToggleFavoriteCommand { get; }
    public RelayCommand ToggleShuffleCommand { get; }
    public RelayCommand CycleRepeatModeCommand { get; }
    public RelayCommand ToggleLyricsCommand { get; }
    public RelayCommand RescanLibraryCommand { get; }
    public RelayCommand RefreshMetadataCommand { get; }
    public RelayCommand ValidateLibraryCommand { get; }
    public RelayCommand RelinkTrackCommand { get; }
    public RelayCommand HandleDropItemsCommand { get; }

    public RelayCommand ContextPlayCommand { get; }
    public RelayCommand ContextPlayNextCommand { get; }
    public RelayCommand ContextAddToPlaylistCommand { get; }
    public RelayCommand ContextAddToCollectionCommand { get; }
    public RelayCommand ContextToggleFavoriteCommand { get; }
    public RelayCommand ContextRemoveFromLibraryCommand { get; }
    public RelayCommand ContextOpenFileLocationCommand { get; }

    public NavSection ActiveSection
    {
        get => _activeSection;
        set
        {
            if (SetField(ref _activeSection, value))
            {
                SaveAppState();
                RefreshTracksView();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                RefreshTracksView();
            }
        }
    }

    public string SelectedGenreFilter
    {
        get => _selectedGenreFilter;
        set
        {
            if (SetField(ref _selectedGenreFilter, value))
            {
                RefreshTracksView();
            }
        }
    }

    public string SelectedMoodFilter
    {
        get => _selectedMoodFilter;
        set
        {
            if (SetField(ref _selectedMoodFilter, value))
            {
                RefreshTracksView();
            }
        }
    }

    public SortOption SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (SetField(ref _selectedSortOption, value))
            {
                ApplySort();
                RefreshTracksView();
            }
        }
    }

    public UserCollection? SelectedCollection
    {
        get => _selectedCollection;
        set
        {
            if (SetField(ref _selectedCollection, value))
            {
                OnPropertyChanged(nameof(SelectedCollectionTracks));
                RefreshTracksView();
                AddSelectedToCollectionCommand.RaiseCanExecuteChanged();
                RemoveSelectedFromCollectionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public TrackViewModel? SelectedQueueTrack
    {
        get => _selectedQueueTrack;
        set => SetField(ref _selectedQueueTrack, value);
    }

    public bool ShuffleEnabled
    {
        get => _shuffleEnabled;
        set
        {
            if (SetField(ref _shuffleEnabled, value))
            {
                SaveAppState();
            }
        }
    }

    public RepeatMode RepeatMode
    {
        get => _repeatMode;
        set
        {
            if (SetField(ref _repeatMode, value))
            {
                OnPropertyChanged(nameof(RepeatModeDisplay));
                SaveAppState();
            }
        }
    }

    public bool IsLyricsCollapsed
    {
        get => _isLyricsCollapsed;
        set
        {
            if (SetField(ref _isLyricsCollapsed, value))
            {
                OnPropertyChanged(nameof(LyricsToggleText));
            }
        }
    }

    public string LyricsToggleText => IsLyricsCollapsed ? "Show Lyrics" : "Hide Lyrics";

    public string RepeatModeDisplay => RepeatMode switch
    {
        RepeatMode.Off => "Repeat: Off",
        RepeatMode.All => "Repeat: All",
        RepeatMode.One => "Repeat: One",
        _ => "Repeat: Off"
    };

    public string NewCollectionName
    {
        get => _newCollectionName;
        set => SetField(ref _newCollectionName, value);
    }

    public bool FavoritesOnly
    {
        get => _favoritesOnly;
        set
        {
            if (SetField(ref _favoritesOnly, value))
            {
                RefreshTracksView();
            }
        }
    }

    public bool RecentOnly
    {
        get => _recentOnly;
        set
        {
            if (SetField(ref _recentOnly, value))
            {
                RefreshTracksView();
            }
        }
    }

    public TrackViewModel? SelectedTrack
    {
        get => _selectedTrack;
        set
        {
            if (SetField(ref _selectedTrack, value))
            {
                OnPropertyChanged(nameof(HasTrackSelection));
                SaveAppState();
            }
        }
    }

    public bool HasTrackSelection => SelectedTrack is not null;
    public bool IsPlaying => _audioService.IsPlaying;
    public Track? CurrentTrack => _audioService.CurrentTrack;
    public string CurrentTrackTitle => CurrentTrack?.Title ?? "Nothing playing";
    public string CurrentTrackArtist => CurrentTrack?.Artist ?? "Pick a track to begin";
    public string CurrentCoverPath => CurrentTrack?.CoverPath ?? "Assets/Covers/cover1.png";
    public bool HasAnyTracks => Tracks.Count > 0;
    public bool HasVisibleTracks => TracksView.Cast<object>().Any();
    public bool IsNoResultsState => HasAnyTracks && !HasVisibleTracks;
    public bool IsLibraryEmptyState => !HasAnyTracks;
    public int QueueCount => QueueTracks.Count;
    public int RecentlyPlayedCount => _recentPlayedTrackIds.Count;

    public IReadOnlyList<TrackViewModel> SelectedCollectionTracks =>
        SelectedCollection is null
            ? []
            : _allTracks.Where(t => SelectedCollection.TrackIds.Contains(t.Model.Id)).ToList();

    public double CurrentPositionSeconds
    {
        get => _audioService.Position.TotalSeconds;
        set
        {
            _isSeeking = true;
            _audioService.Seek(TimeSpan.FromSeconds(value));
            _isSeeking = false;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentPositionDisplay));
        }
    }

    public double CurrentDurationSeconds => Math.Max(_audioService.Duration.TotalSeconds, 1);
    public string CurrentPositionDisplay => $"{(int)_audioService.Position.TotalMinutes}:{_audioService.Position.Seconds:D2}";
    public string CurrentDurationDisplay => $"{(int)_audioService.Duration.TotalMinutes}:{_audioService.Duration.Seconds:D2}";

    public double Volume
    {
        get => _volume;
        set
        {
            if (SetField(ref _volume, value))
            {
                _audioService.Volume = value;
                SaveAppState();
            }
        }
    }

    public void SetSelectedTracks(IList<object> selectedItems)
    {
        SelectedTracks.Clear();
        foreach (var track in selectedItems.OfType<TrackViewModel>())
        {
            SelectedTracks.Add(track);
        }

        AddSelectionToQueueCommand.RaiseCanExecuteChanged();
        BulkToggleFavoriteCommand.RaiseCanExecuteChanged();
        AddSelectedToCollectionCommand.RaiseCanExecuteChanged();
        RemoveSelectedFromCollectionCommand.RaiseCanExecuteChanged();
    }

    private bool FilterTracks(object item)
    {
        if (item is not TrackViewModel track)
        {
            return false;
        }

        var matchesSection = ActiveSection switch
        {
            NavSection.Favorites => track.IsFavorite,
            NavSection.RecentlyAdded => track.AddedOn >= DateTime.Today.AddDays(-7),
            NavSection.RecentlyPlayed => _recentPlayedTrackIds.Contains(track.Model.Id),
            NavSection.Collections => SelectedCollection is not null && SelectedCollection.TrackIds.Contains(track.Model.Id),
            _ => true
        };

        var q = SearchText.Trim();
        var matchesSearch = string.IsNullOrWhiteSpace(q)
            || track.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || track.Artist.Contains(q, StringComparison.OrdinalIgnoreCase)
            || track.Album.Contains(q, StringComparison.OrdinalIgnoreCase)
            || track.Genre.Contains(q, StringComparison.OrdinalIgnoreCase)
            || track.Model.MoodTags.Any(m => m.Contains(q, StringComparison.OrdinalIgnoreCase))
            || track.Model.Lyrics.Contains(q, StringComparison.OrdinalIgnoreCase)
            || UserCollections.Any(c => c.TrackIds.Contains(track.Model.Id) && c.Name.Contains(q, StringComparison.OrdinalIgnoreCase));

        var matchesGenre = SelectedGenreFilter == "All" || track.Genre == SelectedGenreFilter;
        var matchesMood = SelectedMoodFilter == "All" || track.Model.MoodTags.Contains(SelectedMoodFilter);
        var matchesFavorite = !FavoritesOnly || track.IsFavorite;
        var matchesRecent = !RecentOnly || track.AddedOn >= DateTime.Today.AddDays(-7);

        return matchesSection && matchesSearch && matchesGenre && matchesMood && matchesFavorite && matchesRecent;
    }

    private IEnumerable<TrackViewModel> ResolveActionTargets(TrackViewModel? track)
    {
        if (track is not null && SelectedTracks.Contains(track))
        {
            return SelectedTracks.ToList();
        }

        if (track is not null)
        {
            return [track];
        }

        return SelectedTracks.Count > 0 ? SelectedTracks.ToList() : [];
    }

    private void SelectTrack(TrackViewModel? track)
    {
        if (track is not null)
        {
            SelectedTrack = track;
        }
    }

    private void PlayTrack(TrackViewModel? track)
    {
        if (track is null)
        {
            return;
        }

        if (track.IsMissing)
        {
            PlayNext();
            return;
        }

        SelectedTrack = track;
        _audioService.LoadTrack(track.Model);
        _audioService.Play();
        AddRecentPlayed(track.Model.Id);
        _playPauseCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CurrentDurationSeconds));
    }

    private void ToggleFavorite(TrackViewModel? track)
    {
        var target = track ?? SelectedTrack;
        if (target is null)
        {
            return;
        }

        target.IsFavorite = !target.IsFavorite;
        RefreshTracksView();
    }

    private void BulkToggleFavorite()
    {
        if (SelectedTracks.Count == 0)
        {
            return;
        }

        var shouldFavorite = SelectedTracks.Any(t => !t.IsFavorite);
        foreach (var track in SelectedTracks)
        {
            track.IsFavorite = shouldFavorite;
        }

        RefreshTracksView();
    }

    private void Navigate(object? target)
    {
        if (target is string navName && Enum.TryParse<NavSection>(navName, out var parsed))
        {
            ActiveSection = parsed;
            FavoritesOnly = parsed == NavSection.Favorites;
            RecentOnly = parsed == NavSection.RecentlyAdded;
            if (!FavoritesOnly && parsed != NavSection.RecentlyAdded)
            {
                RecentOnly = false;
            }
        }
    }

    private void PlayPrevious()
    {
        var candidates = GetPlaybackCandidates();
        if (candidates.Count == 0)
        {
            return;
        }

        var current = SelectedTrack is not null ? candidates.IndexOf(SelectedTrack) : 0;
        var previous = current <= 0 ? candidates[^1] : candidates[current - 1];
        PlayTrack(previous);
    }

    private void PlayNext()
    {
        if (QueueTracks.Count > 0)
        {
            var nextFromQueue = QueueTracks[0];
            QueueTracks.RemoveAt(0);
            OnPropertyChanged(nameof(QueueCount));
            _nextCommand.RaiseCanExecuteChanged();
            ClearQueueCommand.RaiseCanExecuteChanged();
            SaveAppState();
            PlayTrack(nextFromQueue);
            return;
        }

        var candidates = GetPlaybackCandidates();
        if (candidates.Count == 0)
        {
            _audioService.Stop();
            return;
        }

        if (RepeatMode == RepeatMode.One && SelectedTrack is not null && !SelectedTrack.IsMissing)
        {
            PlayTrack(SelectedTrack);
            return;
        }

        TrackViewModel next;
        if (ShuffleEnabled)
        {
            next = candidates[_random.Next(candidates.Count)];
        }
        else
        {
            var current = SelectedTrack is not null ? candidates.IndexOf(SelectedTrack) : -1;
            if (current >= candidates.Count - 1)
            {
                if (RepeatMode == RepeatMode.All)
                {
                    next = candidates[0];
                }
                else
                {
                    _audioService.Stop();
                    return;
                }
            }
            else
            {
                next = candidates[current + 1];
            }
        }

        PlayTrack(next);
    }


    private List<TrackViewModel> GetPlaybackCandidates()
    {
        return _allTracks.Where(t => !t.IsMissing).ToList();
    }

    private void TogglePlayPause()
    {
        if (CurrentTrack is null && SelectedTrack is not null)
        {
            if (SelectedTrack.IsMissing)
            {
                PlayNext();
                return;
            }

            PlayTrack(SelectedTrack);
            return;
        }

        if (_audioService.IsPlaying)
        {
            _audioService.Pause();
        }
        else
        {
            _audioService.Play();
            if (SelectedTrack is not null)
            {
                AddRecentPlayed(SelectedTrack.Model.Id);
            }
        }
    }

    private void AddToQueue(TrackViewModel? track)
    {
        var targets = ResolveActionTargets(track).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var target in targets.Where(t => !t.IsMissing))
        {
            QueueTracks.Add(target);
        }

        OnPropertyChanged(nameof(QueueCount));
        _nextCommand.RaiseCanExecuteChanged();
        ClearQueueCommand.RaiseCanExecuteChanged();
        SaveAppState();
    }

    private void AddSelectionToQueue() => AddToQueue(null);

    private void ClearQueue()
    {
        QueueTracks.Clear();
        OnPropertyChanged(nameof(QueueCount));
        _nextCommand.RaiseCanExecuteChanged();
        ClearQueueCommand.RaiseCanExecuteChanged();
        SaveAppState();
    }

    private void RemoveQueueTrack(TrackViewModel? track)
    {
        var target = track ?? SelectedQueueTrack;
        if (target is null)
        {
            return;
        }

        QueueTracks.Remove(target);
        OnPropertyChanged(nameof(QueueCount));
        ClearQueueCommand.RaiseCanExecuteChanged();
        SaveAppState();
    }

    private void MoveQueue(TrackViewModel? track, int offset)
    {
        var target = track ?? SelectedQueueTrack;
        if (target is null)
        {
            return;
        }

        var index = QueueTracks.IndexOf(target);
        if (index < 0)
        {
            return;
        }

        var next = index + offset;
        if (next < 0 || next >= QueueTracks.Count)
        {
            return;
        }

        QueueTracks.Move(index, next);
        SaveAppState();
    }

    private void ToggleShuffle() => ShuffleEnabled = !ShuffleEnabled;

    private void CycleRepeatMode()
    {
        RepeatMode = RepeatMode switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.Off
        };
    }

    private void SetRating(object? parameter)
    {
        if (parameter is null)
        {
            return;
        }

        var rating = parameter switch
        {
            int value => value,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0
        };

        var targets = SelectedTracks.Count > 0 ? SelectedTracks.ToList() : SelectedTrack is not null ? [SelectedTrack] : [];
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var t in targets)
        {
            t.Rating = rating;
        }

        SaveAppState();
        RefreshTracksView();
    }

    private void AddToPlaylist(TrackViewModel? track)
    {
        var targets = ResolveActionTargets(track).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        var targetName = _selectionDialogService.ChoosePlaylist(Playlists.Select(p => p.Name));
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return;
        }

        var playlistVm = Playlists.FirstOrDefault(p => p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        if (playlistVm is null)
        {
            var playlist = new Playlist
            {
                Name = targetName,
                Description = "Custom playlist",
                CoverPath = targets.First().CoverPath,
                Tracks = []
            };
            playlistVm = new PlaylistViewModel(playlist);
            Playlists.Add(playlistVm);
        }

        foreach (var t in targets)
        {
            if (playlistVm.Model.Tracks.All(x => x.Id != t.Model.Id))
            {
                playlistVm.Model.Tracks.Add(t.Model);
            }
        }

        SaveAppState();
    }

    private void AddTrackToCollection(TrackViewModel? track)
    {
        var targets = ResolveActionTargets(track).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        var targetName = _selectionDialogService.ChooseCollection(UserCollections.Select(c => c.Name));
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return;
        }

        var collection = UserCollections.FirstOrDefault(c => c.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        if (collection is null)
        {
            collection = new UserCollection { Name = targetName };
            UserCollections.Add(collection);
        }

        SelectedCollection = collection;
        foreach (var t in targets)
        {
            if (!collection.TrackIds.Contains(t.Model.Id))
            {
                collection.TrackIds.Add(t.Model.Id);
            }
        }

        OnPropertyChanged(nameof(SelectedCollectionTracks));
        SaveAppState();
    }

    private void RemoveFromLibrary(TrackViewModel? track)
    {
        var targets = ResolveActionTargets(track).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var target in targets)
        {
            _allTracks.Remove(target);
            Tracks.Remove(target);
            QueueTracks.Remove(target);
            _recentPlayedTrackIds.Remove(target.Model.Id);
            foreach (var collection in UserCollections)
            {
                collection.TrackIds.Remove(target.Model.Id);
            }

            foreach (var playlist in Playlists)
            {
                playlist.Model.Tracks.RemoveAll(t => t.Id == target.Model.Id);
            }
        }

        RefreshFilterOptions();
        OnPropertyChanged(nameof(QueueCount));
        OnPropertyChanged(nameof(RecentlyPlayedCount));
        OnPropertyChanged(nameof(SelectedCollectionTracks));
        SaveAppState();
        RefreshTracksView();
    }

    private static void OpenFileLocation(TrackViewModel? track)
    {
        if (track is null || string.IsNullOrWhiteSpace(track.Model.AudioPath) || !File.Exists(track.Model.AudioPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{track.Model.AudioPath}\"",
            UseShellExecute = true
        });
    }

    private void PickLibraryFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a music folder",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            var path = dialog.FolderName;
            if (LibraryFolders.Contains(path))
            {
                return;
            }

            LibraryFolders.Add(path);
            ImportFolder(path);
            SaveAppState();
        }
    }

    private void RemoveLibraryFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        if (LibraryFolders.Remove(folder))
        {
            var toRemove = _allTracks.Where(t => t.Model.AudioPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var track in toRemove)
            {
                _allTracks.Remove(track);
                Tracks.Remove(track);
                QueueTracks.Remove(track);
                _recentPlayedTrackIds.Remove(track.Model.Id);
                foreach (var collection in UserCollections)
                {
                    collection.TrackIds.Remove(track.Model.Id);
                }
                foreach (var playlist in Playlists)
                {
                    playlist.Model.Tracks.RemoveAll(t => t.Id == track.Model.Id);
                }
            }

            RefreshFilterOptions();
            RefreshTracksView();
            OnPropertyChanged(nameof(SelectedCollectionTracks));
            OnPropertyChanged(nameof(QueueCount));
            OnPropertyChanged(nameof(RecentlyPlayedCount));
            SaveAppState();
        }
    }

    private void ImportFolder(string folderPath)
    {
        var imported = _musicImportService.LoadFromFolder(folderPath);
        if (imported.Count == 0)
        {
            return;
        }

        foreach (var track in imported)
        {
            if (_allTracks.Any(t => t.Model.Id == track.Id))
            {
                continue;
            }

            var vm = new TrackViewModel(track);
            _allTracks.Add(vm);
            Tracks.Add(vm);
        }

        RefreshFilterOptions();
        ApplySort();
        RefreshTracksView();
    }

    private void CreateCollection()
    {
        var name = NewCollectionName.Trim();
        if (string.IsNullOrWhiteSpace(name) || UserCollections.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var collection = new UserCollection { Name = name };
        UserCollections.Add(collection);
        SelectedCollection = collection;
        NewCollectionName = string.Empty;
        SaveAppState();
    }

    private void AddSelectedToCollection()
    {
        if (SelectedCollection is null || SelectedTracks.Count == 0)
        {
            return;
        }

        foreach (var track in SelectedTracks)
        {
            if (!SelectedCollection.TrackIds.Contains(track.Model.Id))
            {
                SelectedCollection.TrackIds.Add(track.Model.Id);
            }
        }

        OnPropertyChanged(nameof(SelectedCollectionTracks));
        RefreshTracksView();
        SaveAppState();
    }

    private void RemoveSelectedFromCollection()
    {
        if (SelectedCollection is null || SelectedTracks.Count == 0)
        {
            return;
        }

        foreach (var track in SelectedTracks)
        {
            SelectedCollection.TrackIds.Remove(track.Model.Id);
        }

        OnPropertyChanged(nameof(SelectedCollectionTracks));
        RefreshTracksView();
        SaveAppState();
    }


    public void HandleDropItems(IEnumerable<string>? paths)
    {
        if (paths is null)
        {
            return;
        }

        foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (Directory.Exists(path))
            {
                if (!LibraryFolders.Contains(path))
                {
                    LibraryFolders.Add(path);
                }

                ImportFolder(path);
                continue;
            }

            if (_musicImportService.IsSupportedAudioFile(path))
            {
                var imported = _musicImportService.LoadFromFile(path);
                if (imported is null || _allTracks.Any(t => t.Model.Id == imported.Id))
                {
                    continue;
                }

                var vm = new TrackViewModel(imported);
                _allTracks.Add(vm);
                Tracks.Add(vm);
            }
        }

        ValidateLibrary();
        RefreshFilterOptions();
        ApplySort();
        RefreshTracksView();
        SaveAppState();
    }

    private void RescanLibrary()
    {
        foreach (var folder in LibraryFolders.Where(Directory.Exists).ToList())
        {
            ImportFolder(folder);
        }

        ValidateLibrary();
        RefreshTracksView();
        SaveAppState();
    }

    private void RefreshMetadataForLibrary()
    {
        foreach (var track in _allTracks.Where(t => !t.IsMissing).ToList())
        {
            var refreshed = _musicImportService.LoadFromFile(track.Model.AudioPath);
            if (refreshed is null)
            {
                track.IsMissing = true;
                continue;
            }

            track.Model.Title = refreshed.Title;
            track.Model.Artist = refreshed.Artist;
            track.Model.Album = refreshed.Album;
            track.Model.Duration = refreshed.Duration;
            track.Model.Genre = refreshed.Genre;
            track.Model.Lyrics = refreshed.Lyrics;
            track.Model.CoverPath = refreshed.CoverPath;
            track.IsMissing = false;
        }

        RefreshTracksView();
        SaveAppState();
    }

    private void ValidateLibrary()
    {
        foreach (var track in _allTracks)
        {
            var path = track.Model.AudioPath;
            track.IsMissing = string.IsNullOrWhiteSpace(path) || !File.Exists(path);
        }

        var missingInQueue = QueueTracks.Where(t => t.IsMissing).ToList();
        foreach (var miss in missingInQueue)
        {
            QueueTracks.Remove(miss);
        }

        OnPropertyChanged(nameof(QueueCount));
    }

    private void RelinkTrack(TrackViewModel? track)
    {
        var target = track ?? SelectedTrack;
        if (target is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Relink missing file",
            Filter = "Audio files|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.ogg|All files|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            var refreshed = _musicImportService.LoadFromFile(dialog.FileName);
            if (refreshed is null)
            {
                return;
            }

            target.Model.AudioPath = dialog.FileName;
            target.Model.Title = refreshed.Title;
            target.Model.Artist = refreshed.Artist;
            target.Model.Album = refreshed.Album;
            target.Model.Duration = refreshed.Duration;
            target.Model.Genre = refreshed.Genre;
            target.Model.Lyrics = refreshed.Lyrics;
            target.Model.CoverPath = refreshed.CoverPath;
            target.IsMissing = false;
            SaveAppState();
            RefreshTracksView();
        }
    }

    private void AddRecentPlayed(string trackId)
    {
        _recentPlayedTrackIds.Remove(trackId);
        _recentPlayedTrackIds.Insert(0, trackId);
        if (_recentPlayedTrackIds.Count > RecentHistoryLimit)
        {
            _recentPlayedTrackIds.RemoveRange(RecentHistoryLimit, _recentPlayedTrackIds.Count - RecentHistoryLimit);
        }

        OnPropertyChanged(nameof(RecentlyPlayedCount));
        SaveAppState();
        if (ActiveSection == NavSection.RecentlyPlayed)
        {
            RefreshTracksView();
        }
    }

    private void ApplySort()
    {
        TracksView.SortDescriptions.Clear();
        switch (SelectedSortOption)
        {
            case SortOption.Title:
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.Title), ListSortDirection.Ascending));
                break;
            case SortOption.Artist:
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.Artist), ListSortDirection.Ascending));
                break;
            case SortOption.Album:
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.Album), ListSortDirection.Ascending));
                break;
            case SortOption.Duration:
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.DurationSeconds), ListSortDirection.Descending));
                break;
            case SortOption.Rating:
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.Rating), ListSortDirection.Descending));
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.Title), ListSortDirection.Ascending));
                break;
            case SortOption.RecentlyAdded:
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.AddedOn), ListSortDirection.Descending));
                break;
            case SortOption.FavoritesFirst:
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.IsFavorite), ListSortDirection.Descending));
                TracksView.SortDescriptions.Add(new SortDescription(nameof(TrackViewModel.Title), ListSortDirection.Ascending));
                break;
        }
    }

    private void RefreshFilterOptions()
    {
        Genres.Clear();
        Genres.Add("All");
        foreach (var genre in _allTracks
                     .Select(t => t.Genre?.Trim())
                     .Where(g => !string.IsNullOrWhiteSpace(g) && !g.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g))
        {
            Genres.Add(genre!);
        }

        Moods.Clear();
        Moods.Add("All");
        foreach (var mood in _allTracks
                     .SelectMany(t => t.Model.MoodTags)
                     .Select(m => m.Trim())
                     .Where(m => !string.IsNullOrWhiteSpace(m))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(m => m))
        {
            Moods.Add(mood);
        }

        if (!Genres.Contains(SelectedGenreFilter))
        {
            SelectedGenreFilter = "All";
        }

        if (!Moods.Contains(SelectedMoodFilter))
        {
            SelectedMoodFilter = "All";
        }
    }

    private void RefreshTracksView()
    {
        TracksView.Refresh();
        OnPropertyChanged(nameof(HasVisibleTracks));
        OnPropertyChanged(nameof(IsNoResultsState));
        OnPropertyChanged(nameof(IsLibraryEmptyState));
        OnPropertyChanged(nameof(HasAnyTracks));
    }

    private void SaveAppState()
    {
        var ratings = _allTracks.ToDictionary(t => t.Model.Id, t => t.Rating);
        _appStateService.Save(new AppState
        {
            Volume = Volume,
            LastSection = ActiveSection,
            LastTrackId = SelectedTrack?.Model.Id,
            LibraryFolders = LibraryFolders.ToList(),
            RatingsByTrackId = ratings,
            RecentlyPlayedTrackIds = _recentPlayedTrackIds.ToList(),
            Collections = UserCollections.Select(c => new UserCollection
            {
                Name = c.Name,
                TrackIds = [..c.TrackIds]
            }).ToList(),
            Playlists = Playlists.Select(p => new StoredPlaylist
            {
                Name = p.Name,
                TrackIds = p.Model.Tracks.Select(t => t.Id).Distinct().ToList()
            }).ToList(),
            QueueTrackIds = QueueTracks.Select(t => t.Model.Id).ToList(),
            ShuffleEnabled = ShuffleEnabled,
            RepeatMode = RepeatMode
        });
    }
}
