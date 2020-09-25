
#region # using *.*

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endregion

namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Klasse zum verarbeiten eines Daten-Buffers
  /// </summary>
  internal sealed unsafe class PendingBuffer
  {
    readonly byte* buf;
    int start;
    int end;
    ulong bits;
    int bitCount;

    public PendingBuffer(int bufsize)
    {
      buf = (byte*)Marshal.AllocHGlobal(bufsize);
    }

    ~PendingBuffer()
    {
      Marshal.FreeHGlobal((IntPtr)buf);
    }

    public void Reset()
    {
      start = end = bitCount = 0;
    }

    public void WriteShort(int s)
    {
      buf[end++] = (byte)s;
      buf[end++] = (byte)(s >> 8);
    }

    public void WriteBlock(byte* block, int offset, int len)
    {
      var src = block + offset;
      var dest = buf + end;
      for (int i = 0; i < len; i++) dest[i] = src[i];
      end += len;
    }

    public int BitCount
    {
      get
      {
        return bitCount;
      }
    }

    public void AlignToByte()
    {
      while (bitCount >= 8)
      {
        buf[end++] = (byte)bits;
        bitCount -= 8;
        bits >>= 8;
      }
      if (bitCount <= 0) return;

      buf[end++] = (byte)bits;
      bits = 0;
      bitCount = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void WriteBitsInternal()
    {
      *(ulong*)(buf + end) = bits;
      bits >>= 48;
      bitCount -= 48;
      end += 6;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBits(int b, int count)
    {
      bits |= (ulong)b << bitCount;
      bitCount += count;
      if (bitCount >= 48) WriteBitsInternal();
    }

    public void WriteShortMsb(int s)
    {
      buf[end++] = (byte)(s >> 8);
      buf[end++] = (byte)s;
    }

    public bool IsFlushed
    {
      get
      {
        return end == 0;
      }
    }

    public int Flush(byte[] output, int offset, int length)
    {
      while (bitCount >= 8)
      {
        buf[end++] = (byte)bits;
        bits >>= 8;
        bitCount -= 8;
      }

      if (length > end - start)
      {
        length = end - start;
        Marshal.Copy((IntPtr)(buf + start), output, offset, length);
        start = 0;
        end = 0;
      }
      else
      {
        Marshal.Copy((IntPtr)(buf + start), output, offset, length);
        start += length;
      }
      return length;
    }
  }
}
