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

  [Theory]
  [InlineData("c:\\program files\\some.exe data /config \"random\"", 3)]
  [InlineData("\"c:\\program files\\some.exe\" \"data /config\" \"random\"", 2)]
  [InlineData("c:\\programfiles\\some.exe data /config \"random\"", 3)]
  public void ParseBinPath(string binPath, int expectedParts)
  {
    var si = ServiceImage.FromBinPath(binPath);
    Assert.EndsWith(".exe", si.Filename.Unenclose(), StringComparison.OrdinalIgnoreCase);
    Assert.Equal(expectedParts, si.ArgumentParts.Count);
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