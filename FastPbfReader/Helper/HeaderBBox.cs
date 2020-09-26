// ReSharper disable MemberCanBePrivate.Global

namespace OsmFastPbf.Helper
{
  /// <summary>
  /// Struktur einer Boundary-Box
  /// </summary>
  public struct HeaderBBox
  {
    /// <summary>
    /// linke Seite
    /// </summary>
    public long left;
    /// <summary>
    /// rechte Seite
    /// </summary>
    public long right;
    /// <summary>
    /// obere Kante
    /// </summary>
    public long top;
    /// <summary>
    /// untere Kante
    /// </summary>
    public long bottom;

    /// <summary>
    /// Dekodiert die Werte aus einem PBF-Stream
    /// </summary>
    /// <param name="buf">Buffer, worraus die Werte gelesen werden sollen</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <param name="box">BBox-Struktur mit den ausgelesenen Werten</param>
    /// <returns>Anzahl der gelesenen Bytes aus dem Buffer</returns>
    public static int Decode(byte[] buf, int ofs, out HeaderBBox box)
    {
      /*****
       * message HeaderBBox
       * {
       *   required sint64 left = 1;
       *   required sint64 right = 2;
       *   required sint64 top = 3;
       *   required sint64 bottom = 4;
       * }
       *****/

      box = new HeaderBBox();

      ulong tmp;
      int len = ProtoBuf.ReadVarInt(buf, ofs, out tmp);
      int endLen = len + (int)tmp;

      // --- required sint64 left = 1; ---
      if (buf[ofs + len++] != (1 << 3 | 0)) throw new PbfParseException();
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      box.left = ProtoBuf.SignedInt64(tmp);

      // --- required sint64 right = 2; ---
      if (buf[ofs + len++] != (2 << 3 | 0)) throw new PbfParseException();
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      box.right = ProtoBuf.SignedInt64(tmp);

      // --- required sint64 top = 3; ---
      if (buf[ofs + len++] != (3 << 3 | 0)) throw new PbfParseException();
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      box.top = ProtoBuf.SignedInt64(tmp);

      // --- required sint64 bottom = 4; ---
      if (buf[ofs + len++] != (4 << 3 | 0)) throw new PbfParseException();
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
      box.bottom = ProtoBuf.SignedInt64(tmp);

      if (len != endLen) throw new PbfParseException();

      return len;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new { left, right, top, bottom }.ToString();
    }
  }
}
