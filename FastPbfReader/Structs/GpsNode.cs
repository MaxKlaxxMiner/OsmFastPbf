// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace OsmFastPbf
{
  public struct GpsNode
  {
    public readonly long id;
    public readonly int latCode;
    public readonly int lonCode;
    public double Latitude { get { return latCode / 10000000.0; } }
    public double Longitude { get { return lonCode / 10000000.0; } }
    public GpsNode(long id, int latCode, int lonCode)
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
