﻿using System.IO;
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

      using (var pbfReader = new FastPbfReader(PbfPath, 256 * 1048576))
      {
        var outputBuf = new byte[pbfReader.buffer.Length * 4];
        var firstWays = index.First(x => x.wayCount > 0);
        {
          var blob = firstWays;

          // --- laden ---
          int pbfOfs = pbfReader.PrepareBuffer(blob.pbfOfs + blob.zlibOfs, blob.zlibLen);

          // --- entpacken ---
          int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, blob.zlibLen, outputBuf, 0);
          if (bytes != blob.rawSize) throw new PbfParseException();
          outputBuf[bytes] = 0;

          // --- decoden ---
          int len = PbfFastScan.DecodePrimitiveBlock(outputBuf, 0);
          if (len != blob.rawSize) throw new PbfParseException();
        }
      }
    }
  }
}
