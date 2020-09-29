
namespace OsmFastPbf
{
  /// <summary>
  /// Struktur einer Lese-Aufgabe eines Blockes
  /// </summary>
  public struct BlobTask
  {
    /// <summary>
    /// absolute Position innerhalb der PBF-Datei
    /// </summary>
    public readonly int pbfBufferOfs;
    /// <summary>
    /// Nummer des Blobs in der PBF-Datei (Header = 0)
    /// </summary>
    public readonly int blobIndex;
    /// <summary>
    /// bekannte Blob-Informationen
    /// </summary>
    public readonly OsmBlob blob;
    /// <summary>
    /// Ausgabeposition zum entpacken der Daten
    /// </summary>
    public readonly int outputOfs;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="pbfBufferOfs">absolute Position innerhalb der PBF-Datei</param>
    /// <param name="blobIndex">Nummer des Blobs in der PBF-Datei (Header = 0)</param>
    /// <param name="blob">bekannte Blob-Informationen</param>
    /// <param name="outputOfs">Ausgabeposition zum entpacken der Daten</param>
    public BlobTask(int pbfBufferOfs, int blobIndex, OsmBlob blob, int outputOfs)
    {
      this.pbfBufferOfs = pbfBufferOfs;
      this.blobIndex = blobIndex;
      this.blob = blob;
      this.outputOfs = outputOfs;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { pbfBufferOfs, outputOfs, blobIndex, blob }.ToString();
    }
  }
}
