using System.Collections.Generic;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace TestTool
{
  public struct PointXY
  {
    public int x;
    public int y;

    public PointXY(int x, int y)
    {
      this.x = x;
      this.y = y;
    }

    public static IEnumerable<PointXY> ZigZag(int size)
    {
      int x = 0;
      int y = 0;

      while (y < size)
      {
        // --- up ---
        while (y >= 0)
        {
          yield return new PointXY(x, y);
          x++; y--;
        }
        y++;

        if (x == size) break;

        // --- down ---
        while (x >= 0)
        {
          yield return new PointXY(x, y);
          x--; y++;
        }
        x++;
      }

      if (x == size)
      {
        x--; y++;
        while (y < size)
        {
          yield return new PointXY(x, y);
          x--; y++;
        }
        x++;
      }
      x++; y--;

      while (x < size)
      {
        // --- up ---
        while (x < size)
        {
          yield return new PointXY(x, y);
          x++; y--;
        }
        x--; y += 2;

        // --- down ---
        while (y < size)
        {
          yield return new PointXY(x, y);
          x--; y++;
        }
        x += 2; y--;
      }
    }
  }
}
