using Bogus;

namespace Hsp.Extensions.Io.Test;

public class TempFolderTests
{
  private readonly Faker _faker;


  public TempFolderTests()
  {
    _faker = new Faker();
  }

  private TempFolder CreateFolderWithFiles(int rootFiles, int subFolderFiles)
  {
    var tf = new TempFolder();
    foreach (var unused in Enumerable.Range(0, rootFiles))
    {
      tf.WriteFile(_faker.System.FileName(), _faker.Random.Words(10));
    }

    var subfolder = _faker.System.FileName("");
    foreach (var unused in Enumerable.Range(0, subFolderFiles))
    {
      tf.WriteFile(Path.Combine(subfolder, _faker.System.FileName()), _faker.Random.Words(10));
    }

    return tf;
  }


  [Theory]
  [InlineData(5, 5)]
  public void CreateTempFolderZip(int rootFiles, int subFolderFiles)
  {
    using var tf = CreateFolderWithFiles(rootFiles, subFolderFiles);
    using var ms = new MemoryStream();
    tf.SaveAsZip(ms, true);

    var tf2 = new TempFolder();
    tf2.ExtractArchive(ms);

    Assert.Single(tf2.Folder.GetDirectories());
    Assert.Equal(rootFiles, tf2.Folder.GetFiles().Length);
    Assert.Equal(rootFiles + subFolderFiles, tf2.Folder.GetFiles("*", SearchOption.AllDirectories).Length);
  }

  [Fact]
  public void OverwriteExtracting()
  {
    using var tf = CreateFolderWithFiles(_faker.Random.Int(5, 10), _faker.Random.Int(5, 10));
    using var ms = new MemoryStream();

    using var tempZipfolder = new TempFolder(); 
    var zipFilePath = tempZipfolder.GetFile(_faker.System.FileName("zip")).FullName;
    tf.SaveAsZip(zipFilePath);
    var zf = new FileInfo(zipFilePath);

    using var tf2 = new TempFolder();
    tf2.ExtractArchive(zf.OpenRead());
    ms.Position = 0;
    tf2.ExtractArchive(zf.OpenRead());
  }
}