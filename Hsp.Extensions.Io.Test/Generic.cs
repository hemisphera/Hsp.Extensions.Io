using Hsp.System.WindowsServices;

namespace Hsp.Extensions.Io.Test;

public class Generic
{
  [Fact]
  public async Task RunCmdAndTimeout()
  {
    var runner = new CommandLineRunner
    {
      TimeoutPeriod = TimeSpan.FromSeconds(3),
      Filename = "cmd.exe"
    };
    var result = await runner.Execute().Wait();
    Assert.Contains(result.ErrorLines, l => l.Contains("timed out"));
  }

  [Fact]
  public async Task RunCmdAndList()
  {
    var runner = new CommandLineRunner
    {
      TimeoutPeriod = TimeSpan.FromSeconds(5),
      Filename = "cmd.exe"
    };
    var result = await runner.Execute(ArgBuilder.Create().Add("/c").Add("dir")).Wait(0);
    Assert.NotEmpty(result.OutputLines);
    Assert.Empty(result.ErrorLines);
  }

  [Fact]
  public async Task WaitCapturesAllOutputLines()
  {
    const int lineCount = 1000;
    var contentFile = Path.Combine(Path.GetTempPath(), $"hsp-io-test-{Guid.NewGuid():N}.txt");
    await File.WriteAllTextAsync(contentFile,
      string.Join(Environment.NewLine, Enumerable.Range(0, lineCount).Select(i => $"Line {i}")) + Environment.NewLine);
    try
    {
      var runner = new CommandLineRunner
      {
        TimeoutPeriod = TimeSpan.FromSeconds(30),
        Filename = "cmd.exe",
        // Simulates a consumer that takes time per line (e.g. logging), which widens
        // the gap between the process exiting and its output being fully dispatched.
        OutputCallback = _ => Thread.Sleep(1)
      };
      var result = await runner.Execute(ArgBuilder.Create().Add("/c").Add("type").Add($"\"{contentFile}\"")).Wait(0);
      Assert.Equal(lineCount, result.OutputLines.Count);
    }
    finally
    {
      File.Delete(contentFile);
    }
  }

  [Theory]
  [InlineData("c:\\program files\\some.exe data /config \"random\"", 3)]
  [InlineData("\"c:\\program files\\some.exe\" \"data /config\" \"random\"", 2)]
  [InlineData("c:\\programfiles\\some.exe data /config \"random\"", 3)]
  public void ParseBinPath(string binPath, int expectedParts)
  {
    var si = ServiceImage.FromBinPath(binPath);
    Assert.EndsWith(".exe", si.Filename, StringComparison.OrdinalIgnoreCase);
    Assert.Equal(expectedParts, si.Arguments.SplitQuotedString().Length);
  }

  [Theory]
  [InlineData("some string")]
  [InlineData("\"some string\"")]
  public void Enclose(string str)
  {
    str = str.Enclose();
    Assert.True(str.EndsWith("\"") && str.StartsWith("\""));
  }
}