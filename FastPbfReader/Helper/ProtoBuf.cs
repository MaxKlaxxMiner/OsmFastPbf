﻿using System.Runtime.CompilerServices;
using System.Text;
using OsmFastPbf.zlibTuned.FastInflater;
// ReSharper disable MemberCanBePrivate.Global

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
    /// liest einen gepackten unsignerten Integer-Wert ein
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
          val |= (uint)(b & 127) << bit;
          if (b <= 127) break;
        }
      }
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
    }
  }
}