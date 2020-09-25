using System;
using System.IO;
// ReSharper disable UnusedMember.Global

namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Hochleistungskompressor zum erstellen (komprimieren) von GnuZip-Streams (erstellt gleiche GZip-Daten wie die originale ZLIB)
  /// </summary>
  public sealed class GZipXStream : DeflaterXOutputStream
  {
    /// <summary>
    /// merkt sich den aktuellen CRC-Schlüssel beim packen der Daten
    /// </summary>
    uint crc32;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="baseOutputStream">Stream zum schreiben der komprimierten Daten</param>
    /// <param name="level">Komprimierungstufe (0 - 9) Optional: Level 10, Default: 9</param>
    /// <param name="tooFar">optionale Begrenzung der Suchentfernung (0 - 32767), default: 4096, kleiner = besser für redundante Daten (z.B. Xml), größer = besser für kompakte Daten (z.B. Binary)</param>
    public GZipXStream(Stream baseOutputStream, int level = 9, int tooFar = 4096)
      : base(baseOutputStream, new Deflater(level, tooFar), 4096)
    {
      WriteHeader();
    }

    void WriteHeader()
    {
      int modTime = (int)(DateTime.Now.Ticks / 10000L);  // Ticks give back 100ns intervals
      byte[] gzipHeader =
      {
        /* The two magic bytes */
        31, 139,
        /* The compression type */
        Deflater.Deflated,
        /* The flags (not set) */
        0,
        /* The modification time */
        (byte)modTime, (byte)(modTime >> 8),
        (byte)(modTime >> 16), (byte)(modTime >> 24),
        /* The extra flags */
        0,
        /* The OS type (unknown) */
        255
      };
      baseOutputStream.Write(gzipHeader, 0, gzipHeader.Length);
    }

    /// <summary>
    /// schreibt einen Datenblock in den Stream
    /// </summary>
    /// <param name="buffer">Buffer, welche geschrieben werden soll</param>
    /// <param name="off">Startposition im Buffer</param>
    /// <param name="len">Länge der Daten in Bytes</param>
    public override void Write(byte[] buffer, int off, int len)
    {
      crc32 = Crc32Helper.UpdateCrc32(crc32, buffer, off, len);
      base.Write(buffer, off, len);
    }

    /// <summary>
    /// merkt sich, ob der Stream bereits geschlossen wurde
    /// </summary>
    bool closed;
    /// <summary>
    /// schließt den Stream
    /// </summary>
    public override void Close()
    {
      if (closed) return;
      closed = true;
      Finish();
      baseOutputStream.Close();
    }

    /// <summary>
    /// schreibt das Ende des Streams
    /// </summary>
    public override void Finish()
    {
      base.Finish();
      int totalin = def.TotalIn;
      byte[] gzipFooter = { (byte)crc32, (byte)(crc32 >> 8), (byte)(crc32 >> 16), (byte)(crc32 >> 24), (byte)totalin, (byte)(totalin >> 8), (byte)(totalin >> 16), (byte)(totalin >> 24) };
      baseOutputStream.Write(gzipFooter, 0, gzipFooter.Length);
    }
  }
}
