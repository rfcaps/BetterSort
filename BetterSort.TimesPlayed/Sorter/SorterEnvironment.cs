namespace BetterSort.TimesPlayed.Sorter {
  using BetterSongList;
  using BetterSort.Common.Compatibility;
  using BetterSort.TimesPlayed.External;
  using System;
  using System.Collections.Generic;
  using IPALogger = IPA.Logging.Logger;

  public class SorterEnvironment {
    public SorterEnvironment(IPALogger logger, ITimesPlayedRepository repository, IPlayEventSource playEventSource, TimesPlayedSorter sorter, FilterSortAdaptor adaptor) {
      _logger = logger;
      _repository = repository;
      _playEventSource = playEventSource;
      _sorter = sorter;
      _adaptor = adaptor;
    }

    public void Start(bool register) {
      var data = _repository.Load();
      _sorter.TimesPlayed = data?.LastPlays is Dictionary<string, int> timesPlayed
        ? timesPlayed
        : new Dictionary<string, int>();
      _playEventSource.OnSongPlayed += RecordHistory;
      if (register) {
        SortMethods.RegisterCustomSorter(_adaptor);
        _logger.Debug("Registered last play date sorter.");
      }
      else {
        _logger.Debug("Skip last play date sorter registration.");
      }
    }

    private readonly IPALogger _logger;
    private readonly ITimesPlayedRepository _repository;
    private readonly IPlayEventSource _playEventSource;
    private readonly TimesPlayedSorter _sorter;
    private readonly FilterSortAdaptor _adaptor;

    private void RecordHistory(string levelId) {
      _logger.Debug($"Record play {levelId}");
      _sorter.TimesPlayed[levelId] += 1;
      _repository.Save(_sorter.TimesPlayed);
    }
  }
}
