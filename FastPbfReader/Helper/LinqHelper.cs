using System;
using System.Collections.Generic;
using System.Threading;
// ReSharper disable UnusedMember.Global

namespace OsmFastPbf.Helper
{
  public static class LinqHelper
  {
    public static IEnumerable<TOut> SelectParallelEnumerable<TIn, TOut>(this IEnumerable<TIn> elemente, Func<TIn, TOut> methode, int maxThreads = 0, ThreadPriority priority = ThreadPriority.Normal)
    {
      if (maxThreads < 1) maxThreads = Environment.ProcessorCount;

      int activeThreads = maxThreads;

      var locker = new object();
      var tasks = elemente.GetEnumerator();
      var resultBuffer = new List<TOut>();

      for (int th = 0; th < maxThreads; th++)
      {
        new Thread(() =>
        {
          for (; ; )
          {
            TIn task;
            lock (tasks)
            {
              if (!tasks.MoveNext()) break;
              task = tasks.Current;
            }
            var result = methode(task);
            lock (resultBuffer)
            {
              resultBuffer.Add(result);
            }
          }
          lock (locker)
          {
            activeThreads--;
          }
        }) { Priority = priority }.Start();
      }

      for (; ; )
      {
        TOut[] output = null;
        lock (resultBuffer)
        {
          if (resultBuffer.Count > 0)
          {
            output = resultBuffer.ToArray();
            resultBuffer.Clear();
          }
          else
          {
            lock (locker)
            {
              if (activeThreads == 0) break;
            }
          }
        }
        if (output != null)
        {
          foreach (var result in output)
          {
            yield return result;
          }
        }
        Thread.Sleep(1);
      }
    }

    /// <summary>
    /// führt eine binäre Suche im sortierten Array durch und gibt alle passenden Datensätze zurück
    /// </summary>
    /// <typeparam name="T">Typ im Array</typeparam>
    /// <param name="array">Array mit den vorsortierten Datensätzen</param>
    /// <param name="compareMethod">Vergleichsmethode zum Finden der Datensätze (muss mit der Sortierung kompatibel sein)</param>
    /// <returns>IEnumerable der gefundenen Datensätze</returns>
    public static IEnumerable<T> BinarySearch<T>(this T[] array, Func<T, int> compareMethod)
    {
      int start = 0;
      int end = array.Length;
      if (end == 0) yield break;
      do
      {
        var center = (start + end) >> 1;
        if (compareMethod(array[center]) > 0) end = center; else start = center;
      } while (end - start > 1);

      // Anfang suchen
      while (start > 0 && compareMethod(array[start - 1]) == 0) start--;

      // alle zutreffenden Datensätze zurück geben
      while (start < array.Length && compareMethod(array[start]) == 0) yield return array[start++];
    }

    /// <summary>
    /// führt eine binäre Suche im sortierten Array durch und gibt einen einzelnen passenden Datensatz zurück
    /// </summary>
    /// <typeparam name="T">Typ im Array</typeparam>
    /// <param name="array">Array mit den vorsortierten Datensätzen</param>
    /// <param name="compareMethod">Vergleichsmethode zum Finden des Datensatzes (muss mit der Sortierung kompatibel sein)</param>
    /// <returns>gefundener Datensatz oder null, wenn nicht gefunden</returns>
    public static T BinarySearchSingle<T>(this T[] array, Func<T, int> compareMethod)
    {
      int start = 0;
      int end = array.Length;
      if (end == 0) return default(T);
      do
      {
        var center = (start + end) >> 1;
        if (compareMethod(array[center]) > 0) end = center; else start = center;
      } while (end - start > 1);

      if (compareMethod(array[start]) == 0) return array[start];
      if (start > 0 && compareMethod(array[start - 1]) == 0) return array[start];
      return default(T);
    }

    /// <summary>
    /// führt eine binäre Suche im sortierten Array durch und gibt einen einzelnen passenden Datensatz zurück
    /// </summary>
    /// <typeparam name="T">Typ im Array</typeparam>
    /// <param name="array">Array mit den vorsortierten Datensätzen</param>
    /// <param name="compareMethod">Vergleichsmethode zum Finden des Datensatzes (muss mit der Sortierung kompatibel sein)</param>
    /// <returns>gefundener Datensatz oder null, wenn nicht gefunden</returns>
    public static T BinarySearchSingle<T>(this T[] array, Func<T, long> compareMethod)
    {
      int start = 0;
      int end = array.Length;
      if (end == 0) return default(T);
      do
      {
        var center = (start + end) >> 1;
        if (compareMethod(array[center]) > 0) end = center; else start = center;
      } while (end - start > 1);

      if (compareMethod(array[start]) == 0) return array[start];
      if (start > 0 && compareMethod(array[start - 1]) == 0) return array[start];
      return default(T);
    }

    /// <summary>
    /// führt eine binäre Suche im sortierten Array durch und gibt den Index des einzelnen passenden Datensatzes zurück
    /// </summary>
    /// <typeparam name="T">Typ im Array</typeparam>
    /// <param name="array">Array mit den vorsortierten Datensätzen</param>
    /// <param name="compareMethod">Vergleichsmethode zum Finden des Datensatzes (muss mit der Sortierung kompatibel sein)</param>
    /// <returns>gefundener Index auf den Datensatz oder -1, wenn nicht gefunden</returns>
    public static int BinarySearchSingleIndex<T>(this T[] array, Func<T, long> compareMethod)
    {
      int start = 0;
      int end = array.Length;
      if (end == 0) return -1;
      do
      {
        var center = (start + end) >> 1;
        if (compareMethod(array[center]) > 0) end = center; else start = center;
      } while (end - start > 1);

      if (compareMethod(array[start]) == 0) return start;
      if (start > 0 && compareMethod(array[start - 1]) == 0) return start;
      return -1;
    }

    public static T[] ToArray<T>(this IEnumerable<T> values, int count)
    {
      var output = new T[count];
      int ofs = 0;
      foreach (var val in values)
      {
        output[ofs++] = val;
      }
      if (ofs != output.Length) throw new IndexOutOfRangeException();
      return output;
    }
  }
}
