namespace BetterSongList.LastPlayedSort.External {
  using BS_Utils.Utilities;
  using System;
  using IPALogger = IPA.Logging.Logger;

  public interface IPlayEventSource : IDisposable {
    event Action<string, DateTime> OnSongPlayed;
  }

  internal class BsUtilsEventSource : IPlayEventSource {
    public event Action<string, DateTime> OnSongPlayed = delegate { };

    public BsUtilsEventSource(IClock clock, IPALogger logger) {
      _clock = clock;
      _logger = logger;
      BSEvents.levelSelected += PreserveSelectedLevel;
      BSEvents.gameSceneLoaded += RecordStartTime;
      BSEvents.LevelFinished += DispatchIfLongEnough;
    }

    public void Dispose() {
      BSEvents.LevelFinished -= DispatchIfLongEnough;
      BSEvents.gameSceneLoaded -= RecordStartTime;
      BSEvents.levelSelected -= PreserveSelectedLevel;
    }

    private readonly IClock _clock;
    private readonly IPALogger _logger;
    private string _selectedLevelId = "";
    private string _selectedSongName = "";
    private float _songDuration;
    private DateTime _startTime;

    private void PreserveSelectedLevel(LevelCollectionViewController arg1, IPreviewBeatmapLevel level) {
      _selectedLevelId = level.levelID;
      _selectedSongName = level.songName;
      _songDuration = level.songDuration;
    }

    private void RecordStartTime() {
      _startTime = _clock.Now;
    }

    private void DispatchIfLongEnough(object sender, LevelFinishedEventArgs finished) {
      if (_selectedLevelId == "") {
        _logger.Warn("Cannot determine selected level.");
        return;
      }
      if (finished is not LevelFinishedWithResultsEventArgs) {
        _logger.Debug($"Skip tutorial play record.");
        return;
      }

      DateTime now = _clock.Now;
      TimeSpan duration = now - _startTime;
      bool isPlayedTooShort = duration.Seconds < 10 && _songDuration > 10;
      if (isPlayedTooShort) {
        _logger.Debug($"Skip due to too short play: {_selectedSongName}");
        return;
      }

      _logger.Debug($"Dispatch play event: {_selectedSongName}");
      OnSongPlayed(_selectedLevelId, now);
    }
  }
}
