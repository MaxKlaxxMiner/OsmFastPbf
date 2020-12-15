// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmFastPbf;
using OsmFastPbf.Helper;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

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
      //ParseTest(); return;
      //DrawTest(); return;


      var buf = new byte[256];
      for (ulong i = 0; i < ulong.MaxValue; i++)
      {
        int len1 = ProtoBuf.WriteVarInt(buf, 3, i);
        ulong tmp;
        int len2 = ProtoBuf.ReadVarInt(buf, 3, out tmp);
        if (len1 != len2 || tmp != i) throw new Exception();
      }

      using (var pbf = new OsmPbfReader(PbfPath))
      {
        //long cc = 0;
        //long full = 0;
        //foreach (var node in pbf.ReadAllNodes())
        //{
        //  full++;
        //  if (node.values.Length > 0)
        //  {
        //    cc++;
        //    if ((ushort)cc == 0)
        //    {
        //      Console.WriteLine(cc.ToString("N0") + " / " + full.ToString("N0") + " (" + (100.0 / full * cc).ToString("N2") + " %) - " + node);
        //    }
        //  }
        //}
        //foreach (var way in pbf.ReadAllWays())
        //{
        //  Console.WriteLine(way);
        //}
        //foreach (var relation in pbf.ReadAllRelations())
        //{
        //  cc++;
        //  if ((byte)cc == 0)
        //  {
        //    Console.WriteLine(cc.ToString("N0") + " - " + relation);
        //  }
        //}
      }
    }
  }
}
