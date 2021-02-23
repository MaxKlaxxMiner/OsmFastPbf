
namespace OsmFastPbf
{
  public class GpsStripe
  {
    public readonly uint startY;
    public readonly uint endY;
    public readonly GpsLine[] lines;
    public GpsStripe(uint startY, uint endY, GpsLine[] lines)
    {
      this.startY = startY;
      this.endY = endY;
      this.lines = lines;
    }
  }
}
