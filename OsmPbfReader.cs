#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
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

    struct CacheElement
    {
      public long pbfOffset;
      public int lastUseTime;
      public byte[] rawData;
    }

    /// <summary>
    /// merkt sich die Cache-Daten
    /// </summary>
    readonly CacheElement[] cache;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="planetFilePbf">PBF-Datei, welche geöffnet werden soll (z.B. planet-latest.osm.pbf)</param>
    /// <param name="maxCacheMByte">maximale Cachegröße in Megabyte</param>
    public OsmPbfReader(string planetFilePbf, int maxCacheMByte = 64)
    {
      var index = PbfFast.ReadIndex(planetFilePbf, false);
      nodeIndex = index.Where(x => x.nodeCount > 0).ToArray();
      wayIndex = index.Where(x => x.wayCount > 0).ToArray();
      relationIndex = index.Where(x => x.relationCount > 0).ToArray();
      int maxBlob = index.Max(x => x.blobLen);
      int maxRaw = index.Max(x => x.rawSize);
      maxBlob = (maxBlob + 4096) / 4096 * 4096;
      maxRaw = (maxRaw + 4096) / 4096 * 4096;
      pbfReader = new FastPbfReader(planetFilePbf, maxBlob);
      cache = new CacheElement[Math.Max(1, (int)(maxCacheMByte * 1048576L / maxRaw))];
      for (int i = 0; i < cache.Length; i++)
      {
        cache[i] = new CacheElement { pbfOffset = -1, lastUseTime = 0, rawData = new byte[maxRaw] };
      }
    }
    #endregion

    #region # --- byte[] FetchBlob(OsmBlob blob) ---
    byte[] FetchBlob(OsmBlob blob)
    {
      long search = blob.pbfOfs;
      for (int i = 0; i < cache.Length; i++)
      {
        if (cache[i].pbfOffset == search)
        {
          cache[i].lastUseTime = Environment.TickCount;
          return cache[i].rawData;
        }
      }

      int min = int.MaxValue;
      int minIndex = 0;
      for (int i = 0; i < cache.Length; i++)
      {
        if (cache[i].lastUseTime < min)
        {
          min = cache[i].lastUseTime;
          minIndex = i;
        }
      }

      // --- laden ---
      int pbfOfs = pbfReader.PrepareBuffer(blob.pbfOfs + blob.zlibOfs, blob.zlibLen);

      // --- entpacken ---
      var outputBuf = cache[minIndex].rawData;
      int bytes = ProtoBuf.FastInflate(pbfReader.buffer, pbfOfs, blob.zlibLen, outputBuf, 0);
      if (bytes != blob.rawSize) throw new PbfParseException();
      outputBuf[bytes] = 0;

      // --- Cache aktualisieren ---
      cache[minIndex].pbfOffset = blob.pbfOfs;
      cache[minIndex].lastUseTime = Environment.TickCount;

      return outputBuf;
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

      var nodeBlob = wayIndex[0]; // dummy-element
      OsmNode[] nodes = null;
      for (int n = 0; n < searchNodes.Length; n++)
      {
        long nodeId = searchNodes[n].Key;

        if (nodeId > nodeBlob.maxNodeId || nodeId < nodeBlob.minNodeId)
        {
          nodeBlob = nodeIndex.BinarySearchSingle(x => nodeId >= x.minNodeId && nodeId <= x.maxNodeId ? 0L : x.minNodeId - nodeId);

          Console.WriteLine("read nodes: {0:N0} / {1:N0}", n + 1, searchNodes.Length);

          var buf = FetchBlob(nodeBlob);
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, nodeBlob, out nodes);
          if (len != nodeBlob.rawSize) throw new PbfParseException();
        }

        result[searchNodes[n].Value] = nodes.BinarySearchSingle(x => x.id - nodeId);
      }

      return result;
    }

    /// <summary>
    /// liest mehrere OSM-Knoten ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// </summary>
    /// <param name="nodeIds">Knoten-IDs, welche abgefragt werden sollen</param>
    /// <returns>Array mit den abgefragten Knoten</returns>
    public OsmNode[] ReadNodes2(params long[] nodeIds)
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

      var wayBlob = nodeIndex[0]; // dummy-element
      OsmWay[] ways = null;
      for (int w = 0; w < searchWays.Length; w++)
      {
        long wayId = searchWays[w].Key;

        if (wayId > wayBlob.maxWayId || wayId < wayBlob.minWayId)
        {
          wayBlob = wayIndex.BinarySearchSingle(x => wayId >= x.minWayId && wayId <= x.maxWayId ? 0L : x.minWayId - wayId);

          Console.WriteLine("read ways: {0:N0} / {1:N0}", w + 1, searchWays.Length);

          var buf = FetchBlob(wayBlob);
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, wayBlob, out ways);
          if (len != wayBlob.rawSize) throw new PbfParseException();
        }

        result[searchWays[w].Value] = ways.BinarySearchSingle(x => x.id - wayId);
      }

      return result;
    }

    IEnumerable<T[]> BlobSmtDecoder<T>(List<OsmBlob> blobs, Func<OsmBlob, byte[], T[]> decode)
    {
      return blobs.SelectParallelEnumerable(blob =>
      {
        // --- lesen ---
        var zlibData = new byte[blob.zlibLen];
        lock (pbfReader)
        {
          int pbfOfs = pbfReader.PrepareBuffer(blob.pbfOfs + blob.zlibOfs, blob.zlibLen);
          Array.Copy(pbfReader.buffer, pbfOfs, zlibData, 0, zlibData.Length);
        }

        // --- entpacken ---
        var buf = new byte[blob.rawSize + 1];
        int bytes = ProtoBuf.FastInflate(zlibData, 0, blob.zlibLen, buf, 0);
        if (bytes != blob.rawSize) throw new PbfParseException();
        buf[bytes] = 0;

        // --- decoden ---
        return decode(blob, buf);
      }, priority: ThreadPriority.Lowest);
    }

    /// <summary>
    /// liest ein oder mehrere Wege ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// </summary>
    /// <param name="wayIds">Wege-IDs, welche abgefragt werden sollen</param>
    /// <returns>Array mit den abgefragten Wegen</returns>
    public OsmWay[] ReadWays2(params long[] wayIds)
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

      var relationBlob = wayIndex[0]; // dummy-element
      OsmRelation[] relations = null;
      for (int r = 0; r < searchRelations.Length; r++)
      {
        long relationId = searchRelations[r].Key;

        if (relationId > relationBlob.maxRelationId || relationId < relationBlob.minRelationId)
        {
          relationBlob = relationIndex.BinarySearchSingle(x => relationId >= x.minRelationId && relationId <= x.maxRelationId ? 0L : x.minRelationId - relationId);

          Console.WriteLine("read relations: {0:N0} / {1:N0}", r + 1, searchRelations.Length);

          var buf = FetchBlob(relationBlob);
          int len = PbfFast.DecodePrimitiveBlock(buf, 0, relationBlob, out relations);
          if (len != relationBlob.rawSize) throw new PbfParseException();
        }

        result[searchRelations[r].Value] = relations.BinarySearchSingle(x => x.id - relationId);
      }

      return result;
    }
    #endregion

    public IEnumerable<OsmNode> ReadAllNodes()
    {
      foreach (var nodeBlob in nodeIndex)
      {
        var buf = FetchBlob(nodeBlob);
        OsmNode[] nodes;
        int len = PbfFast.DecodePrimitiveBlock(buf, 0, nodeBlob, out nodes);
        if (len != nodeBlob.rawSize) throw new PbfParseException();
        foreach (var node in nodes)
        {
          yield return node;
        }
      }
    }
    public IEnumerable<OsmWay> ReadAllWays()
    {
      foreach (var wayBlob in wayIndex)
      {
        var buf = FetchBlob(wayBlob);
        OsmWay[] ways;
        int len = PbfFast.DecodePrimitiveBlock(buf, 0, wayBlob, out ways);
        if (len != wayBlob.rawSize) throw new PbfParseException();
        foreach (var way in ways)
        {
          yield return way;
        }
      }
    }
    public IEnumerable<OsmRelation> ReadAllRelations()
    {
      foreach (var relationBlob in relationIndex)
      {
        var buf = FetchBlob(relationBlob);
        OsmRelation[] relations;
        int len = PbfFast.DecodePrimitiveBlock(buf, 0, relationBlob, out relations);
        if (len != relationBlob.rawSize) throw new PbfParseException();
        foreach (var relation in relations)
        {
          yield return relation;
        }
      }
    }
  }
}
