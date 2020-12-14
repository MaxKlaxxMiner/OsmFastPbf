#region # using *.*
// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf.Helper;

// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedField.Local
#endregion

namespace OsmFastPbf
{
  /// <summary>
  /// Klasse zum handlichen Auslesen von Openstreetmap-Daten aus einer PBF-Datei
  /// </summary>
  public class OsmPbfReader : IDisposable
  {
    #region # // --- Felder + Cache ---
    /// <summary>
    /// merkt sich den Index auf die Blöcke mit den OSM-Knoten
    /// </summary>
    readonly OsmBlob[] nodeIndex;
    /// <summary>
    /// merkt sich den Index auf die Blöcke mit den OSM-Wegen
    /// </summary>
    readonly OsmBlob[] wayIndex;
    /// <summary>
    /// merkt sich den Index auf die Blöcke mit den OSM-Relationen
    /// </summary>
    readonly OsmBlob[] relationIndex;
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
    public OsmPbfReader(string planetFilePbf, int maxCacheMByte = 1024)
    {
      var index = PbfFastScan.ReadIndex(planetFilePbf, false);
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

      if (cache[minIndex].pbfOffset == -1)
      {
        Console.Title = "Cache: " + (minIndex + 1) + " / " + cache.Length;
      }

      // --- Cache aktualisieren ---
      cache[minIndex].pbfOffset = blob.pbfOfs;
      cache[minIndex].lastUseTime = Environment.TickCount;

      return outputBuf;
    }
    #endregion

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

    /// <summary>
    /// liest mehrere OSM-Knoten ein und gibt die Ergebnisse in entsprechender Reihenfolge zurück
    /// </summary>
    /// <param name="nodeIds">Knoten-IDs, welche abgefragt werden sollen</param>
    /// <returns>Array mit den abgefragten Knoten</returns>
    public OsmNode[] ReadNodes(params long[] nodeIds)
    {
      var result = new OsmNode[nodeIds.Length];

      var searchNodes = new HashSet<long>(nodeIds).ToArray();
      Array.Sort(searchNodes);



      return null;
    }
  }
}
