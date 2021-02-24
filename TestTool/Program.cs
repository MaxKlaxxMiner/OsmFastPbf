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
          //if (count > 50000000)
          //{
          //  break;
          //}
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

    static IEnumerable<OsmNode> GetPath(OsmWay[] ways, OsmNode[] nodes)
    {
      var nodesDict = nodes.GroupBy(x => x.id).Select(x => x.First()).ToDictionary(x => x.id, x => x);

      long lastNodeId = ways[0].nodeIds[0];
      yield return nodesDict[lastNodeId];

      foreach (var way in ways)
      {
        if (way.nodeIds.First() == lastNodeId)
        {
          foreach (var node in way.nodeIds)
          {
            if (node == lastNodeId) continue;
            yield return nodesDict[node];
          }
          lastNodeId = way.nodeIds.Last();
        }
        else if (way.nodeIds.Last() == lastNodeId)
        {
          foreach (var node in way.nodeIds.Reverse())
          {
            if (node == lastNodeId) continue;
            yield return nodesDict[node];
          }
          lastNodeId = way.nodeIds.First();
        }
        else
        {
          Console.WriteLine("panic!");
        }
      }

    }

    static int WriteNodesPath(byte[] buf, int ofs, OsmNode[] nodes)
    {
      int p = ProtoBuf.WriteVarInt(buf, ofs, (uint)nodes.Length);
      foreach (var node in nodes)
      {
        p += node.WriteBinary(buf, ofs + p);
      }
      return p;
    }

    static int ReadNodesPath(byte[] buf, int ofs, out OsmNode[] nodes)
    {
      ulong tmp;
      int p = ProtoBuf.ReadVarInt(buf, ofs, out tmp);
      nodes = new OsmNode[tmp];
      for (int i = 0; i < nodes.Length; i++)
      {
        p += OsmNode.ReadBinary(buf, ofs + p, out nodes[i]);
      }
      return p;
    }

    static void MappingTest()
    {
      long[] relIds = { 3920249L };
      //long[] relIds = { 3459013L };
      //long[] relIds = { 62504L }; // Brandenburg
      //long[] relIds = { 51477L }; // Deutschland

      using (var pbf = new OsmPbfReader(PbfPath))
      {
        foreach (long relId in relIds)
        {
          OsmNode[] nodesPath;

          if (File.Exists("path_cache_" + relId + ".dat"))
          {
            byte[] buf = File.ReadAllBytes("path_cache_" + relId + ".dat");
            int len = ReadNodesPath(buf, 0, out nodesPath);
            if (len != buf.Length) throw new IOException();
          }
          else
          {
            var rels = pbf.ReadRelations(relIds);
            var ways = pbf.ReadWays(rels.First().members.Where(x => x.type == MemberType.Way).Select(x => x.id).ToArray());
            var nodes = pbf.ReadNodes(ways.SelectMany(x => x.nodeIds).ToArray());
            nodesPath = GetPath(ways, nodes).ToArray();

            byte[] buf = new byte[1048576 * 256];
            int len = WriteNodesPath(buf, 0, nodesPath);
            Array.Resize(ref buf, len);

            File.WriteAllBytes("path_cache_" + relId + ".dat", buf);
          }

          var polyLines = new List<GpsLine>();
          for (int i = 1; i < nodesPath.Length; i++)
          {
            if (nodesPath[i - 1].latCode >= nodesPath[i].latCode)
            {
              polyLines.Add(new GpsLine(new GpsPos(nodesPath[i - 1]), new GpsPos(nodesPath[i])));
            }
            else
            {
              polyLines.Add(new GpsLine(new GpsPos(nodesPath[i]), new GpsPos(nodesPath[i - 1])));
            }
          }
          polyLines.Sort((x, y) => x.pos1.posY.CompareTo(y.pos1.posY));

          // --- init ---
          int maxLinesPerStripe = (int)(Math.Sqrt(nodesPath.Length) * 0.3) + 1;
          maxLinesPerStripe = 0;

          int limitStripes = polyLines.GroupBy(line => line.pos1.posY).Max(g => g.Count());

          if (maxLinesPerStripe < limitStripes) maxLinesPerStripe = limitStripes;

          var firstLines = new List<GpsLine>();
          foreach (var line in polyLines)
          {
            if (firstLines.Count >= maxLinesPerStripe && firstLines.Last().pos1.posY < line.pos1.posY) break;
            firstLines.Add(line);
          }

          var stripes = new List<Tuple<uint, uint, List<GpsLine>>>
          {
            new Tuple<uint, uint, List<GpsLine>>(firstLines.First().pos1.posY, firstLines.Last().pos1.posY, firstLines)
          };

          for (; ; )
          {
            uint startY = stripes.Last().Item2;
            var nextLines = polyLines.Where(line => line.pos1.posY < startY && line.pos2.posY >= startY).ToList();
            int maxCount = nextLines.Count + maxLinesPerStripe;

            foreach (var line in polyLines)
            {
              if (line.pos1.posY >= startY)
              {
                if (nextLines.Count > maxCount && nextLines.Last().pos1.posY < line.pos1.posY) break;
                nextLines.Add(line);
              }
            }

            stripes.Add(new Tuple<uint, uint, List<GpsLine>>(nextLines.First().pos1.posY, nextLines.Last().pos1.posY, nextLines));
            if (Equals(nextLines.Last(), polyLines.Last())) break;
          }

          int sumStripes1 = stripes.Sum(s => s.Item3.Count);

          stripes.Add(new Tuple<uint, uint, List<GpsLine>>(stripes.Last().Item2 + 1, stripes.Last().Item2, new List<GpsLine>()));
          var fastStripes = stripes.Select((x, i) => new GpsStripe(i == 0 ? x.Item1 : stripes[i - 1].Item2, x.Item2, x.Item3.ToArray())).ToArray();

          DrawTest(nodesPath, polyLines, fastStripes);
        }

      }
    }

    static void Main(string[] args)
    {
      //BufferTest(); return;
      //HgtTest(); return;
      //ParseTest(); return;
      //DrawTest(); return;
      MappingTest();
      //ConverterTest_1_ExtractNodes();
      //ConverterTest_2_SortNodes();
      //ConverterTest_3_MergeNodes();
      //SpeedCheck();
      //MemTest();
    }
  }
}
