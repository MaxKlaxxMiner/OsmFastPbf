// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf;
using OsmFastPbf.Helper;

namespace TestTool
{
  partial class Program
  {
    static void ParseTest()
    {
      using (var pbf = new OsmPbfReader(PbfPath))
      {
        //long testRelationID = 418713; // Herrnhut (Germany)
        long testRelationID = 62422; // Berlin
        //long testRelationID = 62504; // Brandenburg (Germany) - https://www.openstreetmap.org/relation/62504
        //long testRelationID = 1434381; // Insel Rügen (Germany)
        //long testRelationID = 51477; // Germany
        //long testRelationID = 2214684; // La Gomera (Canarias)
        //long testRelationID = 2182003; // Menorca (Balearic Islands)
        //long testRelationID = 11775386; // La Palma (Canarias)

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
          break;
        }

        var ways = pbf.ReadWays2(relations.SelectMany(r => r.members.Where(x => x.type == MemberType.Way).Select(x => x.id)).ToArray());
        Array.Sort(ways, (x, y) => x.id.CompareTo(y.id));

        var nodes = pbf.ReadNodes2(relations.SelectMany(r => r.members.Where(x => x.type == MemberType.Node).Select(x => x.id)).Concat(ways.SelectMany(w => w.nodeIds)).ToArray());
        Array.Sort(nodes, (x, y) => x.id.CompareTo(y.id));

        Console.WriteLine("nodes: {0:N0}", nodes.Length);

        var knownNodes = new HashSet<long>();
        Func<StreamWriter, long, string, int> writePath = null;
        writePath = (wdat, relationId, type) =>
        {
          int count = 0;
          var rel = relations.FirstOrDefault(x => x.id == relationId);
          if (rel.id == 0) return 0;
          foreach (var member in rel.members)
          {
            switch (member.type)
            {
              case MemberType.Node: break; // skip
              case MemberType.Way:
              {
                if (member.role == type)
                {
                  var way = ways.BinarySearchSingle(w => w.id - member.id);
                  foreach (var nodeId in way.nodeIds)
                  {
                    if (knownNodes.Contains(nodeId)) continue;
                    knownNodes.Add(nodeId);
                    var node = nodes.BinarySearchSingle(n => n.id - nodeId);
                    wdat.WriteLine(node.latCode + "," + node.lonCode);
                    count++;
                  }
                }
              } break;
              case MemberType.Relation: count += writePath(wdat, member.id, type); break;
            }
          }
          return count;
        };

        using (var wdat = new StreamWriter(testRelationID + "_outer.txt"))
        {
          writePath(wdat, testRelationID, "outer");
        }
        using (var wdat = new StreamWriter(testRelationID + "_inner.txt"))
        {
          writePath(wdat, testRelationID, "inner");
        }
      }
    }
  }
}
