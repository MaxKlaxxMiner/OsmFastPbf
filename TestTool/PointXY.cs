using System;
using System.Collections.Generic;
using OsmFastPbf;

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

    static readonly char[] SplitChars = new[] { ' ', ',', '\t' };

    public PointXY(string txt, bool swapXY = false)
    {
      var sp = txt.Split(SplitChars, 2, StringSplitOptions.RemoveEmptyEntries);
      if (swapXY)
      {
        x = int.Parse(sp[1]);
        y = int.Parse(sp[0]);
      }
      else
      {
        x = int.Parse(sp[0]);
        y = int.Parse(sp[1]);
      }
    }

    public PointXY(OsmNode node)
    {
      x = node.lonCode;
      y = node.latCode;
    }

    public override string ToString()
    {
      return new { x, y }.ToString();
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
