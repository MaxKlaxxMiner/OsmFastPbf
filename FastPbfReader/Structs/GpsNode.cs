// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System.Collections.Generic;

namespace OsmFastPbf
{
  public struct GpsNode
  {
    public readonly long id;
    public readonly int latCode;
    public readonly int lonCode;
    public readonly KeyValuePair<string, string>[] values;
    public double Latitude { get { return latCode / 10000000.0; } }
    public double Longitude { get { return lonCode / 10000000.0; } }
    public GpsNode(long id, int latCode, int lonCode, KeyValuePair<string, string>[] values)
    {
      this.id = id;
      this.latCode = latCode;
      this.lonCode = lonCode;
      this.values = values;
    }
    public override string ToString()
    {
      return new { id, Latitude, Longitude, values = "values[" + values.Length + "]" }.ToString();
    }
  }
}
