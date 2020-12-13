
using System;
using System.Collections.Generic;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation
// ReSharper disable RedundantAssignment
// ReSharper disable TooWideLocalVariableScope

namespace OsmFastPbf
{
  public class PbfFastNodes
  {
    static int DecodeDenseInfo(byte[] buf, int ofs, int itemCount)
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
      int[] version;
      long[] timestamp;
      long[] changeset;
      int[] uid;
      int[] userSid;

      // --- repeated int32 version = 1 [packed = true]; ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out version, itemCount);
      }

      // --- repeated sint64 timestamp = 2 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out timestamp, itemCount);
      }

      // --- repeated sint64 changeset = 3 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out changeset, itemCount);
      }

      // --- repeated sint32 uid = 4 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt32Delta(buf, ofs + len, out uid, itemCount);
      }

      // --- repeated sint32 user_sid = 5 [packed = true]; // String IDs for usernames. DELTA coded ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt32Delta(buf, ofs + len, out userSid, itemCount);
      }

      // --- repeated bool visible = 6 [packed = true]; ---
      if (buf[ofs + len] == (6 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodeDenseNodes(byte[] buf, int ofs, out GpsNode[] gpsNodes)
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
      long[] id;
      long[] lat = null;
      long[] lon = null;
      int[] keysVals = null;

      // --- repeated sint64 id = 1 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out id);
      }
      else
      {
        id = new long[0];
      }

      // --- repeated Info info = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2)) throw new NotSupportedException();

      // --- optional DenseInfo denseinfo = 5; ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        len++;
        len += DecodeDenseInfo(buf, ofs + len, id.Length);
      }

      // --- repeated sint64 lat = 8 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (8 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out lat, id.Length);
      }

      // --- repeated sint64 lon = 9 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (9 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out lon, id.Length);
      }

      // --- repeated int32 keys_vals = 10 [packed = true]; ---
      if (buf[ofs + len] == (10 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out keysVals);
      }

      if (len != endLen) throw new PbfParseException();

      gpsNodes = new GpsNode[id.Length];
      for (int i = 0; i < gpsNodes.Length; i++)
      {
        gpsNodes[i] = new GpsNode(id[i], (int)lat[i], (int)lon[i]);
      }

      return len;
    }

    static int DecodePrimitiveGroup(byte[] buf, int ofs, out GpsNode[] nodes)
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
        len++;
        len += DecodeDenseNodes(buf, ofs + len, out nodes);
      }
      else
      {
        nodes = new GpsNode[0];
      }

      // --- repeated Way ways = 3; ---
      if (buf[ofs + len] == (3 << 3 | 2))
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

    public static int DecodePrimitiveBlock(byte[] buf, int ofs, OsmBlob blob, out GpsNode[] output)
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

      // --- repeated PrimitiveGroup primitivegroup = 2; ---
      output = new GpsNode[blob.nodeCount];
      int outputOfs = 0;
      while (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        ulong dataLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
        int endLen = len + (int)dataLen;
        while (len < endLen)
        {
          GpsNode[] nodes;
          len += DecodePrimitiveGroup(buf, ofs + len, out nodes);
          Array.Copy(nodes, 0, output, outputOfs, nodes.Length);
          outputOfs += nodes.Length;
        }
        if (len != endLen) throw new PbfParseException();
      }
      if (outputOfs != output.Length) throw new IndexOutOfRangeException();

      // optional int32 granularity = 17 [default=100];
      // optional int64 lat_offset = 19 [default=0];
      // optional int64 lon_offset = 20 [default=0];
      // optional int32 date_granularity = 18 [default=1000];

      return len;
    }
  }
}
