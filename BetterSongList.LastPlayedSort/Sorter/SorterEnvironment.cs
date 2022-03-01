namespace BetterSongList.LastPlayedSort.Sorter {
  using BetterSongList.Api;
  using BetterSongList.LastPlayedSort.External;
  using System;
  using System.Collections.Generic;
  using IPALogger = IPA.Logging.Logger;

  internal class SorterEnvironment {
    public SorterEnvironment(IPALogger logger, IPlayedDateRepository repository, IPlayEventSource playEventSource, LastPlayedDateSorter sorter) {
      _logger = logger;
      _repository = repository;
      _playEventSource = playEventSource;
      _sorter = sorter;
    }

    public void Start(bool register) {
      StoredData? data = _repository.Load();
      _sorter.LastPlayedDates = data?.LastPlays ?? new Dictionary<string, DateTime>();
      _playEventSource.OnSongPlayed += RecordHistory;
      if (register) {
        BetterSongListApi.RegisterSorter(_sorter);
        _logger.Debug("Registered last play date sorter.");
      }
      else {
        _logger.Debug("Skip last play date sorter registration.");
      }
    }

    private readonly IPALogger _logger;
    private readonly IPlayedDateRepository _repository;
    private readonly IPlayEventSource _playEventSource;
    private readonly LastPlayedDateSorter _sorter;

    private void RecordHistory(string levelId, DateTime instant) {
      _logger.Info($"Record play {levelId}: {instant}");
      _sorter.LastPlayedDates[levelId] = instant;
      _repository.Save(_sorter.LastPlayedDates);
    }
  }
}
