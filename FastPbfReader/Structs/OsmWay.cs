// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
  }
}
