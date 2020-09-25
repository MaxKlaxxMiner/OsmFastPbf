using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OsmFastPbf.zlibTuned.FastInflater
{
  /// <summary>
  /// Klasse für die Bitweise-Verarbeitung von Daten
  /// </summary>
  public sealed unsafe class StreamManipulator
  {
    byte[] window;
    int window_start;
    int window_end;
    uint buffer;
    int bits_in_buffer;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PeekBits(int n)
    {
      if (bits_in_buffer < n)
      {
        if (window_start == window_end) return -1;
        buffer |= (uint)((window[window_start++] | window[window_start++] << 8) << bits_in_buffer);
        bits_in_buffer += 16;
      }
      return (int)(buffer & ((1 << n) - 1));
    }
    public void DropBits(int n)
    {
      buffer >>= n;
      bits_in_buffer -= n;
    }

    public int AvailableBits
    {
      get
      {
        return bits_in_buffer;
      }
    }
    public int AvailableBytes
    {
      get
      {
        return window_end - window_start + (bits_in_buffer >> 3);
      }
    }
    public void SkipToByteBoundary()
    {
      buffer >>= (bits_in_buffer & 7);
      bits_in_buffer &= ~7;
    }
    public bool IsNeedingInput
    {
      get
      {
        return window_start == window_end;
      }
    }
    public int CopyBytes(byte* output, int offset, int length)
    {
      if ((bits_in_buffer & 7) != 0) throw new InvalidOperationException("Bit buffer is not aligned!");
      int count = 0;
      while (bits_in_buffer > 0 && length > 0)
      {
        output[offset++] = (byte)buffer;
        buffer >>= 8;
        bits_in_buffer -= 8;
        length--;
        count++;
      }
      if (length == 0) return count;
      int avail = window_end - window_start;
      if (length > avail) length = avail;
      Marshal.Copy(window, window_start, (IntPtr)(output + offset), length);
      window_start += length;
      if (((window_start - window_end) & 1) != 0)
      {
        buffer = window[window_start++];
        bits_in_buffer = 8;
      }
      return count + length;
    }

    public void SetInput(byte[] buf, int off, int len)
    {
      if (window_start < window_end) throw new InvalidOperationException("Old input was not completely processed");
      int end = off + len;
      if (0 > off || off > end || end > buf.Length) throw new ArgumentOutOfRangeException();
      if ((len & 1) != 0)
      {
        buffer |= (uint)(buf[off++] << bits_in_buffer);
        bits_in_buffer += 8;
      }
      window = buf;
      window_start = off;
      window_end = end;
    }
  }
}
