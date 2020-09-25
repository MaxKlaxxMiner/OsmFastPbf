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
    public int dataZipOfs;
    /// <summary>
    /// Länge der komprimierten Daten innerhalb des Blocks
    /// </summary>
    public int dataZipLen;
    /// <summary>
    /// Länge der unkomprimierten Daten
    /// </summary>
    public int dataLen;

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
      int len = 0;
      int blobHeaderLen;
      string type;
      uint datasize;
      len += ProtoBuf.ReadInt32Fix(buf, ofs + len, out blobHeaderLen);
      len += ProtoBuf.ReadString(buf, ofs + len, out type);
      len += ProtoBuf.ReadUInt32(buf, ofs + len, out datasize);
      int blobLength = len + (int)datasize;
      long rawSize;
      len += ProtoBuf.ReadInt64(buf, ofs + len, out rawSize);
      long compressedSize;
      len += ProtoBuf.ReadEmbeddedLength(buf, ofs + len, out compressedSize);

      switch (type)
      {
        case "OSMHeader": break;
        case "OSMData": break;
        default: throw new Exception("unknown Type: " + type);
      }

      result = new OsmBlob
      {
        pbfOfs = 0, // wird später überschrieben
        blobLen = blobLength,
        dataZipOfs = len,
        dataZipLen = checked((int)compressedSize),
        dataLen = checked((int)rawSize)
      };

      return blobLength;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { pbfOfs, blobLen, dataLen, dataZipOfs, dataZipLen }.ToString();
    }
  }
}
