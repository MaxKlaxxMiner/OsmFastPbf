// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation

// ReSharper disable UnassignedReadonlyField
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace OsmFastPbf
{
  public struct OsmRelationMember
  {
    public readonly long id;
    public readonly MemberType type;
    public readonly string role;
    public OsmRelationMember(long id, MemberType type, string role)
    {
      this.id = id;
      this.type = type;
      this.role = role;
    }
    public override string ToString()
    {
      switch (type)
      {
        case MemberType.Node: return new { type, nodeId = id, role }.ToString();
        case MemberType.Way: return new { type, wayId = id, role }.ToString();
        case MemberType.Relation: return new { type, relationId = id, role }.ToString();
        default: return new { type, memberId = id, role }.ToString();
      }
    }

    public int WriteBinary(byte[] buf, int ofs)
    {
      int p = 0;
      p += ProtoBuf.WriteVarInt(buf, ofs + p, (ulong)id);
      buf[ofs + p] = (byte)type; p++;
      p += ProtoBuf.WriteString(buf, ofs + p, role);
      return p;
    }

    public static int ReadBinary(byte[] buf, int ofs, out OsmRelationMember member)
    {
      int p = 0;
      ulong tmp;
      p += ProtoBuf.ReadVarInt(buf, ofs + p, out tmp);
      long id = (long)tmp;
      var type = (MemberType)buf[ofs + p]; p++;
      string str;
      p += ProtoBuf.ReadString(buf, ofs + p, out str);
      member = new OsmRelationMember(id, type, str);
      return p;
    }
  }
}
