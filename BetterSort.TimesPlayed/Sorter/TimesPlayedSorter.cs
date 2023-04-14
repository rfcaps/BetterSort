namespace BetterSort.TimesPlayed.Sorter {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using IPALogger = IPA.Logging.Logger;
  using BetterSort.Common.Interfaces;
  using BetterSort.Common.External;

  public class TimesPlayedSorter : ISortFilter {
    /// <summary>
    /// Level id to number of times played.
    /// </summary>
    public Dictionary<string, int> TimesPlayed = new();

    public string Name => "Times played";

    public TimesPlayedSorter(IClock clock, IPALogger logger) {
      _clock = clock;
      _logger = logger;
    }

    public event Action<ISortFilterResult?> OnResultChanged = delegate { };

    public void NotifyChange(IEnumerable<ILevelPreview>? newLevels, bool isSelected = false, CancellationToken? token = null) {
      _isSelected = isSelected;
      _logger.Debug($"NotifyChange called: newLevels.Count: {newLevels.Count()}, isSelected: {isSelected}");

      if (newLevels == null) {
        return;
      }

      _triggeredLevels = newLevels;
      Sort();
    }

    public void RequestRefresh() {
      Sort();
    }

    private readonly IClock _clock;
    private readonly IPALogger _logger;
    private IEnumerable<ILevelPreview>? _triggeredLevels;
    private bool _isSelected = false;

    private void Sort() {
      if (!_isSelected) {
        return;
      }

      if (TimesPlayed == null) {
        throw new InvalidOperationException($"Precondition: {nameof(TimesPlayed)} should not be null.");
      }

      var comparer = new TimesPlayedComparer(TimesPlayed);
      var ordered = _triggeredLevels.OrderBy(x => x, comparer).ToList();
      OnResultChanged(new SortFilterResult(ordered));
      _logger.Info($"{nameof(TimesPlayedSorter)}: Sort finished, ordered[0].Name: {(ordered.Count == 0 ? null : ordered[0].SongName)}");
    }
  }
}
