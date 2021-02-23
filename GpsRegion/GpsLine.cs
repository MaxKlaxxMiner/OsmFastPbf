// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedType.Global
namespace OsmFastPbf
{
  public struct GpsLine
  {
    public GpsPos pos1;
    public GpsPos pos2;
    public GpsLine(GpsPos pos1, GpsPos pos2)
    {
      this.pos1 = pos1;
      this.pos2 = pos2;
    }
  }
}
