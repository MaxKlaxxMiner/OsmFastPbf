using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using OsmFastPbf.zlibTuned.FastInflater;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UselessBinaryOperation

namespace OsmFastPbf.Helper
{
  public static class ProtoBuf
  {
    /// <summary>
    /// liest einen Int32-Wert mit fester Größe ein (4 Bytes)
    /// </summary>
    /// <param name="buf">Buffer, woraus der Wert gelesen werden soll</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <param name="val">Wert, welcher ausgelesen wurde</param>
    /// <returns>Anzahl der gelesenen Bytes</returns>
    public static int ReadInt32Fix(byte[] buf, int ofs, out int val)
    {
      val = (int)((uint)buf[ofs] << 24 | (uint)buf[ofs + 1] << 16 | (uint)buf[ofs + 2] << 8 | buf[ofs + 3]);
      return sizeof(int);
    }

    /// <summary>
    /// liest einen gepackten unsignierten Integer-Wert ein
    /// </summary>
    /// <param name="buf">Buffer, woraus der Wert gelesen werden soll</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <param name="val">Wert, welcher ausgelesen wurde</param>
    /// <returns>Anzahl der gelesenen Bytes</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadVarInt(byte[] buf, int ofs, out ulong val)
    {
      int len = 0;
      val = buf[ofs + len++];
      if (val > 127)
      {
        val &= 127;
        for (int bit = 7; ; bit += 7)
        {
          byte b = buf[ofs + len++];
          val |= (ulong)(b & 127) << bit;
          if (b <= 127) break;
        }
      }
      return len;
    }

    /// <summary>
    /// schreibt einen gepackten unsignierten Integer-Wert in den Buffer und gibt die Anzahl der geschriebenen Bytes zurück
    /// </summary>
    /// <param name="buf">Buffer, wohin der Wert geschrieben werden soll</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <param name="val">Wert, welcher geschrieben werden soll</param>
    /// <returns>Anzahl der geschriebenen Bytes</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarInt(byte[] buf, int ofs, ulong val)
    {
      int len = 0;
      for (; ; )
      {
        buf[ofs + len++] = (byte)(val & 127);
        if (val < 128) break;
        buf[ofs + len - 1] |= 128;
        val >>= 7;
      }
      return len;
    }

    /// <summary>
    /// liest einen gepackten unsignierten Integer-Wert ein und gibt diesen zurück
    /// </summary>
    /// <param name="buf">Buffer, woraus der Wert gelesen werden soll</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <returns>fertig gelesener Wert</returns>
    public static ulong PeekVarInt(byte[] buf, int ofs)
    {
      ulong val;
      ReadVarInt(buf, ofs, out val);
      return val;
    }

    /// <summary>
    /// wandelt eine ZigZag-kodierte Zahl um (uint -> int)
    /// </summary>
    /// <param name="val">Wert, welcher umgewandelt werden soll</param>
    /// <returns>fertiges Ergebnis</returns>
    public static int SignedInt32(uint val)
    {
      return (int)(val >> 1) ^ -(int)(val & 1);
    }

    /// <summary>
    /// wandelt eine ZigZag-kodierte Zahl um (ulong -> long)
    /// </summary>
    /// <param name="val">Wert, welcher umgewandelt werden soll</param>
    /// <returns>fertiges Ergebnis</returns>
    public static long SignedInt64(ulong val)
    {
      return (long)(val >> 1) ^ -(long)(val & 1);
    }

    /// <summary>
    /// wandelt eine ZigZag-kodierte Zahl um (int -> uint)
    /// </summary>
    /// <param name="val">Wert, welcher umgewandelt werden soll</param>
    /// <returns>fertiges Ergebnis</returns>
    public static uint UnsignedInt32(int val)
    {
      return (uint)(val << 1 ^ val >> 31);
    }

    /// <summary>
    /// wandelt eine ZigZag-kodierte Zahl um (long -> ulong)
    /// </summary>
    /// <param name="val">Wert, welcher umgewandelt werden soll</param>
    /// <returns>fertiges Ergebnis</returns>
    public static ulong UnsignedInt64(long val)
    {
      return (ulong)(val << 1 ^ val >> 63);
    }

    #region # // --- Read Packet ---
    public static int SkipStringTable(byte[] buf, int ofs)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      while (len < endLen)
      {
        if (buf[ofs + len++] != (1 << 3 | 2)) throw new PbfParseException();
        len += SkipString(buf, ofs + len);
      }
      if (len != endLen) throw new PbfParseException();
      return len;
    }

    public static int DecodeStringTable(byte[] buf, int ofs, out string[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var stringTable = new List<string>();
      while (len < endLen)
      {
        string tmp;
        if (buf[ofs + len++] != (1 << 3 | 2)) throw new PbfParseException();
        len += ReadString(buf, ofs + len, out tmp);
        stringTable.Add(tmp);
      }
      if (len != endLen) throw new PbfParseException();
      val = stringTable.ToArray();
      return len;
    }

    public static int DecodePackedInt32(byte[] buf, int ofs, out int[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new int[dataLen];
      int resultLen = 0;

      while (len < endLen)
      {
        ulong tmp;
        len += ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen++] = (int)(uint)tmp;
      }

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    public static int DecodePackedInt32(byte[] buf, int ofs, out int[] val, int itemCount)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new int[itemCount];
      for (int i = 0; i < result.Length; i++)
      {
        ulong tmp;
        len += ReadVarInt(buf, ofs + len, out tmp);
        result[i] = (int)(uint)tmp;
      }
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    public static int DecodePackedUInt32(byte[] buf, int ofs, out uint[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new uint[dataLen];
      int resultLen = 0;

      while (len < endLen)
      {
        ulong tmp;
        len += ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen++] = (uint)tmp;
      }

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    public static int DecodePackedSInt32Delta(byte[] buf, int ofs, out int[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new int[dataLen];

      ulong tmp;
      len += ReadVarInt(buf, ofs + len, out tmp);
      result[0] = SignedInt32((uint)tmp);

      int resultLen;
      for (resultLen = 1; resultLen < result.Length && len < endLen; resultLen++)
      {
        len += ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen] = SignedInt32((uint)tmp) + result[resultLen - 1];
      }

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    public static int DecodePackedSInt32Delta(byte[] buf, int ofs, out int[] val, int itemCount)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new int[itemCount];

      ulong tmp;
      len += ReadVarInt(buf, ofs + len, out tmp);
      result[0] = SignedInt32((uint)tmp);

      for (int i = 1; i < result.Length && len < endLen; i++)
      {
        len += ReadVarInt(buf, ofs + len, out tmp);
        result[i] = SignedInt32((uint)tmp) + result[i - 1];
      }

      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    public static int DecodePackedSInt64Delta(byte[] buf, int ofs, out long[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new long[dataLen];

      ulong tmp;
      len += ReadVarInt(buf, ofs + len, out tmp);
      result[0] = SignedInt64(tmp);

      int resultLen;
      for (resultLen = 1; resultLen < result.Length && len < endLen; resultLen++)
      {
        len += ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen] = SignedInt64(tmp) + result[resultLen - 1];
      }

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    public static int DecodePackedSInt64Delta(byte[] buf, int ofs, out long[] val, int itemCount)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new long[itemCount];

      ulong tmp;
      len += ReadVarInt(buf, ofs + len, out tmp);
      result[0] = SignedInt64(tmp);

      for (int i = 1; i < result.Length && len < endLen; i++)
      {
        len += ReadVarInt(buf, ofs + len, out tmp);
        result[i] = SignedInt64(tmp) + result[i - 1];
      }

      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    #endregion

    /// <summary>
    /// überspringt eine Zeichenfolge
    /// </summary>
    /// <param name="buf">Buffer, woraus der Wert gelesen werden soll</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <returns>Anzahl der gelesenen Bytes</returns>
    public static int SkipString(byte[] buf, int ofs)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      len += (int)dataLen;
      return len;
    }

    /// <summary>
    /// liest eine Zeichenfolge ein
    /// </summary>
    /// <param name="buf">Buffer, woraus der Wert gelesen werden soll</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <param name="val">Wert, welcher ausgelesen wurde</param>
    /// <returns>Anzahl der gelesenen Bytes</returns>
    public static int ReadString(byte[] buf, int ofs, out string val)
    {
      int len = 0;
      ulong dataLen;
      len += ReadVarInt(buf, ofs + len, out dataLen);
      val = Encoding.UTF8.GetString(buf, ofs + len, (int)(uint)dataLen);
      len += (int)dataLen;
      return len;
    }

    /// <summary>
    /// schreibt eine Zeichenfolge in den Buffer und gibt die Anzahl der geschriebenen Bytes zurück
    /// </summary>
    /// <param name="buf">Buffer, wohin die Zeichenfolge geschrieben werden soll</param>
    /// <param name="ofs">Startposition innerhalbb des Buffers</param>
    /// <param name="val">Wert, welcher geschrieben werden soll</param>
    /// <returns>Anzahl der geschriebenen Bytes</returns>
    public static int WriteString(byte[] buf, int ofs, string val)
    {
      int len = Encoding.UTF8.GetBytes(val, 0, val.Length, buf, ofs + 1);

      if (len < 128)
      {
        buf[ofs] = (byte)(uint)len;
        len++;
      }
      else
      {
        len = WriteVarInt(buf, ofs, (uint)len);
        len += Encoding.UTF8.GetBytes(val, 0, val.Length, buf, ofs + len);
      }

      return len;
    }

    /// <summary>
    /// entpackt einen Buffer-Block mit dem Inflater
    /// </summary>
    /// <param name="buf">Buffer, welcher die komprimierten Daten enthält</param>
    /// <param name="bufOfs">Startposition innerhalb des Buffers</param>
    /// <param name="bufLen">Anzahl der zu lesenden Bytes im Buffer</param>
    /// <param name="output">Ausgabe-Buffer für die entpackten Daten</param>
    /// <param name="outputOfs">Startposition innerhalb des Ausgabe-Buffers</param>
    /// <returns>Länge der entpackten Daten</returns>
    public static int FastInflate(byte[] buf, int bufOfs, int bufLen, byte[] output, int outputOfs)
    {
      var inf = new Inflater();
      inf.SetInput(buf, bufOfs + 2, bufLen - 2); // Buffer setzen und Header überspringen (2 Bytes)
      return inf.Inflate(output, outputOfs, output.Length - outputOfs);

      // --- .Net default Deflater (takes more than 40% decompressing time) ---
      //using (var src = new MemoryStream(buf, bufOfs + 2, bufLen - 2, false))
      //using (var zlib = new DeflateStream(src, CompressionMode.Decompress))
      //using (var dst = new MemoryStream(output, outputOfs, output.Length - outputOfs))
      //{
      //  zlib.CopyTo(dst);
      //  return (int)dst.Position - outputOfs;
      //}
    }
  }
}
