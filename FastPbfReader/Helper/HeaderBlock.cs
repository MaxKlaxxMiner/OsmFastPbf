using System.Collections.Generic;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace OsmFastPbf.Helper
{
  /// <summary>
  /// Struktur eines Header-Blockes
  /// </summary>
  public struct HeaderBlock
  {
    public HeaderBBox bbox;
    public string[] requiredFeatures;
    public string[] optionalFeatures;
    public string writingprogram;
    public string source;
    public long replicationTimestamp;
    public long replicationSequenceNumber;
    public string replicationBaseUrl;

    /// <summary>
    /// Dekodiert die Werte aus einem PBF-Stream
    /// </summary>
    /// <param name="buf">Buffer, worraus die Werte gelesen werden sollen</param>
    /// <param name="ofs">Startposition innerhalb des Buffers</param>
    /// <param name="headerBlock">HeaderBlock-Struktur mit den ausgelesenen Werten</param>
    /// <returns>Anzahl der gelesenen Bytes aus dem Buffer</returns>
    public static int Decode(byte[] buf, int ofs, out HeaderBlock headerBlock)
    {
      /*****
       * message HeaderBlock
       * {
       *   optional HeaderBBox bbox = 1;
       *   
       *   // Additional tags to aid in parsing this dataset
       *   repeated string required_features = 4;
       *   repeated string optional_features = 5;
       *   
       *   optional string writingprogram = 16;
       *   
       *   optional string source = 17; // From the bbox field.
       *   
       *   // Tags that allow continuing an Osmosis replication
       *   // replication timestamp, expressed in seconds since the epoch,
       *   // otherwise the same value as in the "timestamp=..." field
       *   // in the state.txt file used by Osmosis
       *   optional int64 osmosis_replication_timestamp = 32;
       *   
       *   // replication sequence number (sequenceNumber in state.txt)
       *   optional int64 osmosis_replication_sequence_number = 33;
       *   
       *   // replication base URL (from Osmosis' configuration.txt file)
       *   optional string osmosis_replication_base_url = 34;
       * }
       *****/

      int len = 0;
      ulong tmp;
      var tmps = new List<string>();

      headerBlock = new HeaderBlock();

      // --- optional HeaderBBox bbox = 1; ---
      if (buf[ofs + len] == (1 << 3 | 2))
      {
        len++;
        len += HeaderBBox.Decode(buf, ofs + len, out headerBlock.bbox);
      }

      // --- repeated string required_features = 4; ---
      tmps.Clear();
      while (buf[ofs + len] == (4 << 3 | 2))
      {
        len++;
        string feature;
        len += ProtoBuf.ReadString(buf, ofs + len, out feature);
        tmps.Add(feature);
        switch (feature)
        {
          case "OsmSchema-V0.6": break;
          case "DenseNodes": break;
          default: throw new PbfParseException("required feature not supported: \"" + feature + "\"");
        }
      }
      headerBlock.requiredFeatures = tmps.ToArray();

      // --- repeated string optional_features = 5; ---
      tmps.Clear();
      while (buf[ofs + len] == (5 << 3 | 2))
      {
        len++;
        string feature;
        len += ProtoBuf.ReadString(buf, ofs + len, out feature);
        tmps.Add(feature);
        switch (feature)
        {
          case "Has_Metadata": break;
          case "Sort.Type_then_ID": break;
          default: throw new PbfParseException("optional feature not supported: \"" + feature + "\"");
        }
      }
      headerBlock.optionalFeatures = tmps.ToArray();

      // --- optional string writingprogram = 16; ---
      if (ProtoBuf.PeekVarInt(buf, ofs + len) == (16 << 3 | 2))
      {
        len += 2;
        len += ProtoBuf.ReadString(buf, ofs + len, out headerBlock.writingprogram);
      }
      else headerBlock.writingprogram = "";

      // --- optional string source = 17; ---
      if (ProtoBuf.PeekVarInt(buf, ofs + len) == (17 << 3 | 2))
      {
        len += 2;
        len += ProtoBuf.ReadString(buf, ofs + len, out headerBlock.source);
      }
      else headerBlock.source = "";

      // --- optional int64 osmosis_replication_timestamp = 32; ---
      if (ProtoBuf.PeekVarInt(buf, ofs + len) == (32 << 3 | 0))
      {
        len += 2;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        headerBlock.replicationTimestamp = (long)tmp;
      }

      // --- optional int64 osmosis_replication_sequence_number = 33; ---
      if (ProtoBuf.PeekVarInt(buf, ofs + len) == (33 << 3 | 0))
      {
        len += 2;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        headerBlock.replicationSequenceNumber = (long)tmp;
      }

      // --- optional string osmosis_replication_base_url = 34; ---
      if (ProtoBuf.PeekVarInt(buf, ofs + len) == (34 << 3 | 2))
      {
        len += 2;
        len += ProtoBuf.ReadString(buf, ofs + len, out headerBlock.replicationBaseUrl);
      }
      else headerBlock.replicationBaseUrl = "";

      return len;
    }

    /// <summary>
    /// gibt den Inhalt als lesbare Zeichenkette zurück
    /// </summary>
    /// <returns>lesbare Zeichenkette</returns>
    public override string ToString()
    {
      return new
      {
        bbox,
        requiredFeatures = "string[" + requiredFeatures.Length + "]",
        optionalFeatures = "string[" + optionalFeatures.Length + "]",
        writingprogram,
        source,
        replicationTimestamp,
        replicationSequenceNumber,
        replicationBaseUrl
      }.ToString();
    }
  }
}
