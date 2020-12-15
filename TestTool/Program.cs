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
        long testRelationID = 418713; // Herrnhut (Germany)
        //long testRelationID = 62504; // Brandenburg (Germany) - https://www.openstreetmap.org/relation/62504
        //long testRelationID = 1434381; // Insel Rügen (Germany)
        //long testRelationID = 51477; // Germany
        //long testRelationID = 2214684; // La Gomera (Canarias)
        //long testRelationID = 2182003; // Menorca (Balearic Islands)

        var relations = pbf.ReadRelations(testRelationID);
        for (; ; ) // untergeordnete Relationen einlesen
        {
          var needRelations = new List<long>();
          foreach (var r in relations)
          {
            foreach (var sub in r.members.Where(x => x.type == MemberType.Relation))
            {
              if (relations.Any(x => x.id == sub.id)) continue;
              needRelations.Add(sub.id);
            }
          }
          if (needRelations.Count == 0) break;
          relations = relations.Concat(pbf.ReadRelations(needRelations.ToArray())).ToArray(relations.Length + needRelations.Count);
        }

        var ways = pbf.ReadWays(relations.SelectMany(r => r.members.Where(x => x.type == MemberType.Way).Select(x => x.id)).ToArray());

        var nodes = pbf.ReadNodes(relations.SelectMany(r => r.members.Where(x => x.type == MemberType.Node).Select(x => x.id)).Concat(ways.SelectMany(w => w.nodeIds)).ToArray());

        Console.WriteLine("nodes: {0:N0}", nodes.Length);
      }
    }
  }
}
