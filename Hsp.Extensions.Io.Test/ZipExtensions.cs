using System.IO.Compression;
using Bogus;

namespace Hsp.Extensions.Io.Test;

public class ZipExtensions
{
  private readonly Faker _faker = new();

  [Fact]
  public void CreateZipFileFromString()
  {
    var content = _faker.Lorem.Paragraphs(2);
    var filename = _faker.System.FileName();

    using var ms = new MemoryStream();
    using (var zf1 = new ZipArchive(ms, ZipArchiveMode.Create, true))
    {
      zf1.AddEntry(filename, content);
    }

    ms.Position = 0;
    using (var zf2 = new ZipArchive(ms, ZipArchiveMode.Read))
    {
      Assert.Single(zf2.Entries);
      using (var sr = new StreamReader(zf2.GetEntry(filename).Open()))
      {
        var actualContent = sr.ReadToEnd();
        Assert.Equal(content, actualContent);
      }
    }
  }
}