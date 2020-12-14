// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace OsmFastPbf
{
  public static class PbfFastRelations
  {
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
        //int version = (int)(uint)tmp;
      }

      // --- optional int64 timestamp = 2; ---
      if (buf[ofs + len] == (2 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //long timestamp = (long)tmp;
      }

      // --- optional int64 changeset = 3; ---
      if (buf[ofs + len] == (3 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //long changeset = (long)tmp;
      }

      // --- optional int32 uid = 4; ---
      if (buf[ofs + len] == (4 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //int uid = (int)(uint)tmp;
      }

      // --- optional uint32 user_sid = 5; // String IDs ---
      if (buf[ofs + len] == (5 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //uint userSid = (uint)tmp;
      }

      // --- optional bool visible = 6; ---
      if (buf[ofs + len] == (6 << 3 | 0))
      {
        throw new NotSupportedException();
      }

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    static int DecodeRelation(byte[] buf, int ofs, string[] stringTable, out OsmRelation relation)
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
      int[] keys;
      int[] vals;

      // --- required int64 id = 1; ---
      if (buf[ofs + len++] != (1 << 3 | 0)) throw new PbfParseException();
      ulong tmp;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      long id = (long)tmp;

      // --- repeated uint32 keys = 2 [packed = true]; ---
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
        len += DecodeInfo(buf, ofs + len);
      }

      // --- repeated int32 roles_sid = 8 [packed = true]; ---
      if (buf[ofs + len] == (8 << 3 | 2))
      {
        len++;
        int[] rolesSid;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out rolesSid);
      }

      // --- repeated sint64 memids = 9 [packed = true]; // DELTA encoded ---
      if (buf[ofs + len] == (9 << 3 | 2))
      {
        len++;
        long[] memids;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out memids);
      }

      // --- repeated MemberType types = 10 [packed = true]; ---
      if (buf[ofs + len] == (10 << 3 | 2))
      {
        len++;
        int[] types;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out types);
      }

      if (len != endLen) throw new PbfParseException();

      if (keys.Length != vals.Length) throw new PbfParseException();
      relation = new OsmRelation(id, Enumerable.Range(0, keys.Length).Select(i => new KeyValuePair<string, string>(stringTable[keys[i]], stringTable[vals[i]])).ToArray());

      return len;
    }

    static int DecodePrimitiveGroup(byte[] buf, int ofs, OsmBlob blob, string[] stringTable, out OsmRelation relation)
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
        throw new NotSupportedException();
      }

      // --- repeated Relation relations = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        len += DecodeRelation(buf, ofs + len, stringTable, out relation);
      }
      else
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

    public static int DecodePrimitiveBlock(byte[] buf, int ofs, OsmBlob blob, out OsmRelation[] relations)
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
      relations = new OsmRelation[blob.relationCount];
      int relationsIndex = 0;
      while (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        ulong dataLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
        int endLen = len + (int)dataLen;
        while (len < endLen)
        {
          len += DecodePrimitiveGroup(buf, ofs + len, blob, stringTable, out relations[relationsIndex]);
          relationsIndex++;
        }
        if (len != endLen) throw new PbfParseException();
      }

      // optional int32 granularity = 17 [default=100];
      // optional int64 lat_offset = 19 [default=0];
      // optional int64 lon_offset = 20 [default=0];
      // optional int32 date_granularity = 18 [default=1000];

      return len;
    }
  }
}
