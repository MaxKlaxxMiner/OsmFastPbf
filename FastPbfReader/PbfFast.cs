using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation

namespace OsmFastPbf
{
  public class PbfFast
  {
    #region # // --- Scan-Methoden ---
    static int ScanDenseInfo(byte[] buf, int ofs, int itemCount)
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

    static int ScanDenseNodes(byte[] buf, int ofs, out long count, out long minId, out long maxId)
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
        count = id.Length;
        minId = id[0];
        maxId = id[id.Length - 1];
      }
      else
      {
        id = new long[0];
        count = 0;
        minId = 0;
        maxId = 0;
      }

      // --- repeated Info info = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2)) throw new NotSupportedException();

      // --- optional DenseInfo denseinfo = 5; ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        len++;
        len += ScanDenseInfo(buf, ofs + len, id.Length);
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

    static int ScanInfo(byte[] buf, int ofs)
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

    static int ScanWays(byte[] buf, int ofs, out long id)
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
      id = (long)tmp;

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
        len += ScanInfo(buf, ofs + len);
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

    static int ScanRelation(byte[] buf, int ofs, out long id)
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
      id = (long)tmp;

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
        len += ScanInfo(buf, ofs + len);
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

    static int ScanPrimitiveGroup(byte[] buf, int ofs, OsmBlob blob = null)
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
        long tmpCount, tmpMin, tmpMax;
        len += ScanDenseNodes(buf, ofs + len, out tmpCount, out tmpMin, out tmpMax);
        if (blob != null)
        {
          blob.nodeCount += tmpCount;
          if (blob.minNodeId == 0 || tmpMin < blob.minNodeId) blob.minNodeId = tmpMin;
          if (blob.maxNodeId == 0 || tmpMax > blob.maxNodeId) blob.maxNodeId = tmpMax;
        }
      }

      // --- repeated Way ways = 3; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        long tmp;
        len += ScanWays(buf, ofs + len, out tmp);
        if (blob != null)
        {
          blob.wayCount++;
          if (blob.minWayId == 0 || tmp < blob.minWayId) blob.minWayId = tmp;
          if (blob.maxWayId == 0 || tmp > blob.maxWayId) blob.maxWayId = tmp;
        }
      }

      // --- repeated Relation relations = 4; ---
      if (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        long tmp;
        len += ScanRelation(buf, ofs + len, out tmp);
        if (blob != null)
        {
          blob.relationCount++;
          if (blob.minRelationId == 0 || tmp < blob.minRelationId) blob.minRelationId = tmp;
          if (blob.maxRelationId == 0 || tmp > blob.maxRelationId) blob.maxRelationId = tmp;
        }
      }

      // --- repeated ChangeSet changesets = 5; ---
      if (buf[ofs + len] == (5 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      return len;
    }

    static int ScanPrimitiveBlock(byte[] buf, int ofs, OsmBlob blob = null)
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
      //string[] stringTable;
      //len += ProtoBuf.DecodeStringTable(buf, ofs + len, out stringTable);
      len += ProtoBuf.SkipStringTable(buf, ofs + len);

      // --- repeated PrimitiveGroup primitivegroup = 2; ---
      if (blob != null)
      {
        blob.minNodeId = 0;
        blob.maxNodeId = 0;
        blob.scanned = true;
      }
      while (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        ulong dataLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
        int endLen = len + (int)dataLen;
        while (len < endLen)
        {
          len += ScanPrimitiveGroup(buf, ofs + len, blob);
        }
        if (len != endLen) throw new PbfParseException();
      }

      // optional int32 granularity = 17 [default=100];
      // optional int64 lat_offset = 19 [default=0];
      // optional int64 lon_offset = 20 [default=0];
      // optional int32 date_granularity = 18 [default=1000];

      return len;
    }

    public static List<OsmBlob> ReadIndex(string path, bool console = true)
    {
      if (console) Console.WriteLine();
      using (var pbfReader = new FastPbfReader(path, 256 * 1048576))
      {
        #region # // --- Basis-Index einlesen und erstellen (sofern noch nicht vorhanden) ---
        var blobs = new List<OsmBlob>();

        if (File.Exists(path + ".index.tsv") && new FileInfo(path + ".index.tsv").LastWriteTimeUtc == new FileInfo(path).LastWriteTimeUtc)
        {
          if (console) Console.Write("  read index...");
          blobs.AddRange(File.ReadLines(path + ".index.tsv").Select(OsmBlob.FromTsv));
          if (console) Console.WriteLine(" ok. ({0:N0} Blocks)", blobs.Count);
        }
        else
        {
          console = true;
          pbfReader.RandomBuffering = true;

          long pos = 0;
          int tim = 0;

          for (; ; )
          {
            if (tim != Environment.TickCount)
            {
              tim = Environment.TickCount;
              Console.WriteLine("  scan file: {0:N0} / {1:N0}", pos, pbfReader.pbfSize);
            }
            int ofs = pbfReader.PrepareBuffer(pos, 32);
            OsmBlob blob;
            OsmBlob.DecodeQuick(pbfReader.buffer, ofs, out blob);
            blob.pbfOfs = pos;
            pos += blob.blobLen;
            blobs.Add(blob);
            if (pos >= pbfReader.pbfSize) break;
          }
        }

        if (console)
        {
          Console.WriteLine();
          Console.WriteLine("  PBF-Blocks      : {0,15:N0}", blobs.Count);
          Console.WriteLine("  PBF-Compressed  : {0,15:N0} Bytes", blobs.Sum(blob => (long)blob.blobLen));
          Console.WriteLine("  PBF-Uncompressed: {0,15:N0} Bytes", blobs.Sum(blob => (long)(blob.blobLen - blob.zlibLen + blob.rawSize)));
          Console.WriteLine();
        }
        #endregion

        #region # // --- Daten einlesen und Details in den Index schreiben ---
        pbfReader.RandomBuffering = false;
        var outputBuf = new byte[pbfReader.buffer.Length * 4];
        for (int blobIndex = 0; blobIndex < blobs.Count; )
        {
          if (blobs[blobIndex].scanned)
          {
            blobIndex++;
            continue;
          }
          var blobTasks = new List<BlobTask>();
          var blobFirst = blobs[blobIndex];
          Console.Write("  read PBF: {0:N0} / {1:N0} ...", blobFirst.pbfOfs, pbfReader.pbfSize);
          pbfReader.PrepareBuffer(blobFirst.pbfOfs + blobFirst.zlibOfs, blobFirst.zlibLen);
          Console.WriteLine(" ok.");

          int outputOfs = 0;
          for (int i = blobIndex; i < blobs.Count && outputOfs + blobs[i].rawSize < outputBuf.Length; i++)
          {
            if (blobs[i].scanned) continue;
            int ofs = pbfReader.CheckFastBuffer(blobs[i].pbfOfs + blobs[i].zlibOfs, blobs[i].zlibLen);
            if (ofs < 0) break;
            blobTasks.Add(new BlobTask(ofs, i, blobs[i], outputOfs));
            outputOfs += blobs[i].rawSize + 1;
          }

          Action<BlobTask> scanFunc = task =>
          {
            // --- entpacken ---
            int bytes = ProtoBuf.FastInflate(pbfReader.buffer, task.pbfBufferOfs, task.blob.zlibLen, outputBuf, task.outputOfs);
            if (bytes != task.blob.rawSize) throw new PbfParseException();
            outputBuf[task.outputOfs + bytes] = 0;

            // --- decoden ---
            int len;
            if (task.blob.IsHeader)
            {
              lock (outputBuf)
              {
                blobIndex++;
              }
              HeaderBlock headerBlock;
              len = HeaderBlock.Decode(outputBuf, task.outputOfs, out headerBlock);
              task.blob.scanned = true;
            }
            else
            {
              len = ScanPrimitiveBlock(outputBuf, task.outputOfs, task.blob);

              lock (outputBuf)
              {
                blobIndex++;
                Console.Write("  decode: {0:N0} / {1:N0}", task.blobIndex, blobs.Count);
                if (task.blob.minNodeId > 0)
                {
                  Console.Write(" - Nodes: {0:N0} (ID: {1:N0} - {2:N0})", task.blob.nodeCount, task.blob.minNodeId, task.blob.maxNodeId);
                }
                if (task.blob.minWayId > 0)
                {
                  Console.Write(" - Ways: {0:N0} (ID: {1:N0} - {2:N0})", task.blob.wayCount, task.blob.minWayId, task.blob.maxWayId);
                }
                if (task.blob.minRelationId > 0)
                {
                  Console.Write(" - Relations: {0:N0} (ID: {1:N0} - {2:N0})", task.blob.relationCount, task.blob.minRelationId, task.blob.maxRelationId);
                }
                Console.WriteLine();
              }
            }
            if (len != task.blob.rawSize) throw new PbfParseException();
          };

          //foreach (var task in blobTasks) scanFunc(task); int results = blobTasks.Count;
          int results = blobTasks.SelectParallelEnumerable(task => { scanFunc(task); return true; }).Count();

          if (results > 0)
          {
            Console.Write("  write index...");
            File.WriteAllLines(path + ".index.tsv", blobs.Select(x => x.ToTsv()));
            new FileInfo(path + ".index.tsv").LastWriteTimeUtc = new FileInfo(path).LastWriteTimeUtc;
            Console.WriteLine(" ok.");
          }
          Console.WriteLine();
          if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
          {
            Environment.Exit(0);
          }
        }
        if (console)
        {
          Console.WriteLine("  Nodes-Total     : {0,15:N0}", blobs.Sum(x => x.nodeCount));
          Console.WriteLine("  Ways-Total      : {0,15:N0}", blobs.Sum(x => x.wayCount));
          Console.WriteLine("  Realtions-Total : {0,15:N0}", blobs.Sum(x => x.relationCount));
          Console.WriteLine();
        }
        #endregion

        return blobs;
      }
    }
    #endregion

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

    #region # // --- Nodes-Methoden ---
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

    static int DecodeDenseNodes(byte[] buf, int ofs, string[] stringTable, out OsmNode[] gpsNodes)
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
      int[] nodeKeys;
      if (buf[ofs + len] == (10 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out nodeKeys);
      }
      else
      {
        nodeKeys = new int[0];
      }

      if (len != endLen) throw new PbfParseException();

      var liBuf = new KeyValuePair<string, string>[256];
      gpsNodes = new OsmNode[id.Length];
      for (int i = 0, nodeIndex = 0; i < nodeKeys.Length; i++, nodeIndex++)
      {
        int li = 0;
        while (nodeKeys[i] != 0)
        {
          liBuf[li++] = new KeyValuePair<string, string>(stringTable[nodeKeys[i]], stringTable[nodeKeys[i + 1]]);
          i += 2;
        }
        if (li > 0)
        {
          var t = new KeyValuePair<string, string>[li];
          Array.Copy(liBuf, 0, t, 0, t.Length);
          gpsNodes[nodeIndex] = new OsmNode(id[nodeIndex], (int)lat[nodeIndex], (int)lon[nodeIndex], t);
        }
        else
        {
          gpsNodes[nodeIndex] = new OsmNode(id[nodeIndex], (int)lat[nodeIndex], (int)lon[nodeIndex], null);
        }
      }

      return len;
    }

    static int DecodePrimitiveGroup(byte[] buf, int ofs, string[] stringTable, out OsmNode[] nodes)
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
        len += DecodeDenseNodes(buf, ofs + len, stringTable, out nodes);
      }
      else
      {
        nodes = new OsmNode[0];
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

    public static int DecodePrimitiveBlock(byte[] buf, int ofs, OsmBlob blob, out OsmNode[] gpsNodes)
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
      gpsNodes = new OsmNode[blob.nodeCount];
      int outputOfs = 0;
      while (buf[ofs + len] == (2 << 3 | 2))
      {
        len++;
        ulong dataLen;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
        int endLen = len + (int)dataLen;
        while (len < endLen)
        {
          OsmNode[] nodes;
          len += DecodePrimitiveGroup(buf, ofs + len, stringTable, out nodes);
          Array.Copy(nodes, 0, gpsNodes, outputOfs, nodes.Length);
          outputOfs += nodes.Length;
        }
        if (len != endLen) throw new PbfParseException();
      }
      if (outputOfs != gpsNodes.Length) throw new IndexOutOfRangeException();

      // optional int32 granularity = 17 [default=100];
      // optional int64 lat_offset = 19 [default=0];
      // optional int64 lon_offset = 20 [default=0];
      // optional int32 date_granularity = 18 [default=1000];

      return len;
    }
    #endregion

    #region # // --- Ways-Methoden ---
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
        len += DecodeInfo(buf, ofs + len);
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
    #endregion

    #region # // --- Relations-Methoden ---
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

      // --- required int64 id = 1; ---
      if (buf[ofs + len++] != (1 << 3 | 0)) throw new PbfParseException();
      ulong tmp;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      long id = (long)tmp;

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
        len += DecodeInfo(buf, ofs + len);
      }

      // --- repeated int32 roles_sid = 8 [packed = true]; ---
      int[] rolesSid;
      if (buf[ofs + len] == (8 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out rolesSid);
      }
      else
      {
        rolesSid = new int[0];
      }

      // --- repeated sint64 memids = 9 [packed = true]; // DELTA encoded ---
      long[] memids;
      if (buf[ofs + len] == (9 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedSInt64Delta(buf, ofs + len, out memids);
      }
      else
      {
        memids = new long[0];
      }

      // --- repeated MemberType types = 10 [packed = true]; ---
      int[] types;
      if (buf[ofs + len] == (10 << 3 | 2))
      {
        len++;
        len += ProtoBuf.DecodePackedInt32(buf, ofs + len, out types);
      }
      else
      {
        types = new int[0];
      }

      if (len != endLen) throw new PbfParseException();

      if (keys.Length != vals.Length) throw new PbfParseException();
      if (rolesSid.Length != memids.Length || rolesSid.Length != types.Length) throw new PbfParseException();

      relation = new OsmRelation(
        id,
        Enumerable.Range(0, keys.Length).Select(i => new KeyValuePair<string, string>(stringTable[keys[i]], stringTable[vals[i]])).ToArray(keys.Length),
        Enumerable.Range(0, memids.Length).Select(i => new OsmRelationMember(memids[i], (MemberType)types[i], stringTable[rolesSid[i]])).ToArray(memids.Length)
      );

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
    #endregion
  }
}
