#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf;
using OsmFastPbf.Helper;
// ReSharper disable UnusedMethodReturnValue.Global

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

namespace TestTool
{
  public sealed class OsmCache : IDisposable
  {
    const string CacheIndexFile = "osm-cache_index.dat";
    const string CacheDataFile = "osm-cache_data.dat";

    readonly OsmPbfReader pbfReader;
    readonly FileStream cacheData;

    readonly byte[] cacheBuffer = new byte[16777216];

    public OsmCache(OsmPbfReader pbf)
    {
      if (!File.Exists(CacheIndexFile)) File.WriteAllBytes(CacheIndexFile, new byte[0]);
      if (!File.Exists(CacheDataFile)) File.WriteAllBytes(CacheDataFile, new byte[1]);

      // --- Load Index ---
      relationsIndex = new Dictionary<long, long>();
      waysIndex = new Dictionary<long, long>();
      nodesIndex = new Dictionary<long, long>();
      LoadIndex();

      cacheData = new FileStream(CacheDataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      pbfReader = pbf;
    }

    #region # // --- Index ---
    readonly Dictionary<long, long> relationsIndex;
    readonly Dictionary<long, long> waysIndex;
    readonly Dictionary<long, long> nodesIndex;

    void LoadIndex()
    {
      var buf = File.ReadAllBytes(CacheIndexFile);
      for (int p = 0; p < buf.Length; )
      {
        var type = (MemberType)buf[p++];
        ulong id, index;
        p += ProtoBuf.ReadVarInt(buf, p, out id);
        p += ProtoBuf.ReadVarInt(buf, p, out index);
        switch (type)
        {
          case MemberType.Relation: relationsIndex.Add((long)id, (long)index); break;
          case MemberType.Way: waysIndex.Add((long)id, (long)index); break;
          case MemberType.Node: nodesIndex.Add((long)id, (long)index); break;
          default: throw new Exception("unknown type?");
        }
      }
    }

    long GetRelationIndex(long relationID)
    {
      long index;
      return relationsIndex.TryGetValue(relationID, out index) ? index : 0;
    }

    long GetWayIndex(long wayID)
    {
      long index;
      return waysIndex.TryGetValue(wayID, out index) ? index : 0;
    }

    long GetNodeIndex(long nodeID)
    {
      long index;
      return nodesIndex.TryGetValue(nodeID, out index) ? index : 0;
    }

    void WriteIndex(MemberType type, long id, long index)
    {
      switch (type)
      {
        case MemberType.Relation: relationsIndex.Add(id, index); break;
        case MemberType.Way: waysIndex.Add(id, index); break;
        case MemberType.Node: nodesIndex.Add(id, index); break;
        default: throw new Exception("unknown type?");
      }

      byte[] miniBuf = new byte[64];
      int p = 0;
      miniBuf[p] = (byte)type; p++;
      p += ProtoBuf.WriteVarInt(miniBuf, p, (ulong)id);
      p += ProtoBuf.WriteVarInt(miniBuf, p, (ulong)index);
      using (var indexFile = File.OpenWrite(CacheIndexFile))
      {
        indexFile.Position = indexFile.Length;
        indexFile.Write(miniBuf, 0, p);
      }
    }
    #endregion

    #region # // --- Helper ---
    static void EncodeDelta(OsmNode[] nodes)
    {
      for (int i = nodes.Length - 1; i > 0; i--)
      {
        nodes[i] = new OsmNode(nodes[i].id - nodes[i - 1].id, nodes[i].latCode - nodes[i - 1].latCode, nodes[i].lonCode - nodes[i - 1].lonCode, nodes[i].values);
      }
    }

    static void DecodeDelta(OsmNode[] nodes)
    {
      for (int i = 1; i < nodes.Length; i++)
      {
        nodes[i] = new OsmNode(nodes[i].id + nodes[i - 1].id, nodes[i].latCode + nodes[i - 1].latCode, nodes[i].lonCode + nodes[i - 1].lonCode, nodes[i].values);
      }
    }

    static void EncodeDelta(OsmWay[] ways)
    {
      for (int i = ways.Length - 1; i > 0; i--)
      {
        var ids = ways[i].nodeIds.ToArray();
        for (int n = ids.Length - 1; n > 0; n--)
        {
          ids[n] = (long)ProtoBuf.UnsignedInt64(ids[n] - ids[n - 1]);
        }
        ways[i] = new OsmWay(ways[i].id - ways[i - 1].id, ways[i].values, ids);
      }
    }

    static void DecodeDelta(OsmWay[] ways)
    {
      for (int i = 1; i < ways.Length; i++)
      {
        var ids = ways[i].nodeIds;
        for (int n = 1; n < ids.Length; n++)
        {
          ids[n] = ProtoBuf.SignedInt64((ulong)ids[n]) + ids[n - 1];
        }
        ways[i] = new OsmWay(ways[i].id + ways[i - 1].id, ways[i].values, ids);
      }
    }
    #endregion

    #region # // --- Relations ---
    OsmRelation ReadRelationCache(long relationIndex, out OsmWay[] osmWays, out OsmNode[] osmNodes)
    {
      var buf = cacheBuffer;
      cacheData.Position = relationIndex;
      cacheData.Read(buf, 0, sizeof(int));

      int size = BitConverter.ToInt32(buf, 0) - sizeof(int);
      if (cacheData.Read(buf, sizeof(int), size) != size) throw new IOException("EOF?");

      int p = sizeof(int);
      OsmRelation osmRelation;
      p += OsmRelation.ReadBinary(buf, p, out osmRelation);
      ulong tmp;
      p += ProtoBuf.ReadVarInt(buf, p, out tmp);
      osmWays = new OsmWay[tmp];
      for (int i = 0; i < osmWays.Length; i++)
      {
        p += OsmWay.ReadBinary(buf, p, out osmWays[i]);
      }
      DecodeDelta(osmWays);
      p += ProtoBuf.ReadVarInt(buf, p, out tmp);
      osmNodes = new OsmNode[tmp];
      for (int i = 0; i < osmNodes.Length; i++)
      {
        p += OsmNode.ReadBinary(buf, p, out osmNodes[i]);
      }
      DecodeDelta(osmNodes);
      if (p != size + sizeof(int)) throw new PbfParseException();

      return osmRelation;
    }

    long WriteRelationCache(OsmRelation osmRelation, OsmWay[] osmWays, OsmNode[] osmNodes)
    {
      long relationIndex = cacheData.Length;

      int p = sizeof(int); // cacheblock-size
      var buf = cacheBuffer;
      p += osmRelation.WriteBinary(buf, p);
      p += ProtoBuf.WriteVarInt(buf, p, (ulong)osmWays.Length);
      var tmpWays = osmWays.ToArray();
      EncodeDelta(tmpWays);
      foreach (var way in tmpWays)
      {
        p += way.WriteBinary(buf, p);
      }
      p += ProtoBuf.WriteVarInt(buf, p, (ulong)osmNodes.Length);
      var tmpNodes = osmNodes.ToArray();
      EncodeDelta(tmpNodes);
      foreach (var node in tmpNodes)
      {
        p += node.WriteBinary(buf, p);
      }
      buf[0] = (byte)p;
      buf[1] = (byte)(p >> 8);
      buf[2] = (byte)(p >> 16);
      buf[3] = (byte)(p >> 24);

      cacheData.Position = relationIndex;
      cacheData.Write(buf, 0, p);
      return relationIndex;
    }

    public bool ReadRelation(long relationID, out OsmRelation osmRelation, out OsmWay[] osmWays, out OsmNode[] osmNodes)
    {
      long relationIndex = GetRelationIndex(relationID);
      if (relationIndex > 0) // cache found?
      {
        osmRelation = ReadRelationCache(relationIndex, out osmWays, out osmNodes);
        return true;
      }

      ReadRelationDirect(relationID, out osmRelation, out osmWays, out osmNodes);

      relationIndex = WriteRelationCache(osmRelation, osmWays, osmNodes);
      WriteIndex(MemberType.Relation, relationID, relationIndex);

      return osmRelation.id != 0;
    }

    public bool ReadRelationDirect(long relationID, out OsmRelation osmRelation, out OsmWay[] osmWays, out OsmNode[] osmNodes)
    {
      var rels = pbfReader.ReadRelations(relationID);
      if (rels.Length == 0)
      {
        osmRelation = default(OsmRelation);
        osmWays = new OsmWay[0];
        osmNodes = new OsmNode[0];
        return false;
      }

      osmRelation = rels.First();

      osmWays = pbfReader.ReadWays(osmRelation.members.Where(x => x.type == MemberType.Way).Select(x => x.id).ToArray());
      Array.Sort(osmWays, (x, y) => x.id.CompareTo(y.id));
      int wayCount = 1;
      for (int i = 1; i < osmWays.Length; i++)
      {
        if (osmWays[i - 1].id == osmWays[i].id) continue;
        osmWays[wayCount++] = osmWays[i];
      }
      Array.Resize(ref osmWays, wayCount);

      osmNodes = pbfReader.ReadNodes(osmWays.SelectMany(x => x.nodeIds).Concat(osmRelation.members.Where(x => x.type == MemberType.Node).Select(x => x.id)).ToArray());
      Array.Sort(osmNodes, (x, y) => x.id.CompareTo(y.id));
      int nodeCount = 1;
      for (int i = 1; i < osmNodes.Length; i++)
      {
        if (osmNodes[i - 1].id == osmNodes[i].id) continue;
        osmNodes[nodeCount++] = osmNodes[i];
      }
      Array.Resize(ref osmNodes, nodeCount);

      return true;
    }
    #endregion

    public bool ReadWay(long wayID, out OsmWay osmWay, out OsmNode[] osmNodes)
    {
      long wayIndex = GetWayIndex(wayID);
      if (wayIndex > 0) // cache found?
      {
        //  osmRelation = ReadRelationCache(relationIndex, out osmWays, out osmNodes);
        osmWay = new OsmWay();
        osmNodes = null;
        return true;
      }

      ReadWayDirect(wayID, out osmWay, out osmNodes);

      //relationIndex = WriteRelationCache(osmRelation, osmWays, osmNodes);
      //WriteIndex(MemberType.Relation, relationID, relationIndex);

      //return osmRelation.id != 0;
    }

    public bool ReadWayDirect(long wayID, out OsmWay osmWay, out OsmNode[] osmNodes)
    {
      var ways = pbfReader.ReadWays(wayID);
      if (ways.Length == 0)
      {
        osmWay = default(OsmWay);
        osmNodes = new OsmNode[0];
        return false;
      }

      osmWay = ways.First();

      osmNodes = pbfReader.ReadNodes(osmWay.nodeIds);
      Array.Sort(osmNodes, (x, y) => x.id.CompareTo(y.id));
      int nodeCount = 1;
      for (int i = 1; i < osmNodes.Length; i++)
      {
        if (osmNodes[i - 1].id == osmNodes[i].id) continue;
        osmNodes[nodeCount++] = osmNodes[i];
      }
      Array.Resize(ref osmNodes, nodeCount);

      return true;
    }

    public void Dispose()
    {
      cacheData.Dispose();
    }
  }
}
