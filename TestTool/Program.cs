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

      using (var pbf = new OsmPbfReader(PbfPath))
      {
        var nodes = pbf.ReadNodes(240109189, 20833613, 21687149, 1695218178, 240109188, 240109189, 240109190);
      }

      //using ()
      //{
      //  //long testNodeID = 240109189; // Berlin - 52.5170365, 13.3888599 - https://www.openstreetmap.org/node/240109189
      //  //{
      //  //  var nodeBlob = nodeIndex.BinarySearchSingle(x => testNodeID >= x.minNodeId && testNodeID <= x.maxNodeId ? 0L : x.minNodeId - testNodeID);

      //  //  // --- laden ---
      //  //  int pbfOfs = pbfReader.PrepareBuffer(nodeBlob.pbfOfs + nodeBlob.zlibOfs, nodeBlob.zlibLen);

      //  //  // --- entpacken ---
      //  //  int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, nodeBlob.zlibLen, outputBuf, 0);
      //  //  if (bytes != nodeBlob.rawSize) throw new PbfParseException();
      //  //  outputBuf[bytes] = 0;

      //  //  // --- decoden ---
      //  //  GpsNode[] nodes;
      //  //  int len = PbfFastNodes.DecodePrimitiveBlock(outputBuf, 0, nodeBlob, out nodes);
      //  //  if (len != nodeBlob.rawSize) throw new PbfParseException();

      //  //  var node = nodes.BinarySearchSingle(x => x.id - testNodeID);
      //  //  var values = node.values.ToDictionary(x => x.Key, x => x.Value);
      //  //}

      //  var searchWays = new List<long>();
      //  OsmRelationMember[] members;
      //  //long testRelationID = 62504; // Brandenburg (Germany) - https://www.openstreetmap.org/relation/62504
      //  long testRelationID = 341042;
      //  {
      //    var relationBlob = relationIndex.BinarySearchSingle(x => testRelationID >= x.minRelationId && testRelationID <= x.maxRelationId ? 0L : x.minRelationId - testRelationID);

      //    // --- laden ---
      //    int pbfOfs = pbfReader.PrepareBuffer(relationBlob.pbfOfs + relationBlob.zlibOfs, relationBlob.zlibLen);

      //    // --- entpacken ---
      //    int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, relationBlob.zlibLen, outputBuf, 0);
      //    if (bytes != relationBlob.rawSize) throw new PbfParseException();
      //    outputBuf[bytes] = 0;

      //    // --- decoden ---
      //    OsmRelation[] relations;
      //    int len = PbfFastRelations.DecodePrimitiveBlock(outputBuf, 0, relationBlob, out relations);
      //    if (len != relationBlob.rawSize) throw new PbfParseException();

      //    var relation = relations.BinarySearchSingle(x => x.id - testRelationID);
      //    var values = relation.values.ToDictionary(x => x.Key, x => x.Value);
      //    members = relation.members.Where(x => x.type == MemberType.Way && (x.role == "inner" || x.role == "outer")).ToArray();
      //    searchWays.AddRange(members.Select(x => x.id));
      //  }

      //  searchWays.Sort();
      //  var waysOutput = new OsmWay[searchWays.Count];
      //  {
      //    var wayBlob = nodeIndex[0];
      //    OsmWay[] ways = null;
      //    for (int way = 0; way < searchWays.Count; way++)
      //    {
      //      long wayId = searchWays[way];
      //      var nextBlob = wayId >= wayBlob.minWayId && wayId <= wayBlob.maxWayId ? wayBlob : wayIndex.BinarySearchSingle(x => wayId >= x.minWayId && wayId <= x.maxWayId ? 0L : x.minWayId - wayId);

      //      if (nextBlob != wayBlob)
      //      {
      //        wayBlob = nextBlob;

      //        Console.WriteLine("ways: {0:N0} / {1:N0}", way + 1, searchWays.Count);

      //        // --- laden ---
      //        int pbfOfs = pbfReader.PrepareBuffer(wayBlob.pbfOfs + wayBlob.zlibOfs, wayBlob.zlibLen);

      //        // --- entpacken ---
      //        int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, wayBlob.zlibLen, outputBuf, 0);
      //        if (bytes != wayBlob.rawSize) throw new PbfParseException();
      //        outputBuf[bytes] = 0;

      //        // --- decoden ---
      //        int len = PbfFastWays.DecodePrimitiveBlock(outputBuf, 0, wayBlob, out ways);
      //        if (len != wayBlob.rawSize) throw new PbfParseException();
      //      }
      //      waysOutput[way] = ways.BinarySearchSingle(x => x.id - wayId);
      //    }
      //  }
      //  var searchNodes = new HashSet<long>(waysOutput.SelectMany(x => x.nodeIds)).ToArray();
      //  Array.Sort(searchNodes);

      //  var nodesOutput = new OsmNode[searchNodes.Length];
      //  {
      //    var nodeBlob = wayIndex[0];
      //    OsmNode[] nodes = null;
      //    for (int n = 0; n < searchNodes.Length; n++)
      //    {
      //      long nodeId = searchNodes[n];
      //      var nextBlob = nodeId >= nodeBlob.minNodeId && nodeId <= nodeBlob.maxNodeId ? nodeBlob : nodeIndex.BinarySearchSingle(x => nodeId >= x.minNodeId && nodeId <= x.maxNodeId ? 0L : x.minNodeId - nodeId);

      //      if (nextBlob != nodeBlob)
      //      {
      //        nodeBlob = nextBlob;

      //        Console.WriteLine("nodes: {0:N0} / {1:N0}", n + 1, searchNodes.Length);

      //        // --- laden ---
      //        int pbfOfs = pbfReader.PrepareBuffer(nodeBlob.pbfOfs + nodeBlob.zlibOfs, nodeBlob.zlibLen);

      //        // --- entpacken ---
      //        int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, nodeBlob.zlibLen, outputBuf, 0);
      //        if (bytes != nodeBlob.rawSize) throw new PbfParseException();
      //        outputBuf[bytes] = 0;

      //        // --- decoden ---
      //        int len = PbfFastNodes.DecodePrimitiveBlock(outputBuf, 0, nodeBlob, out nodes);
      //        if (len != nodeBlob.rawSize) throw new PbfParseException();
      //      }
      //      nodesOutput[n] = nodes.BinarySearchSingle(x => x.id - nodeId);
      //    }
      //  }

      //  File.WriteAllLines(@"C:\Users\Max\Desktop\Temps\tempwegwerf\relation.txt", members.Select(x => x.id + "\t" + x.role));
      //  File.WriteAllLines(@"C:\Users\Max\Desktop\Temps\tempwegwerf\ways.txt", waysOutput.Select(x => x.id + "\t" + string.Join(",", x.nodeIds)));
      //  File.WriteAllLines(@"C:\Users\Max\Desktop\Temps\tempwegwerf\nodes.txt", nodesOutput.Select(x => x.id + "\t" + x.latCode + "\t" + x.lonCode));
      //}
    }
  }
}
