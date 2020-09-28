using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OsmFastPbf;
using OsmFastPbf.Helper;

namespace TestTool
{
  partial class Program
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

      // --- repeated int32 version = 1 [packed = true]; ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        len++;
        int[] version;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out version, itemCount);
      }

      // --- repeated sint64 timestamp = 2 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        long[] timestamp;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out timestamp, itemCount);
      }

      // --- repeated sint64 changeset = 3 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        long[] changeset;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out changeset, itemCount);
      }

      // --- repeated sint32 uid = 4 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        int[] uid;
        len += ProtoBuf.DecodePackedSInt32Delta(buf, ofs + len, out uid, itemCount);
      }

      // --- repeated sint32 user_sid = 5 [packed = true]; // String IDs for usernames. DELTA coded ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        len++;
        int[] userSid;
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
      long[] id;

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
        long[] lat;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out lat, id.Length);
      }

      // --- repeated sint64 lon = 9 [packed = true]; // DELTA coded ---
      if (buf[ofs + len] == (9 << 3 | 2))
      {
        len++;
        long[] lon;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out lon, id.Length);
      }

      // --- repeated int32 keys_vals = 10 [packed = true]; ---
      if (buf[ofs + len] == (10 << 3 | 2))
      {
        len++;
        int[] keysVals;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out keysVals);
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
        len += ProtoBuf.DecodePackedUInt32(buf, ofs + len, out keys);
      }

      // --- repeated uint32 vals = 3 [packed = true]; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        uint[] vals;
        len += ProtoBuf.DecodePackedUInt32(buf, ofs + len, out vals);
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
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out refs);
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
        len += ProtoBuf.DecodePackedUInt32(buf, ofs + len, out keys);
      }

      // --- repeated uint32 vals = 3 [packed = true]; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        uint[] vals;
        len += ProtoBuf.DecodePackedUInt32(buf, ofs + len, out vals);
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
      len += ProtoBuf.DecodeStringTable(buf, ofs + len, out stringTable);

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

    public struct BlobTask
    {
      public int pbfBufferOfs;
      public OsmBlob blob;
      public int outputOfs;
      public BlobTask(int pbfBufferOfs, OsmBlob blob, int outputOfs)
      {
        this.pbfBufferOfs = pbfBufferOfs;
        this.blob = blob;
        this.outputOfs = outputOfs;
      }
    }

    static void Main(string[] args)
    {
      //BufferTest();

      string path = "planet-latest.osm.pbf";
      for (int i = 0; i < 32 && !File.Exists(path); i++) path = "../" + path;
      if (!File.Exists(path)) throw new FileNotFoundException(path.TrimStart('.', '/'));

      using (var test = new FastPbfReader(path, 1024 * 1048576))
      {
        #region # // --- Index einlesen ---
        test.RandomBuffering = true;

        long pos = 0;
        var buf = test.buffer;
        int tim = 0;

        var blobs = new List<OsmBlob>();
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
          blobs.Add(blob);
          if (pos >= test.pbfSize) break;
        }

        Console.WriteLine();
        Console.WriteLine("            Blocks: {0,15:N0}", blobs.Count);
        Console.WriteLine("    PBF-Compressed: {0,15:N0} Bytes", blobs.Sum(blob => (long)blob.blobLen));
        Console.WriteLine("  PBF-Uncompressed: {0,15:N0} Bytes", blobs.Sum(blob => (long)(blob.blobLen - blob.zlibLen + blob.rawSize)));
        Console.WriteLine();
        #endregion

        test.RandomBuffering = false;
        int cores = 4;
        var outputBuf = new byte[cores * 16 * 1048576];
        for (int blobIndex = 0; blobIndex < blobs.Count; )
        {
          var todo = new List<BlobTask>();
          var blobFirst = blobs[blobIndex];
          int ofs = test.PrepareBuffer(blobFirst.pbfOfs + blobFirst.zlibOfs, blobFirst.zlibLen);
          if (blobs.Count - blobIndex > cores && test.CheckFastBuffer(blobs[blobIndex + cores - 1].pbfOfs + blobs[blobIndex + cores - 1].zlibOfs, blobs[blobIndex + cores-1].zlibLen) >= 0)
          {
            for (int i = 0; i < cores; i++)
            {
              todo.Add(new BlobTask(test.CheckFastBuffer(blobs[blobIndex + i].pbfOfs + blobs[blobIndex + i].zlibOfs, blobs[blobIndex + i].zlibLen), blobs[blobIndex + i], i * 16 * 1048576));
            }
          }
          else
          {
            todo.Add(new BlobTask(ofs, blobFirst, 0 * 16 * 1048576));
          }

          Parallel.ForEach(todo, blob =>
          {
            // --- entpacken ---
            int bytes = ProtoBuf.FastInflate(buf, blob.pbfBufferOfs, blob.blob.zlibLen, outputBuf, blob.outputOfs);
            if (bytes != blob.blob.rawSize) throw new PbfParseException();
            outputBuf[blob.outputOfs + bytes] = 0;

            // --- decoden ---
            int len;
            if (blob.blob.IsHeader)
            {
              lock (outputBuf)
              {
                blobIndex++;
              }
              HeaderBlock headerBlock;
              len = HeaderBlock.Decode(outputBuf, blob.outputOfs, out headerBlock);
            }
            else
            {
              lock (outputBuf)
              {
                blobIndex++;
                Console.WriteLine("decode: {0:N0} / {1:N0}", blobIndex, blobs.Count);
              }
              len = DecodePrimitiveBlock(outputBuf, blob.outputOfs);
            }
            if (len != blob.blob.rawSize) throw new PbfParseException();
          });
        }
      }
    }
  }
}
