namespace Hsp.Extensions.Io.Test;

public class UnitTest1
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
}