namespace BetterSort.TimesPlayed.External {
  using Newtonsoft.Json;
  using System;
  using System.Collections.Generic;

  public class StoredData {
    [JsonProperty("version")]
    public string? Version { get; set; }

    [JsonProperty("timesPlayed")]
    public IDictionary<string, int>? TimesPlayed { get; set; }
  }
}
