using System;
using System.Collections;
using System.Collections.Generic;
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace OsmFastPbf
{
  /// <summary>
  /// Klasse zum schnellen reservieren von Arrays
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class MemArray<T> : IList<T>, IDisposable where T : struct
  {
    /// <summary>
    /// Array mit den Rohdaten
    /// </summary>
    T[] data;
    /// <summary>
    /// Anzahl der benutzbaren Elemente
    /// </summary>
    readonly int count;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="count">Anzahl der zu reservierenden Elemente</param>
    /// <param name="clear">gibt an, ob das Array geleert werden soll (default: true)</param>
    public MemArray(int count, bool clear = true)
    {
      data = new T[count];
      this.count = count;
    }

    /// <summary>
    /// gibt die Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      data = null;
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
    /// Destructor
    /// </summary>
    ~MemArray()
    {
      Dispose();
    }

    #region # // --- IList ---
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
