namespace BetterSort.TimesPlayed.External {
  using BetterSort.TimesPlayed.Sorter;
  using Newtonsoft.Json;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using IPALogger = IPA.Logging.Logger;

  public interface ITimesPlayedRepository {
    void Save(IReadOnlyDictionary<string, int> timesPlayed);

    StoredData? Load();
  }

  internal class TimesPlayedRepository : ITimesPlayedRepository {
    public TimesPlayedRepository(IPALogger logger) {
      _logger = logger;
    }

    public void Save(IReadOnlyDictionary<string, int> timesPlayed) {
      var sorted = new SortedDictionary<string, int>(
        timesPlayed.ToDictionary(x => x.Key, x => x.Value),
        new TimesPlayedComparer(timesPlayed)
      );
      string json = JsonConvert.SerializeObject(new StoredData() {
        Version = $"{typeof(TimesPlayedRepository).Assembly.GetName().Version}",
        TimesPlayed = sorted,
      }, Formatting.Indented);
      File.WriteAllText(_path, json);
      _logger.Info($"Saved {timesPlayed.Count} records");
    }

    public StoredData? Load() {
      if (!File.Exists(_path)) {
        _logger.Debug($"Try loading but play history is not exist.");
        return null;
      }

      try {
        string json = File.ReadAllText(_path);
        var data = JsonConvert.DeserializeObject<StoredData>(json);
        _logger.Info($"Loaded {data.LastPlays?.Count.ToString() ?? "no"} records");
        return data;
      }
      catch (Exception exception) {
        _logger.Warn(exception);
        return null;
      }
    }

    private readonly string _path = Path.Combine(Environment.CurrentDirectory, "UserData", "TimesPlayed.json.dat");
    private readonly IPALogger _logger;
  }

  public class ImmigrationRepository : ITimesPlayedRepository {
    internal ImmigrationRepository(IPALogger logger, ITimesPlayedRepository repository, TimesPlayedHistoryImporter importer) {
      _logger = logger;
      _repository = repository;
      _importer = importer;
    }

    public StoredData? Load() {
      return _repository.Load() ?? TryImportingSongPlayHistoryData();
    }

    public void Save(IReadOnlyDictionary<string, int> timesPlayed) {
      _repository.Save(timesPlayed);
    }

    private readonly IPALogger _logger;
    private readonly ITimesPlayedRepository _repository;
    private readonly TimesPlayedHistoryImporter _importer;

    private StoredData? TryImportingSongPlayHistoryData() {
      var history = _importer.Load();
      if (history == null) {
        _logger.Debug("Searched SongPlayHistory data but not found. No history restored.");
        return null;
      }

      _logger.Debug($"Imported SongPlayHistory data, total count: {history.Count}");
      return new StoredData() { TimesPlayed = history };
    }
  }
}
