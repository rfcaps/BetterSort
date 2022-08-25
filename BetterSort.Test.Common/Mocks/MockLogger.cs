namespace BetterSort.Test.Common.Mocks {
  using Xunit.Abstractions;
  using IPALogger = IPA.Logging.Logger;

  public class MockLogger : IPALogger {
    private readonly ITestOutputHelper _output;

    public MockLogger(ITestOutputHelper output) {
      _output = output;
    }

    public override void Log(Level level, string message) {
      _output.WriteLine($"[{level}]: {message}");
    }
  }
}
