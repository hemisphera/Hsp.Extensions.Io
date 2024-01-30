using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hsp.Extensions.Io
{
  /// <summary>
  /// Result of a file collection operation.
  /// </summary>
  public class CollectFilesResult : ICollection<string>
  {
    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    private readonly List<string> _items = new List<string>();

    /// <summary>
    /// The root directory of the collection.
    /// </summary>
    public DirectoryInfo Root { get; }


    /// <summary>
    /// </summary>
    /// <param name="root"></param>
    public CollectFilesResult(DirectoryInfo root)
    {
      Root = root;
    }


    /// <summary>
    /// Add the specified path to the collection.
    /// </summary>
    /// <param name="fullPath">The full path to the file.</param>
    public void Add(string fullPath)
    {
      if (!fullPath.StartsWith(Root.FullName, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("The full path must start with the parent path");
      var relativePath = new String(fullPath.Substring(Root.FullName.Length).SkipWhile(c => c == Path.DirectorySeparatorChar).ToArray());
      _items.Add(relativePath);
    }

    /// <inheritdoc />
    public void Clear()
    {
      _items.Clear();
    }

    /// <inheritdoc />
    public bool Contains(string item)
    {
      return _items.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(string[] array, int arrayIndex)
    {
      _items.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(string item)
    {
      return _items.Remove(item);
    }

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator()
    {
      return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}