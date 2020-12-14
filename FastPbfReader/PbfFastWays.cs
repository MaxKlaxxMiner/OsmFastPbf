using System;
using System.Collections.Generic;
using System.Linq;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation

namespace OsmFastPbf
{
  public class PbfFastWays
  {
    static int DecodeWays(byte[] buf, int ofs, string[] stringTable, out OsmWay way)
    {
      /*****
       * message Way
       * {
       *   required int64 id = 1;
       *   // Parallel arrays.
       *   repeated uint32 keys = 2 [packed = true];
       *   repeated uint32 vals = 3 [packed = true];
       *   
       *   optional Info info = 4;
       *   
       *   repeated sint64 refs = 8 [packed = true];  // DELTA coded
       * }
       *****/

      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      // --- required int64 id = 1; ---
      if (buf[ofs + len++] != (1 << 3 | 0)) throw new PbfParseException();
      ulong id;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out id);

      // --- repeated uint32 keys = 2 [packed = true]; ---
      int[] keys;
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out keys);
      }
      else
      {
        keys = new int[0];
      }

      // --- repeated uint32 vals = 3 [packed = true]; ---
      int[] vals;
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out vals);
      }
      else
      {
        vals = new int[0];
      }

      // --- optional Info info = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        len += PbfFast.DecodeInfo(buf, ofs + len);
      }

      // --- repeated sint64 refs = 8 [packed = true]; // DELTA coded ---
      long[] refs;
      if (buf[ofs + len] == (8 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out refs);
      }
      else
      {
        refs = new long[0];
      }

      if (len != endLen) throw new PbfParseException();

      if (keys.Length != vals.Length) throw new PbfParseException();

      way = new OsmWay(
        (long)id,
        Enumerable.Range(0, keys.Length).Select(i => new KeyValuePair<string, string>(stringTable[keys[i]], stringTable[vals[i]])).ToArray(keys.Length),
        refs
      );

      return len;
    }

    static int DecodePrimitiveGroup(byte[] buf, int ofs, string[] stringTable, out OsmWay way)
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
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      // --- optional DenseNodes dense = 2; ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      // --- repeated Way ways = 3; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        len += DecodeWays(buf, ofs + len, stringTable, out way);
      }
      else
      {
        throw new NotSupportedException();
      }

      // --- repeated Relation relations = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      // --- repeated ChangeSet changesets = 5; ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      return len;
    }

    public static int DecodePrimitiveBlock(byte[] buf, int ofs, OsmBlob blob, out OsmWay[] ways)
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
      len += ProtoBuf.DecodeStringTable(buf, ofs + len, out stringTable);

      ways = new OsmWay[blob.wayCount];
      int waysIndex = 0;
      // --- repeated PrimitiveGroup primitivegroup = 2; ---
      while (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        ulong dataLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
        int endLen = len + (int)dataLen;
        while (len < endLen)
        {
          len += DecodePrimitiveGroup(buf, ofs + len, stringTable, out ways[waysIndex]);
          waysIndex++;
        }
        if (len != endLen) throw new PbfParseException();
      }
      if (waysIndex != ways.Length) throw new PbfParseException();

      // optional int32 granularity = 17 [default=100];
      // optional int64 lat_offset = 19 [default=0];
      // optional int64 lon_offset = 20 [default=0];
      // optional int32 date_granularity = 18 [default=1000];

      return len;
    }
  }
}
