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

    static int DecodePackedSInt32(byte[] buf, int ofs, out int[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new int[dataLen];
      int resultLen = 0;

      while (len < endLen)
      {
        ulong tmp;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen++] = ProtoBuf.SignedInt32((uint)tmp);
      }

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodePackedSInt64(byte[] buf, int ofs, out long[] val)
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

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodePackedInt32(byte[] buf, int ofs, out int[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new int[dataLen];
      int resultLen = 0;

      while (len < endLen)
      {
        ulong tmp;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen++] = (int)(uint)tmp;
      }

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodePackedUInt32(byte[] buf, int ofs, out uint[] val)
    {
      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      var result = new uint[dataLen];
      int resultLen = 0;

      while (len < endLen)
      {
        ulong tmp;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        result[resultLen++] = (uint)tmp;
      }

      Array.Resize(ref result, resultLen);
      val = result;

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static void DecodeDelta(long[] values)
    {
      for (int i = 1; i < values.Length; i++) values[i] += values[i - 1];
    }

    static void DecodeDelta(int[] values)
    {
      for (int i = 1; i < values.Length; i++) values[i] += values[i - 1];
    }

    static int DecodeDenseInfo(byte[] buf, int ofs)
    {
      /*****
       * message DenseInfo
       * {
       *   repeated int32 version = 1 [packed = true];
       *   repeated sint64 timestamp = 2 [packed = true]; // DELTA coded
       *   repeated sint64 changeset = 3 [packed = true]; // DELTA coded
       *   repeated sint32 uid = 4 [packed = true]; // DELTA coded
       *   repeated sint32 user_sid = 5 [packed = true]; // String IDs for usernames. DELTA coded
       *   
       *   // The visible flag is used to store history information. It indicates that
       *   // the current object version has been created by a delete operation on the
       *   // OSM API.
       *   // When a writer sets this flag, it MUST add a required_features tag with
       *   // value "HistoricalInformation" to the HeaderBlock.
       *   // If this flag is not available for some object it MUST be assumed to be
       *   // true if the file has the required_features tag "HistoricalInformation"
       *   // set.
       *   repeated bool visible = 6 [packed = true];
       * }
       *****/

      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      // --- repeated int32 version = 1 [packed = true]; ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        len++;
        int[] version;
        len += DecodePackedInt32(buf, ofs + len, out version);
      }

      // --- repeated sint64 timestamp = 2 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        long[] timestamp;
        len += DecodePackedSInt64(buf, ofs + len, out timestamp);
        DecodeDelta(timestamp);
      }

      // --- repeated sint64 changeset = 3 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        long[] changeset;
        len += DecodePackedSInt64(buf, ofs + len, out changeset);
        DecodeDelta(changeset);
      }

      // --- repeated sint32 uid = 4 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        int[] uid;
        len += DecodePackedSInt32(buf, ofs + len, out uid);
        DecodeDelta(uid);
      }

      // --- repeated sint32 user_sid = 5 [packed = true]; // String IDs for usernames. DELTA coded ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        len++;
        int[] userSid;
        len += DecodePackedSInt32(buf, ofs + len, out userSid);
        DecodeDelta(userSid);
      }

      // --- repeated bool visible = 6 [packed = true]; ---
      if (buf[ofs + len] == (6 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      if (len != endLen) throw new PbfParseException();

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

      // --- repeated sint64 id = 1 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        len++;
        long[] id;
        len += DecodePackedSInt64(buf, ofs + len, out id);
        DecodeDelta(id);
      }

      // --- repeated Info info = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2)) throw new NotSupportedException();

      // --- optional DenseInfo denseinfo = 5; ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        len++;
        len += DecodeDenseInfo(buf, ofs + len);
      }

      // --- repeated sint64 lat = 8 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (8 << 3 | 2))
      {
        len++;
        long[] lat;
        len += DecodePackedSInt64(buf, ofs + len, out lat);
        DecodeDelta(lat);
      }

      // --- repeated sint64 lon = 9 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (9 << 3 | 2))
      {
        len++;
        long[] lon;
        len += DecodePackedSInt64(buf, ofs + len, out lon);
        DecodeDelta(lon);
      }

      // --- repeated int32 keys_vals = 10 [packed = true]; ---
      if (buf[ofs + len] == (10 << 3 | 2))
      {
        len++;
        int[] keysVals;
        len += DecodePackedInt32(buf, ofs + len, out keysVals);
      }

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodeInfo(byte[] buf, int ofs)
    {
      /*****
       * message Info
       * {
       *   optional int32 version = 1 [default = -1];
       *   optional int64 timestamp = 2;
       *   optional int64 changeset = 3;
       *   optional int32 uid = 4;
       *   optional uint32 user_sid = 5; // String IDs
       *   
       *   // The visible flag is used to store history information. It indicates that
       *   // the current object version has been created by a delete operation on the
       *   // OSM API.
       *   // When a writer sets this flag, it MUST add a required_features tag with
       *   // value "HistoricalInformation" to the HeaderBlock.
       *   // If this flag is not available for some object it MUST be assumed to be
       *   // true if the file has the required_features tag "HistoricalInformation"
       *   // set.
       *   optional bool visible = 6;
       * }
       *****/

      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;
      ulong tmp;

      // --- optional int32 version = 1 [default = -1]; ---
      if (buf[ofs + len] == (1 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        int version = (int)(uint)tmp;
      }

      // --- optional int64 timestamp = 2; ---
      if (buf[ofs + len] == (2 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        long timestamp = (long)tmp;
      }

      // --- optional int64 changeset = 3; ---
      if (buf[ofs + len] == (3 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        long changeset = (long)tmp;
      }

      // --- optional int32 uid = 4; ---
      if (buf[ofs + len] == (4 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        int uid = (int)(uint)tmp;
      }

      // --- optional uint32 user_sid = 5; // String IDs ---
      if (buf[ofs + len] == (5 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        uint userSid = (uint)tmp;
      }

      // --- optional bool visible = 6; ---
      if (buf[ofs + len] == (6 << 3 | 0))
      {
        throw new NotSupportedException();
      }

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodeWays(byte[] buf, int ofs)
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
      ulong tmp;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      long id = (long)tmp;

      // --- repeated uint32 keys = 2 [packed = true]; ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        uint[] keys;
        len += DecodePackedUInt32(buf, ofs + len, out keys);
      }

      // --- repeated uint32 vals = 3 [packed = true]; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        uint[] vals;
        len += DecodePackedUInt32(buf, ofs + len, out vals);
      }

      // --- optional Info info = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        len += DecodeInfo(buf, ofs + len);
      }

      // --- repeated sint64 refs = 8 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (8 << 3 | 2))
      {
        len++;
        long[] refs;
        len += DecodePackedSInt64(buf, ofs + len, out refs);
        DecodeDelta(refs);
      }

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodeRelation(byte[] buf, int ofs)
    {
      /*****
       * message Relation
       * {
       *   enum MemberType
       *   {
       *     NODE = 0;
       *     WAY = 1;
       *     RELATION = 2;
       *   } 
       *   required int64 id = 1;
       *   
       *   // Parallel arrays.
       *   repeated uint32 keys = 2 [packed = true];
       *   repeated uint32 vals = 3 [packed = true];
       *   
       *   optional Info info = 4;
       *   
       *   // Parallel arrays
       *   repeated int32 roles_sid = 8 [packed = true];
       *   repeated sint64 memids = 9 [packed = true]; // DELTA encoded
       *   repeated MemberType types = 10 [packed = true];
       * }
       *****/

      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;

      // --- required int64 id = 1; ---
      if (buf[ofs + len++] != (1 << 3 | 0)) throw new PbfParseException();
      ulong tmp;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      long id = (long)tmp;

      // --- repeated uint32 keys = 2 [packed = true]; ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        uint[] keys;
        len += DecodePackedUInt32(buf, ofs + len, out keys);
      }

      // --- repeated uint32 vals = 3 [packed = true]; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        uint[] vals;
        len += DecodePackedUInt32(buf, ofs + len, out vals);
      }

      // --- optional Info info = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        len += DecodeInfo(buf, ofs + len);
      }

      // --- repeated int32 roles_sid = 8 [packed = true]; ---
      if (buf[ofs + len] == (8 << 3 | 2))
      {
        len++;
        int[] rolesSid;
        len += DecodePackedInt32(buf, ofs + len, out rolesSid);
      }

      // --- repeated sint64 memids = 9 [packed = true]; // DELTA encoded ---
      if (buf[ofs + len] == (9 << 3 | 2))
      {
        len++;
        long[] memids;
        len += DecodePackedSInt64(buf, ofs + len, out memids);
        DecodeDelta(memids);
      }

      // --- repeated MemberType types = 10 [packed = true]; ---
      if (buf[ofs + len] == (10 << 3 | 2))
      {
        len++;
        int[] types;
        len += DecodePackedInt32(buf, ofs + len, out types);
      }

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
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        throw new NotImplementedException();
      }

      // --- optional DenseNodes dense = 2; ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        len += DecodeDenseNodes(buf, ofs + len);
      }

      // --- repeated Way ways = 3; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        len += DecodeWays(buf, ofs + len);
      }

      // --- repeated Relation relations = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        len += DecodeRelation(buf, ofs + len);
      }

      // --- repeated ChangeSet changesets = 5; ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        throw new NotSupportedException();
      }

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
        if (len != endLen) throw new PbfParseException();
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
        for (int blobIndex = 15687; blobIndex < blocks.Count; blobIndex++)
        {
          var blob = blocks[blobIndex];
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
            Console.WriteLine("decode: {0:N0} / {1:N0}", blobIndex, blocks.Count);
            len = DecodePrimitiveBlock(outputBuf, 0);
          }
          if (len != bytes) throw new PbfParseException();
        }
      }
    }
  }
}
