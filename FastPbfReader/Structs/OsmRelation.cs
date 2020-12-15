// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
      if (name != "")
      {
        return new { id, name, values = "[" + values.Length + "]", members = "[" + members.Length + "]" }.ToString();
      }
      return new { id, values = "[" + values.Length + "]", members = "[" + members.Length + "]" }.ToString();
    }
  }
}
