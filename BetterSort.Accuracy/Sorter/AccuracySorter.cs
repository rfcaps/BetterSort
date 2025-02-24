using BetterSort.Accuracy.External;
using BetterSort.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IPALogger = IPA.Logging.Logger;

namespace BetterSort.Accuracy.Sorter {
  public class AccuracySorter : ISortFilter {
    public string Name => "Accuracy";

    public AccuracySorter(IPALogger logger, IAccuracyRepository repository) {
      _logger = logger;
      _repository = repository;
    }

    public event Action<ISortFilterResult?> OnResultChanged = delegate { };

    public void NotifyChange(IEnumerable<ILevelPreview>? newLevels, bool isSelected = false, CancellationToken? token = null) {
      try {
        _isSelected = isSelected;
        _triggeredLevels = newLevels;
        _logger.Debug($"{nameof(AccuracySorter)}.{nameof(NotifyChange)}: newLevels.Count: {newLevels.Count()}, isSelected: {isSelected}");

        if (newLevels == null || !_isSelected) {
          return;
        }

        OnResultChanged(Sort());
      }
      catch (Exception ex) {
        _logger.Error(ex);
        OnResultChanged(new SortFilterResult(_triggeredLevels ?? new List<ILevelPreview>()));
      }
    }

    internal List<LevelRecord> Mapping { get; private set; } = new();

    private readonly IPALogger _logger;
    private readonly IAccuracyRepository _repository;
    private IEnumerable<ILevelPreview>? _triggeredLevels;
    private bool _isSelected = false;

    private SortFilterResult Sort() {
      var records = _repository.Load().Result;
      if (records == null) {
        _logger.Info($"{nameof(AccuracySorter)}.{nameof(Sort)}: records is null, give it as is.");
        return new SortFilterResult(_triggeredLevels!);
      }

      var comparer = new LevelAccuracyComparer(records.BestRecords);
      var ordered = _triggeredLevels.SelectMany(comparer.Inflate).OrderBy(x => x, comparer).ToList();
      Mapping.Clear();
      foreach (var preview in ordered) {
        if (comparer.LevelMap.TryGetValue(preview, out var record)) {
          Mapping.Add(record);
        } else {
          break;
        }
      }
      var legend = AccuracyLegendMaker.GetLegend(Mapping, ordered.Count);
      _logger.Info($"{nameof(AccuracySorter)}: Sort finished, ordered[0].Name: {(ordered.Count == 0 ? "(empty)" : ordered[0].SongName)}");
      return new SortFilterResult(ordered, legend);
    }
  }
}
