namespace BetterSort.TimesPlayed.External {
  using Newtonsoft.Json;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using IPALogger = IPA.Logging.Logger;

  public class TimesPlayedHistoryImporter {
    public TimesPlayedHistoryImporter(IPALogger logger) {
      _logger = logger;
    }

    public Dictionary<string, int>? Load() {
      if (!File.Exists(_sphJsonPath)) {
        return null;
      }

      var result = new Dictionary<string, int>();

      string json = File.ReadAllText(_sphJsonPath);
      var history = JsonConvert.DeserializeObject<IDictionary<string, IList<Record>>>(json);
      foreach (var record in history) {
        string difficulty = record.Key;
        string[] fields = difficulty.Split(_sphSeparator, StringSplitOptions.None);
        if (fields.Length != 3) {
          _logger.Warn($"Can't parse {difficulty}. Skip.");
          continue;
        }

        int timesPlayed = record.Value.Count;

        if (result.TryGetValue(fields[0], out var existing)) {
          result[fields[0]] = timesPlayed < existing ? existing : timesPlayed;
        }
        else {
          result[fields[0]] = timesPlayed;
        }
      }

      return result;
    }

    private static readonly string _sphJsonPath = Path.Combine(Environment.CurrentDirectory, "UserData", "SongPlayData.json");
    private static readonly string[] _sphSeparator = new[] { "___" };
    private readonly IPALogger _logger;

    class Record {
#pragma warning disable 0649
      public long Date;
#pragma warning restore 0649
    }
  }
}
