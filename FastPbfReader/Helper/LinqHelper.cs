using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;

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
  }
}
