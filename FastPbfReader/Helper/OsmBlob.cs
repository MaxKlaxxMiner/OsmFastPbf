using System;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace OsmFastPbf.Helper
{
  /// <summary>
  /// Struktur eines OSM-Blocks
  /// </summary>
  public struct OsmBlob
  {
    /// <summary>
    /// globale Position innerhalb der PBF-Datei
    /// </summary>
    public long pbfOfs;
    /// <summary>
    /// Anzahl belegter Bytes innerhalb der PBF-Datei
    /// </summary>
    public int blobLen;
    /// <summary>
    /// Offset der komprimierten Daten innerhalb des Blocks
    /// </summary>
    public int zlibOfs;
    /// <summary>
    /// Länge der komprimierten Daten innerhalb des Blocks
    /// </summary>
    public int zlibLen;
    /// <summary>
    /// Länge der unkomprimierten Daten
    /// </summary>
    public int rawSize;

    /// <summary>
    /// handelt es sich um einen Header-Block?
    /// </summary>
    public bool IsHeader { get { return pbfOfs == 0; } }

    /// <summary>
    /// dekodiert nur den Header eines Blockes (benötigt mindestens 32 Bytes)
    /// </summary>
    /// <param name="buf">Buffer, welcher ausgelesen werden soll</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <param name="result">fertig gelesenes Ergebnis</param>
    /// <returns>Größe des gesamten Blockes in Bytes</returns>
    public static int DecodeQuick(byte[] buf, int ofs, out OsmBlob result)
    {
      /*****
       * message BlobHeader
       * {
       *   required string type = 1;
       *   optional bytes indexdata = 2;
       *   required int32 datasize = 3;
       * }
       *****/

      int len = 0;
      int blobHeaderLen;
      len += ProtoBuf.ReadInt32Fix(buf, ofs + len, out blobHeaderLen);
      result = new OsmBlob();

      // --- required string type = 1; ---
      string type;
      if (buf[ofs + len++] != (1 << 3 | 2)) throw new PbfParseException();
      len += ProtoBuf.ReadString(buf, ofs + len, out type);

      // --- optional bytes indexdata = 2; ---
      if (buf[ofs + len] == (2 << 3 | 2))
      {
        throw new NotImplementedException();
      }

      // --- required int32 datasize = 3; ---
      if (buf[ofs + len++] != (3 << 3 | 0)) throw new PbfParseException();
      ulong tmp;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      result.blobLen = len + (int)tmp;
      if (len - sizeof(int) != blobHeaderLen) throw new PbfParseException();

      /*****
       * message Blob
       * {
       *   optional bytes raw = 1; // keine Kompression
       *   optional int32 raw_size = 2; // Nur gesetzt, wenn komprimiert, auf die unkomprimierte Größe
       *   optional bytes zlib_data = 3;
       *   // optional bytes lzma_data = 4; // GEPLANT.
       *   // optional bytes OBSOLETE_bzip2_data = 5; // Veraltet.
       * }
       *****/

      // --- optional bytes raw = 1; ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        throw new NotSupportedException();
      }

      // --- optional int32 raw_size = 2; ---
      if (buf[ofs + len] == (2 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        result.rawSize = (int)tmp;
      }

      // --- optional bytes zlib_data = 3; ---
      if (buf[ofs + len] == (3 << 3 | 2))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        result.zlibOfs = len;
        result.zlibLen = (int)tmp;
        len += (int)tmp;
      }
      else
      {
        // --- optional bytes lzma_data = 4; ---
        if (buf[ofs + len] == (4 << 3 | 2))
        {
          throw new NotSupportedException();
        }

        // --- optional bytes OBSOLETE_bzip2_data = 5; ---
        if (buf[ofs + len] == (5 << 3 | 2))
        {
          throw new NotSupportedException();
        }
      }

      if (len != result.blobLen) throw new PbfParseException();

      switch (type)
      {
        case "OSMHeader": break;
        case "OSMData": break;
        default: throw new Exception("unknown Type: " + type);
      }

      return len;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { pbfOfs, blobLen, rawSize, zlibOfs, zlibLen }.ToString();
    }
  }
}
