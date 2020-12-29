using System.Collections.Generic;
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
  }
}
