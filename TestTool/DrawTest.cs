// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OsmFastPbf;

namespace TestTool
{
  partial class Program
  {
    static void ViewPicture(Image image, string title = "", Action<Form, int, int> mouseMove = null)
    {
      var pic = new PictureBox
      {
        Image = image,
        SizeMode = PictureBoxSizeMode.StretchImage,
        Dock = DockStyle.Fill
      };
      var form = new Form
      {
        ClientSize = image.Size,
        Text = title
      };
      form.Controls.Add(pic);
      if (mouseMove != null)
      {
        pic.MouseMove += (sender, e) => mouseMove(form, e.X, e.Y);
      }
      form.ShowDialog();
    }

    static double GpsDistance(double lat1, double lon1, double lat2, double lon2)
    {
      const double GpsRadPi = 0.017453292519943295769236907684886;
      const double GpsLen = 6378000.0;
      double radius1 = lat1 * GpsRadPi;
      double radius2 = lon1 * GpsRadPi;
      double radius3 = lat2 * GpsRadPi;
      double radius4 = lon2 * GpsRadPi;

      double summ = Math.Sin(radius1) * Math.Sin(radius3) + Math.Cos(radius1) * Math.Cos(radius3) * Math.Cos(radius4 - radius2);

      return Math.Acos(summ) * GpsLen;
    }

    static void DrawTest()
    {
      const int Height = 1400;
      const int Padding = 10;
      //const string PointFile = "11775386_outer.txt"; // La Palma
      //const string PointFile = "51477_outer.txt"; // DE
      //const string PointFile = "62422_outer.txt"; // Berlin
      const string PointFile = "2214684_outer.txt";

      var points = File.ReadAllLines(PointFile).Select(x => new PointXY(x, true)).ToArray();
      int minX = points.Min(p => p.x);
      int maxX = points.Max(p => p.x);
      int minY = points.Min(p => p.y);
      int maxY = points.Max(p => p.y);

      double distX = GpsDistance((minY + (maxY - minY) / 2) / 10000000.0, minX / 10000000.0, (minY + (maxY - minY) / 2) / 10000000.0, maxX / 10000000.0);
      double distY = GpsDistance(minY / 10000000.0, 0, maxY / 10000000.0, 0);

      int width = (int)((Height - Padding - Padding) / distY * distX) + Padding + Padding;

      var pic = new Bitmap(width, Height, PixelFormat.Format32bppRgb);
      double mulX = (width - Padding - Padding) / (double)(maxX - minX);
      double mulY = (Height - Padding - Padding) / (double)(maxY - minY);

      foreach (var p in points)
      {
        int x = (int)((p.x - minX) * mulX) + Padding;
        int y = Height - (int)((p.y - minY) * mulY) - Padding;
        pic.SetPixel(x, y, Color.FromArgb(0x0080ff - 16777216));
      }

      ViewPicture(pic, PointFile);
    }

    static bool CheckPoint(long x, long y, long cx1, long cy1, long cx2, long cy2)
    {
      if (cy1 > cy2)
      {
        long t = cx1; cx1 = cx2; cx2 = t;
        t = cy1; cy1 = cy2; cy2 = t;
      }

      y *= 2;
      cy1 = cy1 * 2 + 1;
      cy2 = cy2 * 2 + 1;

      if (y < cy1 || y > cy2) return false;
      if (cx1 > x && cx2 > x) return false;

      long tx = (y - cy1) * (cx2 - cx1) / (cy2 - cy1) + cx1;

      return tx <= x;
    }

    static void DrawTest(OsmNode[] nodes, List<Tuple<OsmNode, OsmNode>> polyLines, List<Tuple<int, int, List<Tuple<OsmNode, OsmNode>>>> stripes)
    {
      const int Height = 1400;
      const int Padding = 10;

      var points = nodes.Select(node => new PointXY(node)).ToArray();
      int minX = points.Min(p => p.x);
      int maxX = points.Max(p => p.x);
      int minY = points.Min(p => p.y);
      int maxY = points.Max(p => p.y);

      double distX = GpsDistance((minY + (maxY - minY) / 2) / 10000000.0, minX / 10000000.0, (minY + (maxY - minY) / 2) / 10000000.0, maxX / 10000000.0);
      double distY = GpsDistance(minY / 10000000.0, 0, maxY / 10000000.0, 0);

      int width = (int)((Height - Padding - Padding) / distY * distX) + Padding + Padding;

      var pic = new Bitmap(width, Height, PixelFormat.Format32bppRgb);
      double mulX = (width - Padding - Padding) / (double)(maxX - minX);
      double mulY = (Height - Padding - Padding) / (double)(maxY - minY);

      var g = Graphics.FromImage(pic);
      //g.FillPolygon(new SolidBrush(Color.FromArgb(0x0080ff - 16777216)), points.Select(p => new Point((int)((p.x - minX) * mulX) + Padding, Height - (int)((p.y - minY) * mulY) - Padding)).ToArray());
      g.DrawPolygon(new Pen(Color.FromArgb(0x0080ff - 16777216)), points.Select(p => new Point((int)((p.x - minX) * mulX) + Padding, Height - (int)((p.y - minY) * mulY) - Padding)).ToArray());

      //ViewPicture(pic, nodes.Length.ToString("N0") + " Lines");

      ViewPicture(pic, nodes.Length.ToString("N0") + " Lines", (form, x, y) =>
      {
        long latCode = (long)((Height - y - Padding) / mulY) + minY;
        long lonCode = (long)((x - Padding) / mulX) + minX;

        int colli = 0;

        var time = Stopwatch.StartNew();

        for (int r = 0; r < 100; r++)
        {
          colli = 0;
          foreach (var stripe in stripes)
          {
            if (latCode >= stripe.Item1 && latCode <= stripe.Item2)
            {
              foreach (var line in stripe.Item3)
              {
                if (CheckPoint(lonCode, latCode, line.Item1.lonCode, line.Item1.latCode, line.Item2.lonCode, line.Item2.latCode)) colli++;
              }
              break;
            }
          }

          //foreach (var line in polyLines)
          //{
          //  if (CheckPoint(lonCode, latCode, line.Item1.lonCode, line.Item1.latCode, line.Item2.lonCode, line.Item2.latCode)) colli++;
          //}
        }

        time.Stop();
        double ms = time.ElapsedTicks * 1000.0 / Stopwatch.Frequency;

        string txt = "lat: " + (latCode / 10000000.0).ToString("N5") + ", lon: " + (lonCode / 10000000.0).ToString("N5") + ", " + ms.ToString("N2") + " ms, colli: " + colli + (colli % 2 == 1 ? " #####" : "");

        File.AppendAllText(@"C:\Users\Max\Desktop\prog\vacaVista\dummytest\log.txt", txt + "\r\n");

        form.Text = txt;
      });
    }
  }
}
