namespace BetterSort.Accuracy.External {
  using BetterSort.Accuracy.Sorter;
  using Newtonsoft.Json;
  using Scoresaber;
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using IPALogger = IPA.Logging.Logger;

  public class ScoresaberImporter : IScoreImporter {
    private readonly IPALogger _logger;
    private readonly ILeaderboardId _id;
    private readonly ScoreImporterHelper _helper;

    internal ScoresaberImporter(IPALogger logger, ILeaderboardId id, ScoreImporterHelper helper) {
      _logger = logger;
      _id = id;
      _helper = helper;
    }

    public async Task<List<BestRecord>?> GetPlayerBests() {
      string? id = await _id.GetUserId().ConfigureAwait(false);
      if (id == null) {
        _logger.Info("Cannot get user ID. Abort data import.");
        return null;
      }

      var records = await GetRecords(id).ConfigureAwait(false);
      return records;
    }

    public async Task<(List<BestRecord> Records, int MaxPage)?> GetPagedRecord(string platformId, int page) {
      string url = GetUrl(platformId, page);
      string? json = await _helper.GetJsonWithRetry(url).ConfigureAwait(false);
      if (json == null) {
        return null;
      }

      return await GetScores(json);
    }

    private async Task<List<BestRecord>> GetRecords(string platformId) {
      var records = new List<BestRecord>();
      for (int page = 1; ; page++) {
        _logger.Debug($"Try getting scoresaber page {page}...");

        var scores = await GetPagedRecord(platformId, page).ConfigureAwait(false);
        if (scores is not (var data, var maxPage)) {
          break;
        }

        records.AddRange(data);
        if (page >= maxPage) {
          _logger.Info("Scoresaber score last page reached.");
          break;
        }
      }

      return records;
    }

    private async Task<(List<BestRecord> Records, int MaxPage)?> GetScores(string json) {
      var records = new List<BestRecord>();

      var page = JsonConvert.DeserializeObject<PagedPlayerScores>(json);
      var scores = page.PlayerScores;
      if (scores == null) {
        _logger.Warn("Records field is missing. Can't import from scoresaber.");
        return null;
      }

      foreach (var score in scores) {
        string? hash = score.Leaderboard?.SongHash;
        if (hash == null) {
          _logger.Warn("Cannot get song hash from scoresaber. skip.");
          continue;
        }

        int? multiplied = score.Score?.BaseScore;
        int? maxScore = score.Leaderboard?.MaxScore;
        if (multiplied > 0 && (maxScore == null || maxScore == 0)) {
          maxScore = null;
          // TODO: calculate max score
          //await _scorer.GetDifficultyBeatmap(score?.Leaderboard?.SongHash, score?.Leaderboard?.);
          //maxScore = await _scorer.CalculateMaxScore()
        }
        double? accuracyOrNull = (double?)multiplied / maxScore;
        if (accuracyOrNull is not double accuracy) {
          _logger.Warn($"Can't derive scoresaber accuracy. skip({hash}, {score.Leaderboard?.SongName})");
          continue;
        }
        var difficulty = ConvertToEnum(score.Leaderboard?.Difficulty?.Difficulty);
        if (difficulty == null) {
          _logger.Warn($"Unknown scoresaber difficulty. Regard it as ExpertPlus({hash}, {score.Leaderboard?.SongName})");
        }

        records.Add(new BestRecord() {
          SongHash = hash,
          Mode = GetGameMode(score.Leaderboard?.Difficulty?.GameMode),
          Difficulty = difficulty ?? RecordDifficulty.ExpertPlus,
          Accuracy = accuracy,
          Score = score.Score?.ModifiedScore ?? 0,
        });
      }

      int maxPage = (int)Math.Ceiling((double)page.Metadata!.Total / page.Metadata.ItemsPerPage);
      return (records, maxPage);
    }

    private static string GetUrl(string platformId, int page) {
      return $"https://scoresaber.com/api/player/{platformId}/scores?page={page}&sort=recent";
    }

    private static string GetGameMode(string? mode) {
      return mode?.Replace("Solo", "") ?? "Standard";
    }

    private static RecordDifficulty? ConvertToEnum(int? scoresaberDifficulty) {
      return scoresaberDifficulty switch {
        1 => RecordDifficulty.Easy,
        3 => RecordDifficulty.Normal,
        5 => RecordDifficulty.Hard,
        7 => RecordDifficulty.Expert,
        9 => RecordDifficulty.ExpertPlus,
        _ => null,
      };
    }
  }
}

namespace Scoresaber {
  using Newtonsoft.Json;
  using System;
  using System.Collections.Generic;

  public class PagedPlayerScores {
    [JsonProperty("playerScores")]
    public List<PlayerScore>? PlayerScores { get; set; }

    [JsonProperty("metadata")]
    public PageMetadata? Metadata { get; set; }

  }

  public class PageMetadata {
    [JsonProperty("total")]
    public int Total { get; set; }

    [JsonProperty("page")]
    public int Page { get; set; }

    [JsonProperty("itemsPerPage")]
    public int ItemsPerPage { get; set; }
  }

  public class PlayerScore {
    [JsonProperty("score")]
    public Score? Score { get; set; }

    [JsonProperty("leaderboard")]
    public Leaderboard? Leaderboard { get; set; }
  }

  public class Leaderboard {
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("songHash")]
    public string? SongHash { get; set; }

    [JsonProperty("songName")]
    public string? SongName { get; set; }

    [JsonProperty("songSubName")]
    public string? SongSubName { get; set; }

    [JsonProperty("songAuthorName")]
    public string? SongAuthorName { get; set; }

    [JsonProperty("levelAuthorName")]
    public string? LevelAuthorName { get; set; }

    [JsonProperty("difficulty")]
    public SongDifficulty? Difficulty { get; set; }

    [JsonProperty("maxScore")]
    public int MaxScore { get; set; }

    [JsonProperty("createdDate")]
    public string? CreatedDate { get; set; }
    [JsonProperty("rankedDate")]
    public string? RankedDate { get; set; }
    [JsonProperty("qualifiedDate")]
    public string? QualifiedDate { get; set; }

    [JsonProperty("lovedDate")]
    public DateTime? LovedDate { get; set; }

    [JsonProperty("ranked")]
    public bool Ranked { get; set; }

    [JsonProperty("qualified")]
    public bool Qualified { get; set; }

    [JsonProperty("loved")]
    public bool Loved { get; set; }

    [JsonProperty("maxPP")]
    public double MaxPp { get; set; }

    [JsonProperty("stars")]
    public double Stars { get; set; }

    [JsonProperty("plays")]
    public int Plays { get; set; }

    [JsonProperty("dailyPlays")]
    public int DailyPlays { get; set; }

    [JsonProperty("positiveModifiers")]
    public bool PositiveModifiers { get; set; }

    // "playerScore": null,
    [JsonProperty("coverImage")]
    public string? CoverImage { get; set; }

    // "difficulties": null,
  }

  public class SongDifficulty {
    [JsonProperty("leaderboardId")]
    public int LeaderboardId { get; set; }

    [JsonProperty("difficulty")]
    public int Difficulty { get; set; }

    [JsonProperty("gameMode")]
    public string? GameMode { get; set; }

    [JsonProperty("difficultyRaw")]
    public string? DifficultyRaw { get; set; }
  }

  public class Score {
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("rank")]
    public int Rank { get; set; }

    [JsonProperty("baseScore")]
    public int BaseScore { get; set; }

    [JsonProperty("modifiedScore")]
    public int ModifiedScore { get; set; }

    [JsonProperty("pp")]
    public double Pp { get; set; }

    [JsonProperty("weight")]
    public double Weight { get; set; }

    [JsonProperty("modifiers")]
    public string? Modifiers { get; set; }

    [JsonProperty("multiplier")]
    public double Multiplier { get; set; }

    [JsonProperty("badCuts")]
    public int BadCuts { get; set; }

    [JsonProperty("missedNotes")]
    public int MissedNotes { get; set; }

    [JsonProperty("maxCombo")]
    public int MaxCombo { get; set; }

    [JsonProperty("fullCombo")]
    public bool FullCombo { get; set; }

    [JsonProperty("hmd")]
    public int Hmd { get; set; }

    [JsonProperty("timeSet")]
    public DateTime TimeSet { get; set; }

    [JsonProperty("hasReplay")]
    public bool HasReplay { get; set; }
  }
}
