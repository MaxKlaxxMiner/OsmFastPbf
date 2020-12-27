#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OsmFastPbf.Helper;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedField.Local
#endregion

namespace OsmFastPbf
{
  /// <summary>
  /// Klasse zum handlichen Auslesen von Openstreetmap-Daten aus einer PBF-Datei
  /// </summary>
  public sealed class OsmPbfReader : IDisposable
  {
    #region # // --- Felder + Cache ---
    /// <summary>
    /// merkt sich den Index auf die Blöcke mit den OSM-Knoten
    /// </summary>
    public readonly OsmBlob[] nodeIndex;
    /// <summary>
    /// merkt sich den Index auf die Blöcke mit den OSM-Wegen
    /// </summary>
    public readonly OsmBlob[] wayIndex;
    /// <summary>
    /// merkt sich den Index auf die Blöcke mit den OSM-Relationen
    /// </summary>
    public readonly OsmBlob[] relationIndex;
    /// <summary>
    /// merkt sich den geöffneten PBF-Reader
    /// </summary>
    FastPbfReader pbfReader;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="planetFilePbf">PBF-Datei, welche geöffnet werden soll (z.B. planet-latest.osm.pbf)</param>
    public OsmPbfReader(string planetFilePbf)
    {
      var index = PbfFast.ReadIndex(planetFilePbf, false);
      nodeIndex = index.Where(x => x.nodeCount > 0).ToArray();
      wayIndex = index.Where(x => x.wayCount > 0).ToArray();
      relationIndex = index.Where(x => x.relationCount > 0).ToArray();
      pbfReader = new FastPbfReader(planetFilePbf) { RandomBuffering = true };
    }
    #endregion

    #region # // --- Dispose ---
    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      if (pbfReader != null)
      {
        pbfReader.Dispose();
        pbfReader = null;
      }
    }
    #endregion

    #region # // --- SMT Decoder ---
    public static List<long> times = new List<long>();

    static readonly Dictionary<int, byte[]> SmtCache = new Dictionary<int, byte[]>();
    IEnumerable<T[]> BlobSmtDecoder<T>(IList<OsmBlob> blobs, Func<OsmBlob, byte[], T[]> decode)
    {
      return blobs.SelectParallelEnumerable(blob =>
      {
        // --- Thread-Buffer abfragen ---
        int threadId = Thread.CurrentThread.ManagedThreadId;
        byte[] buf;
        lock (SmtCache)
        {
          if (!SmtCache.TryGetValue(threadId, out buf))
          {
            buf = new byte[16777216 * 2];
            SmtCache.Add(threadId, buf);
          }
        }

        // --- lesen ---
        var tim = Stopwatch.StartNew();
        if (pbfReader == null) return new T[0];
        lock (pbfReader)
        {
          int pbfOfs = pbfReader.PrepareBuffer(blob.pbfOfs + blob.zlibOfs, blob.zlibLen);
          var b = pbfReader.buffer;
          if (b == null) return new T[0];
          Array.Copy(b, pbfOfs, buf, 16777216, blob.zlibLen);
        }
        tim.Stop();
        lock (times)
        {
          times.Add(tim.ElapsedMilliseconds);
        }

        // --- entpacken ---
        int bytes = ProtoBuf.FastInflate(buf, 16777216, blob.zlibLen, buf, 0);
        if (bytes != blob.rawSize) throw new PbfParseException();
        buf[bytes] = 0;

        // --- decoden ---
        return decode(blob, buf);
      }, priority: ThreadPriority.Lowest);
    }
    #endregion

    #region # public OsmNode[] ReadNodes(params long[] nodeIds) // liest mehrere OSM-Knoten ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// <summary>
    /// liest mehrere OSM-Knoten ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// </summary>
    /// <param name="nodeIds">Knoten-IDs, welche abgefragt werden sollen</param>
    /// <returns>Array mit den abgefragten Knoten</returns>
    public OsmNode[] ReadNodes(params long[] nodeIds)
    {
      var result = new OsmNode[nodeIds.Length];

      var searchNodes = Enumerable.Range(0, nodeIds.Length).Select(i => new KeyValuePair<long, int>(nodeIds[i], i)).ToArray(nodeIds.Length);
      Array.Sort(searchNodes, (x, y) => x.Key.CompareTo(y.Key));

      var needBlobs = new List<OsmBlob>();
      var lastBlob = wayIndex[0];
      foreach (var nodeId in searchNodes.Select(x => x.Key))
      {
        if (nodeId > lastBlob.maxNodeId || nodeId < lastBlob.minNodeId)
        {
          lastBlob = nodeIndex.BinarySearchSingle(x => nodeId >= x.minNodeId && nodeId <= x.maxNodeId ? 0L : x.minNodeId - nodeId);
          needBlobs.Add(lastBlob);
        }
      }

      var blobDecoder = BlobSmtDecoder(needBlobs, (blob, buf) =>
      {
        try
        {
          OsmNode[] tmp;
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, blob, out tmp);
          if (len != blob.rawSize) throw new PbfParseException();
          return tmp;
        }
        catch
        {
          return null;
        }
      }).GetEnumerator();

      OsmNode[] nodes = null;
      var tmpNodes = new List<OsmNode[]>();
      for (int w = 0; w < searchNodes.Length; w++)
      {
        long nodeId = searchNodes[w].Key;

        if (nodes == null || nodes[nodes.Length - 1].id < nodeId)
        {
          nodes = null;
          for (int i = 0; i < tmpNodes.Count; i++)
          {
            if (nodeId >= tmpNodes[i].First().id && nodeId <= tmpNodes[i].Last().id)
            {
              nodes = tmpNodes[i];
              tmpNodes.RemoveAt(i);
              break;
            }
          }
          while (nodes == null)
          {
            blobDecoder.MoveNext();
            nodes = blobDecoder.Current;
            if (nodeId < nodes.First().id || nodeId > nodes.Last().id)
            {
              tmpNodes.Add(nodes);
              nodes = null;
            }
          }

          Console.WriteLine("read nodes: {0:N0} / {1:N0}", w + 1, searchNodes.Length);
        }

        result[searchNodes[w].Value] = nodes.BinarySearchSingle(x => x.id - nodeId);
      }

      return result;
    }

    /// <summary>
    /// liest alle Knoten aus
    /// </summary>
    /// <returns>Enumerable aller Knoten</returns>
    public IEnumerable<OsmNode> ReadAllNodes()
    {
      var blobDecoder = BlobSmtDecoder(nodeIndex, (blob, buf) =>
      {
        try
        {
          OsmNode[] tmp;
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, blob, out tmp);
          if (len != blob.rawSize) throw new PbfParseException();
          return tmp;
        }
        catch
        {
          return null;
        }
      }).GetEnumerator();

      OsmNode[] nodes = null;
      var tmpNodes = new List<OsmNode[]>();
      foreach (var nodeBlob in nodeIndex)
      {
        if (nodes == null || nodes[0].id != nodeBlob.minNodeId)
        {
          nodes = null;
          for (int i = 0; i < tmpNodes.Count; i++)
          {
            if (tmpNodes[i][0].id == nodeBlob.minNodeId)
            {
              nodes = tmpNodes[i];
              tmpNodes.RemoveAt(i);
              break;
            }
          }
          while (nodes == null)
          {
            blobDecoder.MoveNext();
            nodes = blobDecoder.Current;
            if (nodes[0].id != nodeBlob.minNodeId)
            {
              tmpNodes.Add(nodes);
              nodes = null;
            }
          }
        }

        foreach (var node in nodes)
        {
          yield return node;
        }
      }
    }

    public IEnumerable<OsmNode[]> ReadAllNodes2()
    {
      var blobDecoder = BlobSmtDecoder(nodeIndex, (blob, buf) =>
      {
        try
        {
          OsmNode[] tmp;
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, blob, out tmp);
          if (len != blob.rawSize) throw new PbfParseException();
          return tmp;
        }
        catch
        {
          return null;
        }
      }).GetEnumerator();

      OsmNode[] nodes = null;
      var tmpNodes = new List<OsmNode[]>();
      foreach (var nodeBlob in nodeIndex)
      {
        if (nodes == null || nodes[0].id != nodeBlob.minNodeId)
        {
          nodes = null;
          for (int i = 0; i < tmpNodes.Count; i++)
          {
            if (tmpNodes[i][0].id == nodeBlob.minNodeId)
            {
              nodes = tmpNodes[i];
              tmpNodes.RemoveAt(i);
              break;
            }
          }
          while (nodes == null)
          {
            blobDecoder.MoveNext();
            nodes = blobDecoder.Current;
            if (nodes[0].id != nodeBlob.minNodeId)
            {
              tmpNodes.Add(nodes);
              nodes = null;
            }
          }
        }

        yield return nodes;
      }
    }

    public IEnumerable<OsmNode[]> ReadAllNodes3()
    {
      foreach (var blob in BlobSmtDecoder(nodeIndex, (blob, buf) =>
      {
        OsmNode[] tmp;
        int len = PbfFast.DecodePrimitiveBlock(buf, 0, blob, out tmp);
        if (len != blob.rawSize) throw new PbfParseException();
        return tmp;
      }))
      {
        yield return blob;
      }
    }
    #endregion

    #region # public OsmWay[] ReadWays(params long[] wayIds) // liest ein oder mehrere Wege ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// <summary>
    /// liest ein oder mehrere Wege ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// </summary>
    /// <param name="wayIds">Wege-IDs, welche abgefragt werden sollen</param>
    /// <returns>Array mit den abgefragten Wegen</returns>
    public OsmWay[] ReadWays(params long[] wayIds)
    {
      var result = new OsmWay[wayIds.Length];

      var searchWays = Enumerable.Range(0, wayIds.Length).Select(i => new KeyValuePair<long, int>(wayIds[i], i)).ToArray(wayIds.Length);
      Array.Sort(searchWays, (x, y) => x.Key.CompareTo(y.Key));

      var needBlobs = new List<OsmBlob>();
      var lastBlob = nodeIndex[0];
      foreach (var wayId in searchWays.Select(x => x.Key))
      {
        if (wayId > lastBlob.maxWayId || wayId < lastBlob.minWayId)
        {
          lastBlob = wayIndex.BinarySearchSingle(x => wayId >= x.minWayId && wayId <= x.maxWayId ? 0L : x.minWayId - wayId);
          needBlobs.Add(lastBlob);
        }
      }

      var blobDecoder = BlobSmtDecoder(needBlobs, (blob, buf) =>
      {
        try
        {
          OsmWay[] tmp;
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, blob, out tmp);
          if (len != blob.rawSize) throw new PbfParseException();
          return tmp;
        }
        catch
        {
          return null;
        }
      }).GetEnumerator();

      OsmWay[] ways = null;
      var tmpWays = new List<OsmWay[]>();
      for (int w = 0; w < searchWays.Length; w++)
      {
        long wayId = searchWays[w].Key;

        if (ways == null || ways[ways.Length - 1].id < wayId)
        {
          ways = null;
          for (int i = 0; i < tmpWays.Count; i++)
          {
            if (wayId >= tmpWays[i].First().id && wayId <= tmpWays[i].Last().id)
            {
              ways = tmpWays[i];
              tmpWays.RemoveAt(i);
              break;
            }
          }
          while (ways == null)
          {
            blobDecoder.MoveNext();
            ways = blobDecoder.Current;
            if (wayId < ways.First().id || wayId > ways.Last().id)
            {
              tmpWays.Add(ways);
              ways = null;
            }
          }

          Console.WriteLine("read ways: {0:N0} / {1:N0}", w + 1, searchWays.Length);
        }

        result[searchWays[w].Value] = ways.BinarySearchSingle(x => x.id - wayId);
      }

      return result;
    }
    #endregion

    #region # public OsmRelation[] ReadRelations(params long[] relationIds) // liest ein oder mehrere OSM-Relationen ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// <summary>
    /// liest ein oder mehrere OSM-Relationen ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// </summary>
    /// <param name="relationIds">Relations-IDs, welche abgefragt werden sollen</param>
    /// <returns>abgefragte Relationen</returns>
    public OsmRelation[] ReadRelations(params long[] relationIds)
    {
      var result = new OsmRelation[relationIds.Length];

      var searchRelations = Enumerable.Range(0, relationIds.Length).Select(i => new KeyValuePair<long, int>(relationIds[i], i)).ToArray(relationIds.Length);
      Array.Sort(searchRelations, (x, y) => x.Key.CompareTo(y.Key));

      var needBlobs = new List<OsmBlob>();
      var lastBlob = nodeIndex[0];
      foreach (var relationId in searchRelations.Select(x => x.Key))
      {
        if (relationId > lastBlob.maxRelationId || relationId < lastBlob.minRelationId)
        {
          lastBlob = relationIndex.BinarySearchSingle(x => relationId >= x.minRelationId && relationId <= x.maxRelationId ? 0L : x.minRelationId - relationId);
          needBlobs.Add(lastBlob);
        }
      }

      var blobDecoder = BlobSmtDecoder(needBlobs, (blob, buf) =>
      {
        try
        {
          OsmRelation[] tmp;
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, blob, out tmp);
          if (len != blob.rawSize) throw new PbfParseException();
          return tmp;
        }
        catch
        {
          return null;
        }
      }).GetEnumerator();

      OsmRelation[] relations = null;
      var tmpRelations = new List<OsmRelation[]>();
      for (int r = 0; r < searchRelations.Length; r++)
      {
        long relationId = searchRelations[r].Key;

        if (relations == null || relations[relations.Length - 1].id < relationId)
        {
          relations = null;
          for (int i = 0; i < tmpRelations.Count; i++)
          {
            if (relationId >= tmpRelations[i].First().id && relationId <= tmpRelations[i].Last().id)
            {
              relations = tmpRelations[i];
              tmpRelations.RemoveAt(i);
              break;
            }
          }
          while (relations == null)
          {
            blobDecoder.MoveNext();
            relations = blobDecoder.Current;
            if (relationId < relations.First().id || relationId > relations.Last().id)
            {
              tmpRelations.Add(relations);
              relations = null;
            }
          }

          Console.WriteLine("read relations: {0:N0} / {1:N0}", r + 1, searchRelations.Length);
        }

        result[searchRelations[r].Value] = relations.BinarySearchSingle(x => x.id - relationId);
      }

      return result;
    }
    #endregion
  }
}
