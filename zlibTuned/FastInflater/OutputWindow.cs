using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OsmFastPbf.zlibTuned.FastInflater
{
  /// <summary>
  /// Fenstersystem für die Verarbeitung von Deflate-Daten (im 32k-Suchfenster)
  /// </summary>
  public unsafe sealed class OutputWindow
  {
    static readonly int WINDOW_SIZE = 1 << 15;
    static readonly int WINDOW_MASK = WINDOW_SIZE - 1;
    readonly byte* window = (byte*)Marshal.AllocHGlobal(WINDOW_SIZE);
    ~OutputWindow() { Marshal.FreeHGlobal((IntPtr)window); }
    int windowEnd;
    int windowFilled;
    public void Write(int abyte)
    {
      if (windowFilled++ == WINDOW_SIZE) throw new InvalidOperationException("Window full");
      window[windowEnd++] = (byte)abyte;
      windowEnd &= WINDOW_MASK;
    }
    void SlowRepeat(int repStart, int len)
    {
      int _windowEnd = windowEnd;
      int _repStart = repStart;
      var _window = window;
      while (len-- > 0)
      {
        _window[_windowEnd & WINDOW_MASK] = _window[_repStart & WINDOW_MASK];
        _windowEnd++;
        _repStart++;
      }
      windowEnd = _windowEnd & WINDOW_MASK;
    }

    /// <summary>
    /// kopiert Bytes von einer Speicheradresse auf eine andere Speicheradresse
    /// </summary>
    /// <param name="quelle">Adresse auf die Quelldaten</param>
    /// <param name="ziel">Adresse auf die Zieldaten</param>
    /// <param name="bytes">Anzahl der Bytes, welche kopiert werden sollen</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyBytes(byte* quelle, byte* ziel, int bytes)
    {
      // --- 64-Bit Modus (als longs kopieren) ---
      int bis = bytes >> 3;
      var pQuelle = (long*)quelle;
      var pZiel = (long*)ziel;
      for (int i = 0; i < bis; i++)
      {
        pZiel[i] = pQuelle[i];
      }
      int pos = bis << 3;

      // --- die restlichen Bytes kopieren ---
      for (; pos < bytes; pos++)
      {
        ziel[pos] = quelle[pos];
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Repeat(int len, int dist)
    {
      if ((windowFilled += len) > WINDOW_SIZE) throw new InvalidOperationException("Window full");
      int rep_start = (windowEnd - dist) & WINDOW_MASK;
      int border = WINDOW_SIZE - len;
      if (len <= dist && rep_start <= border && windowEnd < border)
      {
        CopyBytes(window + rep_start, window + windowEnd, len);
        windowEnd += len;
      }
      else SlowRepeat(rep_start, len);
    }
    public int CopyStored(StreamManipulator input, int len)
    {
      len = Math.Min(Math.Min(len, WINDOW_SIZE - windowFilled), input.AvailableBytes);
      int copied;
      int tailLen = WINDOW_SIZE - windowEnd;
      if (len > tailLen)
      {
        copied = input.CopyBytes(window, windowEnd, tailLen);
        if (copied == tailLen) copied += input.CopyBytes(window, 0, len - tailLen);
      }
      else copied = input.CopyBytes(window, windowEnd, len);
      windowEnd = (windowEnd + copied) & WINDOW_MASK;
      windowFilled += copied;
      return copied;
    }
    public int GetFreeSpace()
    {
      return WINDOW_SIZE - windowFilled;
    }
    public int GetAvailable()
    {
      return windowFilled;
    }
    public int CopyOutput(byte[] output, int offset, int len)
    {
      int copy_end = windowEnd;
      if (len > windowFilled) len = windowFilled; else copy_end = (windowEnd - windowFilled + len) & WINDOW_MASK;
      int copied = len;
      int tailLen = len - copy_end;
      if (tailLen > 0)
      {
        Marshal.Copy((IntPtr)(window + (WINDOW_SIZE - tailLen)), output, offset, tailLen);
        offset += tailLen;
        len = copy_end;
      }
      Marshal.Copy((IntPtr)(window + (copy_end - len)), output, offset, len);
      windowFilled -= copied;
      if (windowFilled < 0) throw new InvalidOperationException();
      return copied;
    }
  }
}
