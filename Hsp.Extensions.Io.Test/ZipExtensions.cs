using System.IO.Compression;
using Bogus;

namespace Hsp.Extensions.Io.Test;

public class ZipExtensions
{
  private readonly Faker _faker = new();

  [Fact]
  public void ExtractWithSubfolder()
  {
    // arrange: create a ZIP file with some text files 
    const string subFolderName = "subdir";
    var numFiles = _faker.Random.Int(3, 5);

    using var source = new MemoryStream();
    using (var za = new ZipArchive(source, ZipArchiveMode.Create, true))
    {
      za.AddEntry($"{subFolderName}/");
      for (var i = 0; i < numFiles; i++)
      {
        za.AddEntry($"{subFolderName}/{i}.txt", _faker.Lorem.Paragraphs());
      }
    }

    // act: decompress that ZIP to a new temporary folder
    source.Position = 0;
    var target = new TempFolder();
    target.ExtractArchive(source);

    // assert: must have a subfolder
    var subDir = new DirectoryInfo(Path.Combine(target.FolderPath, subFolderName));
    Assert.True(subDir.Exists);
    Assert.Empty(target.Folder.EnumerateFiles());
    Assert.Equal(numFiles, subDir.EnumerateFiles().Count());
  }

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