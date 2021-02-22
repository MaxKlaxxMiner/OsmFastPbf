using System.Collections.Generic;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation

// ReSharper disable MemberCanBePrivate.Global

namespace OsmFastPbf
{
  public struct OsmNode
  {
    public readonly long id;
    public readonly int latCode;
    public readonly int lonCode;
    public readonly KeyValuePair<string, string>[] values;
    static readonly KeyValuePair<string, string>[] emptyValues = new KeyValuePair<string, string>[0];
    public double Latitude { get { return latCode / 10000000.0; } }
    public double Longitude { get { return lonCode / 10000000.0; } }
    public OsmNode(long id, int latCode, int lonCode, KeyValuePair<string, string>[] values)
    {
      this.id = id;
      this.latCode = latCode;
      this.lonCode = lonCode;
      this.values = values ?? emptyValues;
    }
    public override string ToString()
    {
      return new { id, Latitude, Longitude, values = "[" + values.Length + "]" }.ToString();
    }

    public int WriteBinary(byte[] buf, int ofs)
    {
      int p = 0;
      p += ProtoBuf.WriteVarInt(buf, ofs + p, (ulong)id);
      p += ProtoBuf.WriteVarInt(buf, ofs + p, ProtoBuf.UnsignedInt32(latCode));
      p += ProtoBuf.WriteVarInt(buf, ofs + p, ProtoBuf.UnsignedInt32(lonCode));
      p += ProtoBuf.WriteVarInt(buf, ofs + p, (uint)values.Length);
      foreach (var val in values)
      {
        p += ProtoBuf.WriteString(buf, ofs + p, val.Key);
        p += ProtoBuf.WriteString(buf, ofs + p, val.Value);
      }
      return p;
    }

    public static int ReadBinary(byte[] buf, int ofs, out OsmNode node)
    {
      int p = 0;
      ulong tmp;
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      long id = (long)tmp;
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      int latCode = ProtoBuf.SignedInt32((uint)tmp);
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      int lonCode = ProtoBuf.SignedInt32((uint)tmp);
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      var values = tmp == 0 ? emptyValues : new KeyValuePair<string, string>[tmp];
      for (int i = 0; i < values.Length; i++)
      {
        string key, val;
        p += ProtoBuf.ReadString(buf, ofs + p, out key);
        p += ProtoBuf.ReadString(buf, ofs + p, out val);
        values[i] = new KeyValuePair<string, string>(key, val);
      }
      node = new OsmNode(id, latCode, lonCode, values);
      return p;
    }
  }
}
