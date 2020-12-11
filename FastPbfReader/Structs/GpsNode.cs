// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace OsmFastPbf
{
  public struct GpsNode
  {
    public readonly long id;
    public readonly long latCode;
    public readonly long lonCode;
    public double Latitude { get { return latCode / 10000000.0; } }
    public double Longitude { get { return lonCode / 10000000.0; } }
    public GpsNode(long id, long latCode, long lonCode)
    {
      this.id = id;
      this.latCode = latCode;
      this.lonCode = lonCode;
    }
    public override string ToString()
    {
      return new { id, Latitude, Longitude }.ToString();
    }
  }
}
