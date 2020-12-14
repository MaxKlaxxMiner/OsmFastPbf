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
    public OsmRelation(long id, KeyValuePair<string, string>[] values)
    {
      this.id = id;
      this.values = values;
    }
    public override string ToString()
    {
      return new { id, values = "[" + values.Length + "]" }.ToString();
    }
  }
}
