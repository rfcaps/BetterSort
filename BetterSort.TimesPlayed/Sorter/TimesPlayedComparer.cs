namespace BetterSort.TimesPlayed.Sorter {
  using BetterSort.Common.Interfaces;
  using System;
  using System.Collections.Generic;

  internal class TimesPlayedComparer : IComparer<ILevelPreview>, IComparer<string> {
    public TimesPlayedComparer(IReadOnlyDictionary<string, int> timesPlayed) {
      _timesPlayed = timesPlayed;
    }

    public int Compare(ILevelPreview a, ILevelPreview b) {
      return Compare(a.LevelId, b.LevelId);
    }

    /// <summary>
    /// Use with map id that will be serialized.
    /// </summary>
    public int Compare(string a, string b) {
      if (_timesPlayed == null) {
        return 0;
      }

      if (_timesPlayed.TryGetValue(a, out var timesPlayedOfA)) {
        if (_timesPlayed.TryGetValue(b, out var timesPlayedOfB)) {
          int descending = timesPlayedOfB.CompareTo(timesPlayedOfA);
          return descending;
        }
        return -1;
      }
      else {
        if (_timesPlayed.TryGetValue(b, out var _)) {
          return 1;
        }
        return 0;
      }
    }

    /// <summary>
    /// Level id to instant.
    /// </summary>
    private readonly IReadOnlyDictionary<string, int> _timesPlayed;
  }
}
