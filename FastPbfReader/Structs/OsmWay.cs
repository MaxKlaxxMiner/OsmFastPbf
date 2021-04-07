// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace OsmFastPbf
{
  public struct OsmWay
  {
    public readonly long id;
    public readonly KeyValuePair<string, string>[] values;
    public readonly long[] nodeIds;
    public OsmWay(long id, KeyValuePair<string, string>[] values, long[] nodeIds)
    {
      this.id = id;
      this.values = values;
      this.nodeIds = nodeIds;
    }
    public override string ToString()
    {
      return new { id, values = "[" + values.Length + "]", nodeIds = "[" + nodeIds.Length + "]" }.ToString();
    }

    public int WriteBinary(byte[] buf, int ofs)
    {
      int p = 0;
      p += ProtoBuf.WriteVarInt(buf, ofs + p, (ulong)id);
      p += ProtoBuf.WriteVarInt(buf, ofs + p, (uint)values.Length);
      foreach (var val in values)
      {
        p += ProtoBuf.WriteString(buf, ofs + p, val.Key);
        p += ProtoBuf.WriteString(buf, ofs + p, val.Value);
      }
      p += ProtoBuf.WriteVarInt(buf, ofs + p, (uint)nodeIds.Length);
      foreach (var nodeId in nodeIds)
      {
        p += ProtoBuf.WriteVarInt(buf, ofs + p, (ulong)nodeId);
      }
      return p;
    }

    public static int ReadBinary(byte[] buf, int ofs, out OsmWay way)
    {
      int p = 0;
      ulong tmp;
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      long id = (long)tmp;
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      var values = new KeyValuePair<string, string>[tmp];
      for (int i = 0; i < values.Length; i++)
      {
        string key, val;
        p += ProtoBuf.ReadString(buf, ofs + p, out key);
        p += ProtoBuf.ReadString(buf, ofs + p, out val);
        values[i] = new KeyValuePair<string, string>(key, val);
      }
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      way = new OsmWay(id, values, new long[tmp]);
      for (int i = 0; i < way.nodeIds.Length; i++)
      {
        p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
        way.nodeIds[i] = (long)tmp;
      }
      return p;
    }
  }
}
