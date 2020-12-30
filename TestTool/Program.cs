// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OsmFastPbf;
using OsmFastPbf.Helper;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

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

    static void ConverterTest_3_MergeNodes()
    {

    }

    static void SpeedCheck()
    {
      using (var pbf = new OsmPbfReader(PbfPath))
      {
        long count = 0;
        long totalCount = pbf.nodeIndex.Sum(x => x.nodeCount);
        //foreach (var node in pbf.ReadAllNodes())
        //{
        //  count++;
        //  if ((count & 1048575) == 1)
        //  {
        //    Console.WriteLine(count.ToString("N0") + " / " + totalCount.ToString("N0"));
        //  }
        //}
        var blockTimes = new List<long>();
        var tim = Stopwatch.StartNew();
        foreach (var nodes in pbf.ReadAllNodes4())
        {
          blockTimes.Add(tim.ElapsedMilliseconds);
          tim.Restart();
          count += nodes.Length;
          lock (OsmPbfReader.times)
          {
            Console.WriteLine(count.ToString("N0") + " / " + totalCount.ToString("N0") + " - " + OsmPbfReader.times.Average().ToString("N2") + " / " + blockTimes.Average().ToString("N2"));
          }
          //foreach (var node in nodes)
          //{
          //  count++;
          //  if ((count & 1048575) == 1)
          //  {
          //    Console.WriteLine(count.ToString("N0") + " / " + totalCount.ToString("N0"));
          //  }
          //}
          if (count > 50000000)
          {
            break;
          }
          nodes.Dispose();
        }
      }
    }

    static void MemTest()
    {
      var tim = Stopwatch.StartNew();
      for (int i = 0; i < 10000; i++)
      {
        //var mem = new OsmNode[1000000];
        var mem = new MemArray<OsmNode>(1000000);
        mem[mem.Length / 2] = new OsmNode(1, 2, 3, null);
        mem[mem.Length - 1] = new OsmNode(2, 3, 4, null);
        mem.Dispose();
      }
      tim.Stop();
      Console.WriteLine("{0:N0} ms", tim.ElapsedMilliseconds);
    }

    static void Main(string[] args)
    {
      //BufferTest(); return;
      //HgtTest(); return;
      //ParseTest(); return;
      //DrawTest(); return;
      //ConverterTest_1_ExtractNodes();
      //ConverterTest_2_SortNodes();
      //ConverterTest_3_MergeNodes();
      SpeedCheck();
      //MemTest();
    }
  }
}
