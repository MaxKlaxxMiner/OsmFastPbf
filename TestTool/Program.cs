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

      using (var pbfReader = new FastPbfReader(PbfPath, 256 * 1048576))
      {
        var outputBuf = new byte[pbfReader.buffer.Length * 4];

        //var firstWays = index.First(x => x.wayCount > 0);
        //{
        //  var blob = firstWays;

        //  // --- laden ---
        //  int pbfOfs = pbfReader.PrepareBuffer(blob.pbfOfs + blob.zlibOfs, blob.zlibLen);

        //  // --- entpacken ---
        //  int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, blob.zlibLen, outputBuf, 0);
        //  if (bytes != blob.rawSize) throw new PbfParseException();
        //  outputBuf[bytes] = 0;

        //  // --- decoden ---
        //  int len = PbfFastWays.DecodePrimitiveBlock(outputBuf, 0);
        //  if (len != blob.rawSize) throw new PbfParseException();
        //}

        long testNodeID = 240109189; // Berlin - 52.5170365, 13.3888599 - https://www.openstreetmap.org/node/240109189
        {
          var blob = nodeIndex.BinarySearchSingle(x => testNodeID >= x.minNodeId  && testNodeID <= x.maxNodeId ? 0L : x.minNodeId - testNodeID);

          // --- laden ---
          int pbfOfs = pbfReader.PrepareBuffer(blob.pbfOfs + blob.zlibOfs, blob.zlibLen);

          // --- entpacken ---
          int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, blob.zlibLen, outputBuf, 0);
          if (bytes != blob.rawSize) throw new PbfParseException();
          outputBuf[bytes] = 0;

          // --- decoden ---
          GpsNode[] nodes;
          int len = PbfFastNodes.DecodePrimitiveBlock(outputBuf, 0, blob, out nodes);
          if (len != blob.rawSize) throw new PbfParseException();

          var node = nodes.BinarySearchSingle(x => x.id - testNodeID);
          var values = node.values.ToDictionary(x => x.Key, x => x.Value);
        }
      }
    }
  }
}
