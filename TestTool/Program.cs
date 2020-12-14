// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmFastPbf;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation
// ReSharper disable UnusedVariable

namespace TestTool
{
  partial class Program
  {
    static string PbfPath
    {
      get
      {
        string path = "planet-latest.osm.pbf";
        for (int i = 0; i < 32 && !File.Exists(path); i++) path = "../" + path;
        if (!File.Exists(path)) throw new FileNotFoundException(path.TrimStart('.', '/'));
        return path;
      }
    }

    static void Main(string[] args)
    {
      //BufferTest(); return;
      //HgtTest(); return;

      var index = PbfFastScan.ReadIndex(PbfPath, false);
      var nodeIndex = index.Where(x => x.nodeCount > 0).ToArray();
      var wayIndex = index.Where(x => x.wayCount > 0).ToArray();
      var relationIndex = index.Where(x => x.relationCount > 0).ToArray();
      int maxBlob = index.Max(x => x.blobLen);
      int maxRaw = index.Max(x => x.rawSize);

      using (var pbfReader = new FastPbfReader(PbfPath, (maxBlob + 4096) / 4096 * 4096))
      {
        var outputBuf = new byte[(maxRaw + 4096) / 4096 * 4096];

        //long testNodeID = 240109189; // Berlin - 52.5170365, 13.3888599 - https://www.openstreetmap.org/node/240109189
        //{
        //  var nodeBlob = nodeIndex.BinarySearchSingle(x => testNodeID >= x.minNodeId && testNodeID <= x.maxNodeId ? 0L : x.minNodeId - testNodeID);

        //  // --- laden ---
        //  int pbfOfs = pbfReader.PrepareBuffer(nodeBlob.pbfOfs + nodeBlob.zlibOfs, nodeBlob.zlibLen);

        //  // --- entpacken ---
        //  int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, nodeBlob.zlibLen, outputBuf, 0);
        //  if (bytes != nodeBlob.rawSize) throw new PbfParseException();
        //  outputBuf[bytes] = 0;

        //  // --- decoden ---
        //  GpsNode[] nodes;
        //  int len = PbfFastNodes.DecodePrimitiveBlock(outputBuf, 0, nodeBlob, out nodes);
        //  if (len != nodeBlob.rawSize) throw new PbfParseException();

        //  var node = nodes.BinarySearchSingle(x => x.id - testNodeID);
        //  var values = node.values.ToDictionary(x => x.Key, x => x.Value);
        //}

        long testRelationID = 62504; // Brandenburg (Germany) - https://www.openstreetmap.org/relation/62504
        {
          var relationBlob = relationIndex.BinarySearchSingle(x => testRelationID >= x.minRelationId && testRelationID <= x.maxRelationId ? 0L : x.minRelationId - testRelationID);

          // --- laden ---
          int pbfOfs = pbfReader.PrepareBuffer(relationBlob.pbfOfs + relationBlob.zlibOfs, relationBlob.zlibLen);

          // --- entpacken ---
          int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, relationBlob.zlibLen, outputBuf, 0);
          if (bytes != relationBlob.rawSize) throw new PbfParseException();
          outputBuf[bytes] = 0;

          // --- decoden ---
          OsmRelation[] relations;
          int len = PbfFastRelations.DecodePrimitiveBlock(outputBuf, 0, relationBlob, out relations);
          if (len != relationBlob.rawSize) throw new PbfParseException();

          var relation = relations.BinarySearchSingle(x => x.id - testRelationID);
          var values = relation.values.ToDictionary(x => x.Key, x => x.Value);
          var members = relation.members;
        }
      }
    }
  }
}
