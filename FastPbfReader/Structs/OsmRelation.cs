// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation

// ReSharper disable UnusedType.Global
// ReSharper disable UnassignedReadonlyField
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace OsmFastPbf
{
  public struct OsmRelation
  {
    public readonly long id;
    public readonly KeyValuePair<string, string>[] values;
    public readonly OsmRelationMember[] members;
    public OsmRelation(long id, KeyValuePair<string, string>[] values, OsmRelationMember[] members)
    {
      this.id = id;
      this.values = values;
      this.members = members;
    }
    public override string ToString()
    {
      string name = values.FirstOrDefault(x => x.Key == "name").Value;
      if (!string.IsNullOrEmpty(name))
      {
        return new { id, name, values = "[" + values.Length + "]", members = "[" + members.Length + "]" }.ToString();
      }
      return new { id, values = "[" + values.Length + "]", members = "[" + members.Length + "]" }.ToString();
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
      p += ProtoBuf.WriteVarInt(buf, ofs + p, (uint)members.Length);
      foreach (var member in members)
      {
        p += member.WriteBinary(buf, ofs + p);
      }
      return p;
    }

    public static int ReadBinary(byte[] buf, int ofs, out OsmRelation relation)
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
      var members = new OsmRelationMember[tmp];
      for (int i = 0; i < members.Length; i++)
      {
        p += OsmRelationMember.ReadBinary(buf, ofs + p, out members[i]);
      }
      relation = new OsmRelation(id, values, members);
      return p;
    }
  }
}
