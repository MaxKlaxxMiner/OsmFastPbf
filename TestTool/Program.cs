using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmFastPbf;
using OsmFastPbf.Helper;

namespace TestTool
{
  partial class Program
  {
    static int DecodeStringTable(byte[] buf, int ofs)
    {
      int len = 0;
      //byte t = buf[ofs + len++];
      //if (t != (2 | 1 << 3)) throw new PbfParseException(); // Length-delimited (2), string (1)
      //ulong dataLen;
      //len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      //int endLen = len + (int)dataLen;
      //var stringTable = new List<string>();
      //for (; len < endLen; )
      //{
      //  string tmp;
      //  len += ProtoBuf.ReadString(buf, ofs + len, out tmp);
      //  stringTable.Add(tmp);
      //}
      //if (len != endLen) throw new PbfParseException();
      return len;
    }

    static int DecodePrimitiveGroup(byte[] buf, int ofs)
    {
      /*****
       * message PrimitiveGroup
       * {
       *   repeated Node nodes = 1;
       *   optional DenseNodes dense = 2;
       *   repeated Way ways = 3;
       *   repeated Relation relations = 4;
       *   repeated ChangeSet changesets = 5;
       * }
       *****/

      int len = 0;

      // --- repeated Node nodes = 1; ---
      if (buf[ofs + len] == (1 | 2 << 3))
      {
        throw new NotImplementedException();
      }

      // --- optional DenseNodes dense = 2; ---
      if (buf[ofs + len] == (2 | 2 << 3))
      {
        byte t = buf[ofs + len++];
        if (t != (2 | 2 << 3)) throw new PbfParseException(); // Length
        ulong nodesLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out nodesLen);
        int endNodes = len + (int)nodesLen;
        for (; len < endNodes; )
        {
          //len += DecodeNode(buf, ofs + len);
        }
      }

      //todo: repeated Way ways = 3;

      //todo: repeated Relation relations = 4;

      //todo: repeated ChangeSet changesets = 5;

      return len;
    }

    static int DecodePrimitiveBlock(byte[] buf, int ofs)
    {
      /*****
       * message PrimitiveBlock
       * {
       *   required StringTable stringtable = 1;
       *   repeated PrimitiveGroup primitivegroup = 2;
       *   
       *   // Einheit der Auflösung: Nanograd, zur Speicherung Koordinaten in diesem Block
       *   optional int32 granularity = 17 [default=100];
       *   
       *   // Offset-Wert zwischen der  Koordinaten-Ausgabe den Koordinaten und dem Auflösungsraster - in Nanograd.
       *   optional int64 lat_offset = 19 [default=0];
       *   optional int64 lon_offset = 20 [default=0];
       *   
       *   // Genauigkeit des Zeitpunkts, üblicherweise seit 1970, in Millisekunden.
       *   optional int32 date_granularity = 18 [default=1000];
       *   
       *   // Vorgeschlagene Erweiterung:
       *   //optional BBox bbox = XX;
       * }    
       *****/

      int len = 0;

      // --- required StringTable stringtable = 1; ---
      len += DecodeStringTable(buf, ofs + len);

      // --- repeated PrimitiveGroup primitivegroup = 2; ---
      if (buf[ofs + len] == (2 | 2 << 3))
      {
        ulong dataLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + ++len, out dataLen);
        int endLen = len + (int)dataLen;
        for (; len < endLen; len++)
        {
          len += DecodePrimitiveGroup(buf, ofs + len);
        }
      }

      //todo: optional int32 granularity = 17 [default=100];

      //todo: optional int64 lat_offset = 19 [default=0];

      //todo: optional int64 lon_offset = 20 [default=0];

      //todo: optional int32 date_granularity = 18 [default=1000];

      return len;
    }

    static void Main(string[] args)
    {
      //BufferTest();

      #region # // --- old ---
      //static int ReadEmbedded(byte[] buf, int ofs, out byte[] val)
      //{
      //  int len = 0;
      //  byte t = buf[ofs + len++];
      //  if (t != (2 | 3 << 3)) throw new PbfParseException(); // Length-delimited (2), embedded message (3)
      //  int size = buf[ofs + len++];
      //  if (size > 127)
      //  {
      //    size &= 127;
      //    for (int bit = 7; ; bit += 7)
      //    {
      //      byte b = buf[ofs + len++];
      //      size |= (b & 127) << bit;
      //      if (b <= 127) break;
      //    }
      //  }
      //  val = new byte[size];
      //  Array.Copy(buf, ofs + len, val, 0, size); len += size;
      //  return len;
      //}
      //static int ReadEmbeddedCompressed(byte[] buf, int ofs, out byte[] val)
      //{
      //  int len = ReadEmbedded(buf, ofs, out val);
      //  using (var zlib = new InflaterInputStream(new RamStream(val))) val = zlib.ReadAllBytes(true);
      //  return len;
      //}

      //static int ReadPacket(byte[] buf, int ofs, out List<ulong> val)
      //{
      //  int len = 0;
      //  byte t = buf[ofs + len++];
      //  if (t != (2 | 4 << 3) && t != (2 | 1 << 3) && t != (2 | 5 << 3) && t != (2 | 16 << 3)) throw new PbfParseException(); // Length-delimited (2), *
      //  int size = buf[ofs + len++];
      //  if (size > 127)
      //  {
      //    size &= 127;
      //    for (int bit = 7; ; bit += 7)
      //    {
      //      byte b = buf[ofs + len++];
      //      size |= (b & 127) << bit;
      //      if (b <= 127) break;
      //    }
      //  }
      //  int end = len + size;
      //  var vals = new List<ulong>();
      //  while (len < end)
      //  {
      //    ulong v = buf[ofs + len++];
      //    if (v > 127)
      //    {
      //      v &= 127;
      //      for (int bit = 7; ; bit += 7)
      //      {
      //        byte b = buf[ofs + len++];
      //        v |= (ulong)(b & 127) << bit;
      //        if (b <= 127) break;
      //      }
      //    }
      //    vals.Add(v);
      //  }
      //  val = vals;
      //  return len;
      //}
      //static int ReadPacket(byte[] buf, int ofs, out string val)
      //{
      //  List<ulong> tmp;
      //  int len = ReadPacket(buf, ofs, out tmp);
      //  val = new string(tmp.SelectArray(c => checked((char)c)));
      //  return len;
      //}
      //static int ReadPacket(byte[] buf, int ofs, out ulong[] val)
      //{
      //  List<ulong> tmp;
      //  int len = ReadPacket(buf, ofs, out tmp);
      //  val = tmp.ToArray();
      //  return len;
      //}
      //static int ReadPacket(byte[] buf, int ofs, string val)
      //{
      //  string tmp;
      //  int len = ReadPacket(buf, ofs, out tmp);
      //  if (tmp != val) throw new PbfParseException();
      //  return len;
      //}

      //public struct OSMHeader
      //{
      //  public OSMHeader(byte[] buf)
      //  {
      //    using (var zlib = new InflaterInputStream(new RamStream(buf))) buf = zlib.ReadAllBytes(true);

      //    int pos = 0;
      //    ulong[] bbox;
      //    pos += ReadPacket(buf, pos, out bbox);
      //    pos += ReadPacket(buf, pos, "OsmSchema-V0.6");
      //    pos += ReadPacket(buf, pos, "DenseNodes");
      //    pos += ReadPacket(buf, pos, "Has_Metadata");
      //    pos += ReadPacket(buf, pos, "Sort.Type_then_ID");
      //    pos += ReadPacket(buf, pos, "\x14");
      //  }
      //}
      #endregion

      string path = "planet-latest.osm.pbf";
      for (int i = 0; i < 32 && !File.Exists(path); i++) path = "../" + path;
      if (!File.Exists(path)) throw new FileNotFoundException(path.TrimStart('.', '/'));
      using (var test = new FastPbfReader(path))
      {
        #region # // --- Index einlesen ---
        test.RandomBuffering = true;

        long pos = 0;
        var buf = test.buffer;
        int tim = 0;

        var blocks = new List<OsmBlob>();
        for (; ; )
        {
          if (tim != Environment.TickCount)
          {
            tim = Environment.TickCount;
            Console.WriteLine("{0:N0} / {1:N0}", pos, test.pbfSize);
          }
          int ofs = test.PrepareBuffer(pos, 32);
          OsmBlob blob;
          OsmBlob.DecodeQuick(buf, ofs, out blob);
          blob.pbfOfs = pos;
          pos += blob.blobLen;
          blocks.Add(blob);
          if (pos >= test.pbfSize) break;
        }

        Console.WriteLine();
        Console.WriteLine("            Blocks: {0,15:N0}", blocks.Count);
        Console.WriteLine("    PBF-Compressed: {0,15:N0} Bytes", blocks.Sum(blob => (long)blob.blobLen));
        Console.WriteLine("  PBF-Uncompressed: {0,15:N0} Bytes", blocks.Sum(blob => (long)(blob.blobLen - blob.zlibLen + blob.rawSize)));
        Console.WriteLine();
        #endregion

        test.RandomBuffering = false;
        foreach (var blob in blocks)
        {
          int ofs = test.PrepareBuffer(blob.pbfOfs + blob.zlibOfs, blob.zlibLen);
          var outputBuf = new byte[16 * 1048576];
          int bytes = ProtoBuf.FastInflate(buf, ofs, blob.zlibLen, outputBuf, 0);
          if (bytes != blob.rawSize) throw new PbfParseException();
          int len;
          if (blob.IsHeader)
          {
            HeaderBlock headerBlock;
            len = HeaderBlock.Decode(outputBuf, ofs, out headerBlock);
          }
          else
          {
            len = DecodePrimitiveBlock(outputBuf, ofs);
          }
          if (len != bytes) throw new PbfParseException();
        }
      }
    }
  }
}
