
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
    public override string ToString()
    {
      return new { startY, endY, lines = "[" + lines.Length + "]" }.ToString();
    }
  }
}
