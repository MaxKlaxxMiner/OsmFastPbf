﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmFastPbf;
using OsmFastPbf.Helper;

namespace TestTool
{
  partial class Program
  {
    static int DecodeStringTable(byte[] buf, int ofs, out string[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;
      var stringTable = new List<string>();
      while (len < endLen)
      {
        string tmp;
        if (buf[ofs + len++] != (1 << 3 | 2)) throw new PbfParseException();
        len += ProtoBuf.ReadString(buf, ofs + len, out tmp);
        stringTable.Add(tmp);
      }
      if (len != endLen) throw new PbfParseException();
      val = stringTable.ToArray();
      return len;
    }

    static int DecodeSignedValues(byte[] buf, int ofs, out long[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new long[dataLen];
      int resultLen = 0;

      while (len < endLen)
      {
        ulong tmp;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen++] = ProtoBuf.SignedInt64(tmp);
      }

      if (len != endLen) throw new PbfParseException();

      Array.Resize(ref result, result.Length);
      val = result;

      return len;
    }

    static int DecodeDenseNodes(byte[] buf, int ofs)
    {
      /*****
       * message DenseNodes
       * {
       *   repeated sint64 id = 1 [packed = true]; // DELTA coded
       *   
       *   //repeated Info info = 4;
       *   optional DenseInfo denseinfo = 5;
       *   
       *   repeated sint64 lat = 8 [packed = true]; // DELTA coded
       *   repeated sint64 lon = 9 [packed = true]; // DELTA coded
       *   
       *   // Special packing of keys and vals into one array. May be empty if all nodes in this block are tagless.
       *   repeated int32 keys_vals = 10 [packed = true];
       * }
       *****/

      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      // --- repeated sint64 id = 1 [packed = true]; ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        len++;
        long[] ids;
        len += DecodeSignedValues(buf, ofs + len, out ids);
      }

      // --- repeated Info info = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2)) throw new NotSupportedException();

      // todo: --- optional DenseInfo denseinfo = 5; ---

      // todo: --- repeated sint64 lat = 8 [packed = true]; ---

      // todo: --- repeated sint64 lon = 9 [packed = true]; ---

      // todo: --- repeated int32 keys_vals = 10 [packed = true]; ---

      if (len != endLen) throw new PbfParseException();

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
        len++;
        DecodeDenseNodes(buf, ofs + len);
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
      if (buf[ofs + len++] != (1 << 3 | 2)) throw new PbfParseException();
      string[] stringTable;
      len += DecodeStringTable(buf, ofs + len, out stringTable);

      // --- repeated PrimitiveGroup primitivegroup = 2; ---
      while (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        ulong dataLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
        int endLen = len + (int)dataLen;
        while (len < endLen)
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
            len = HeaderBlock.Decode(outputBuf, 0, out headerBlock);
          }
          else
          {
            len = DecodePrimitiveBlock(outputBuf, 0);
          }
          if (len != bytes) throw new PbfParseException();
        }
      }
    }
  }
}
