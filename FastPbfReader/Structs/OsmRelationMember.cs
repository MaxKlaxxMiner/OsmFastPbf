// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
  }
}
