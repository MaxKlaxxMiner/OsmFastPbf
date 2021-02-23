using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable UnusedMember.Global

namespace OsmFastPbf
{
  /// <summary>
  /// Klasse zum schnellen reservieren von Arrays
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public sealed class MemArray<T> : IList<T>, IDisposable where T : struct
  {
    /// <summary>
    /// Array mit den Rohdaten
    /// </summary>
    T[] data;
    /// <summary>
    /// merkt sich die Größen-ID
    /// </summary>
    readonly int sizeId;
    /// <summary>
    /// merkt sich den benutzten Block
    /// </summary>
    readonly int blockIndex;
    /// <summary>
    /// Anzahl der benutzbaren Elemente
    /// </summary>
    int count;

    /// <summary>
    /// Struktur eines reservierten Speicherblockes
    /// </summary>
    struct MemBlock
    {
      /// <summary>
      /// merkt sich das Array, welches die Daten enthält
      /// </summary>
      public readonly T[] data;
      /// <summary>
      /// gibt an, ob der Block in Benutzung ist
      /// </summary>
      public readonly bool active;

      /// <summary>
      /// Konstruktor
      /// </summary>
      /// <param name="data">Daten-Array, welches reserviert wurde</param>
      /// <param name="active">gibt an, ob der Block bereits benutzt wird</param>
      public MemBlock(T[] data, bool active)
      {
        this.data = data;
        this.active = active;
      }

      /// <summary>
      /// gibt den Inhalt als lesbare Zeichenfolge zurück
      /// </summary>
      /// <returns>lesbare Zeichenfolge</returns>
      public override string ToString()
      {
        return new { active, data = "[" + data.Length + "]" }.ToString();
      }
    }

    /// <summary>
    /// merkt sich alle bereits reservierten Speicherblöcke
    /// </summary>
    static readonly List<MemBlock>[] memBlocks = Enumerable.Range(0, 24).Select(x => new List<MemBlock>()).ToArray();

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="count">Anzahl der zu reservierenden Elemente</param>
    /// <param name="clear">gibt an, ob das Array geleert werden soll (default: false)</param>
    public MemArray(int count, bool clear = false)
    {
      int size = 255;
      int sizeId = 0;
      while (size < count && size > 0)
      {
        size *= 2;
        sizeId++;
      }
      if (size < 15) throw new OutOfMemoryException();

      this.count = count;
      this.sizeId = sizeId;

      var memList = memBlocks[sizeId];
      lock (memList)
      {
        for (int i = 0; i < memList.Count; i++)
        {
          if (!memList[i].active)
          {
            data = memList[i].data;
            if (clear) Array.Clear(data, 0, count);
            blockIndex = i;
            memList[i] = new MemBlock(data, true);
            return;
          }
        }
      }

      data = new T[size];
      lock (memList)
      {
        blockIndex = memList.Count;
        memList.Add(new MemBlock(data, true));
      }
    }

    /// <summary>
    /// gibt die Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      if (data == null) return;
      var memList = memBlocks[sizeId];
      lock (memList)
      {
        memList[blockIndex] = new MemBlock(memList[blockIndex].data, false);
        data = null;
      }
    }

    /// <summary>
    /// gibt den internen reservierten Speicherblock zurück (z.B. für schnellere Abfragen, ist oft größer als <see cref="Count"/>
    /// </summary>
    public T[] RawData
    {
      get
      {
        return data;
      }
    }

    /// <summary>
    /// kann die Größe des Arrays verkleinern bzw. vergrößern, wenn der Platz reichen sollte
    /// </summary>
    /// <param name="newCount">neue Größe des Arrays</param>
    public void Resize(int newCount)
    {
      if (data == null) throw new ObjectDisposedException("data");
      if (newCount < 0) throw new ArgumentOutOfRangeException("newCount");
      if (newCount > data.Length) throw new OutOfMemoryException();
      count = newCount;
    }

    #region # // --- IList ---
    /// <summary>
    /// Destructor
    /// </summary>
    ~MemArray()
    {
      Dispose();
    }

    /// <summary>
    /// gibt eine Aufzählung der Elemente zurück
    /// </summary>
    /// <returns>Enumerator der Elemente</returns>
    public IEnumerator<T> GetEnumerator()
    {
      for (int i = 0; i < count; i++)
      {
        yield return data[i];
      }
    }

    /// <summary>
    /// gibt eine Aufzählung der Elemente zurück
    /// </summary>
    /// <returns>Enumerator der Elemente</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// leert die Inhalte
    /// </summary>
    public void Clear()
    {
      Array.Clear(data, 0, count);
    }

    /// <summary>
    /// prüft, ob ein bestimmtes Element enthalten ist
    /// </summary>
    /// <param name="item">Element, welches gesucht werden soll</param>
    /// <returns>true, wenn das Element gefunden wurde</returns>
    public bool Contains(T item)
    {
      var equalityComparer = EqualityComparer<T>.Default;
      for (int i = 0; i < count; i++)
      {
        if (equalityComparer.Equals(data[i], item)) return true;
      }
      return false;
    }

    /// <summary>
    /// kopiert alle Elemente in ein bestimmtes Array
    /// </summary>
    /// <param name="array">Array, wohin die Elemente kopiert werden sollen</param>
    /// <param name="arrayIndex">Startposition innerhalb des Ausgabe-Arrays</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
      if (array == null) throw new ArgumentNullException("array");
      if (arrayIndex < 0 || count > arrayIndex + array.Length) throw new ArgumentOutOfRangeException();
      Array.Copy(data, 0, array, arrayIndex, count);
    }

    /// <summary>
    /// gibt die Anzahl der Elemente zurück
    /// </summary>
    public int Count { get { return count; } }

    /// <summary>
    /// gibt die Anzahl der Elemente zurück
    /// </summary>
    public int Length { get { return count; } }

    /// <summary>
    /// sucht nach einem bestimmten Element und gibt deren Position zurück (oder -1, wenn nicht gefunden)
    /// </summary>
    /// <param name="item">Element, welches gesucht werden soll</param>
    /// <returns>Position des Elementes (oder -1, wenn nicht gefunden)</returns>
    public int IndexOf(T item)
    {
      return Array.IndexOf(data, item, 0, count);
    }

    /// <summary>
    /// fragt ein Element ab oder setzt dieses
    /// </summary>
    /// <param name="index">Index innerhalb des Arrays</param>
    /// <returns>abgefragtes Element</returns>
    public T this[int index]
    {
      get
      {
        if ((uint)index > (uint)count) throw new IndexOutOfRangeException();
        return data[index];
      }
      set
      {
        if ((uint)index > (uint)count) throw new IndexOutOfRangeException();
        data[index] = value;
      }
    }
    #endregion

    #region # // --- BinarySearchSingle ---
    /// <summary>
    /// führt eine binäre Suche im sortierten Array durch und gibt einen einzelnen passenden Datensatz zurück
    /// </summary>
    /// <typeparam name="T">Typ im Array</typeparam>
    /// <param name="compareMethod">Vergleichsmethode zum Finden des Datensatzes (muss mit der Sortierung kompatibel sein)</param>
    /// <returns>gefundener Datensatz oder null, wenn nicht gefunden</returns>
    public T BinarySearchSingle(Func<T, long> compareMethod)
    {
      int start = 0;
      int end = count;
      if (end == 0) return default(T);
      var array = data;
      do
      {
        var center = (start + end) >> 1;
        if (compareMethod(array[center]) > 0) end = center; else start = center;
      } while (end - start > 1);

      if (compareMethod(array[start]) == 0) return array[start];
      if (start > 0 && compareMethod(array[start - 1]) == 0) return array[start];
      return default(T);
    }
    #endregion

    #region # // --- not supported ---
    public bool IsReadOnly { get { return false; } }
    public void Add(T item)
    {
      throw new NotSupportedException();
    }
    public bool Remove(T item)
    {
      throw new NotSupportedException();
    }
    public void Insert(int index, T item)
    {
      throw new NotSupportedException();
    }
    public void RemoveAt(int index)
    {
      throw new NotSupportedException();
    }
    #endregion
  }
}
