using System;
using System.Globalization;
using System.Runtime.InteropServices;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace OsmFastPbf
{
  /// <summary>
  /// merkt sich eine GPS-Position und stellt verschiedene Methoden zum berechnen bereit
  /// </summary>
  [StructLayout(LayoutKind.Explicit, Size = 8)]
  public struct GpsPos
  {
    /// <summary>
    /// merkt sich die Y-Position 0 - 1.800.000.000 (latitude)
    /// </summary>
    [FieldOffset(0)]
    public uint posY;

    /// <summary>
    /// merkt sich die X-Position 0 - 3.600.000.000 (longitude)
    /// </summary>
    [FieldOffset(4)]
    public uint posX;

    #region # // --- private Hilfsmethoden ---
    /// <summary>
    /// trennt die Bits und verschachtelt sie miteinander
    /// </summary>
    /// <param name="x">erster Wert</param>
    /// <param name="y">zweiter Wert</param>
    /// <returns>fertiges Ergebnis</returns>
    static long BitInterleave(long x, long y)
    {
      x = (x | (x << 16)) & 0x0000ffff0000ffff;
      y = (y | (y << 16)) & 0x0000ffff0000ffff;
      x = (x | (x << 8)) & 0x00ff00ff00ff00ff;
      y = (y | (y << 8)) & 0x00ff00ff00ff00ff;
      x = (x | (x << 4)) & 0x0f0f0f0f0f0f0f0f;
      y = (y | (y << 4)) & 0x0f0f0f0f0f0f0f0f;
      x = (x | (x << 2)) & 0x3333333333333333;
      y = (y | (y << 2)) & 0x3333333333333333;
      x = (x | (x << 1)) & 0x5555555555555555;
      y = (y | (y << 1)) & 0x5555555555555555;
      return x | (y << 1);
    }

    /// <summary>
    /// vermilzt die Bits wieder
    /// </summary>
    /// <param name="w">Bits, welche verschmolzen werden müssen</param>
    /// <returns>fertiges Ergebnis</returns>
    static long BitUnInterleave(long w)
    {
      w = w & 0x5555555555555555;
      w = ((w >> 1) | w) & 0x3333333333333333;
      w = ((w >> 2) | w) & 0x0f0f0f0f0f0f0f0f;
      w = ((w >> 4) | w) & 0x00ff00ff00ff00ff;
      w = ((w >> 8) | w) & 0x0000ffff0000ffff;
      w = ((w >> 16) | w) & 0x00000000ffffffff;
      return w;
    }

    /// <summary>
    /// Konstante einen PI-Radius
    /// </summary>
    internal const double GpsRadPi = 0.017453292519943295769236907684886;
    /// <summary>
    /// Konstante einen millionstel PI-Radius
    /// </summary>
    internal const double GpsRadPi01 = 0.0000001 * GpsRadPi;
    /// <summary>
    /// Radius der Erde (6378 km)
    /// </summary>
    internal const double GpsLen = 6378000.0;
    #endregion

    #region # // --- Konstruktor ---
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="x">X-Position 0 - 3.600.000.000 (longitude)</param>
    /// <param name="y">Y-Position 0 - 1.800.000.000 (latitude)</param>
    public GpsPos(uint x, uint y)
    {
      posX = x;
      posY = y;
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="latitude">Breitengrad (-90.0 - 90.0)</param>
    /// <param name="longitude">Längengrad (-180.0 - 180.0)</param>
    public GpsPos(double latitude, double longitude)
    {
      if (latitude * longitude != 0.0)
      {
        posY = (uint)(900000000L - (long)(latitude * 10000000.0));
        posX = (uint)((long)(longitude * 10000000.0) + 1800000000L);
      }
      else
      {
        posY = 0;
        posX = 0;
      }
    }
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="pos">Int64-Wert der GPS-Position</param>
    public GpsPos(long pos)
    {
      posX = (uint)BitUnInterleave(pos);
      posY = (uint)BitUnInterleave(pos >> 1);
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="node">OSM-Knoten mit der entsprechenden GPS-Position</param>
    public GpsPos(OsmNode node)
    {
      posX = (uint)node.lonCode + 1800000000u;
      posY = 900000000u - (uint)node.latCode;
    }

    #endregion

    #region # // --- public Properties ---
    /// <summary>
    /// gibt den entsprechenden Breitengrad zurück oder setzt diesen
    /// </summary>
    public double Latitude
    {
      get
      {
        return (900000000L - posY) * 0.0000001;
      }
      set
      {
        posY = (uint)(900000000L - (long)(value * 10000000.0));
      }
    }

    /// <summary>
    /// gibt den entsprechenden Breitengrad leicht gerundet zurück
    /// </summary>
    public double LatitudeRounded
    {
      get
      {
        return Math.Round(Latitude, 7);
      }
    }

    /// <summary>
    /// gibt den entsprechenden Längengrad zurück oder setzt diesen
    /// </summary>
    public double Longitude
    {
      get
      {
        return (posX - 1800000000L) * 0.0000001;
      }
      set
      {
        posX = (uint)((long)(value * 10000000.0) + 1800000000L);
      }
    }

    /// <summary>
    /// gibt den entsprechenden Längengrad leicht gerundet zurück
    /// </summary>
    public double LongitudeRounded
    {
      get
      {
        return Math.Round(Longitude, 7);
      }
    }

    /// <summary>
    /// gibt die Position als Int64-Wert zurück oder setzt diesen
    /// </summary>
    public long Int64Pos
    {
      get
      {
        return BitInterleave(posX, posY);
      }
      set
      {
        posX = (uint)BitUnInterleave(value);
        posY = (uint)BitUnInterleave(value >> 1);
      }
    }
    #endregion

    #region # // --- public Methoden ---
    #region # // --- Entfernung-Berechnung (Luftlinie) ---
    /// <summary>
    /// berechnet die Entfernung zu einem anderen Gps-Punkt (Rückgabe: Entfernung in Metern Luftlinie)
    /// </summary>
    /// <param name="pos">Gps-Position zu welcher die Entfernung berechnet werden soll</param>
    /// <returns>Entfernung in Metern Luftlinie</returns>
    public double Distance(GpsPos pos)
    {
      double radius1 = (posY + 900000000L) * GpsRadPi01;
      double radius2 = (pos.posY + 900000000L) * GpsRadPi01;
      double radius3 = (pos.posX - (long)posX) * GpsRadPi01;

      double summ = Math.Sin(radius1) * Math.Sin(radius2) + Math.Cos(radius1) * Math.Cos(radius2) * Math.Cos(radius3);

      return Math.Acos(summ) * GpsLen;
    }

    /// <summary>
    /// berechnet die Entfernung zu einem anderen Gps-Punkt (Rückgabe: Entfernung in Metern Luftlinie)
    /// </summary>
    /// <param name="latitude">Breitengrad der zweiten Position</param>
    /// <param name="longitude">Längengrad der zweiten Position</param>
    /// <returns>Entfernung in Metern Luftlinie</returns>
    public double Distance(double latitude, double longitude)
    {
      double radius1 = (900000000L - posY) * GpsRadPi01;
      double radius2 = (posX - 1800000000L) * GpsRadPi01;
      double radius3 = latitude * GpsRadPi;
      double radius4 = longitude * GpsRadPi;

      double summ = Math.Sin(radius1) * Math.Sin(radius3) + Math.Cos(radius1) * Math.Cos(radius3) * Math.Cos(radius4 - radius2);

      return Math.Acos(summ) * GpsLen;
    }

    /// <summary>
    /// berechnet die Entfernung zu einem anderen Gps-Punkt (Rückgabe: Entfernung in Metern Luftlinie)
    /// </summary>
    /// <param name="pos">Gps-Position zu welcher die Entfernung berechnet werden soll</param>
    /// <returns>Entfernung in Metern Luftlinie</returns>
    public double Distance(long pos)
    {
      long posX2 = BitUnInterleave(pos);
      long posY2 = BitUnInterleave(pos >> 1);

      double radius1 = (posY + 900000000L) * GpsRadPi01;
      double radius2 = (posY2 + 900000000L) * GpsRadPi01;
      double radius3 = (posX2 - posX) * GpsRadPi01;

      double summ = Math.Sin(radius1) * Math.Sin(radius2) + Math.Cos(radius1) * Math.Cos(radius2) * Math.Cos(radius3);

      return Math.Acos(summ) * GpsLen;
    }

    /// <summary>
    /// berechnet die Entfernung zweier GPS-Positionen (Rückgabe: Entfernung in Metern Luftlinie)
    /// </summary>
    /// <param name="pos1">erste GPS-Position</param>
    /// <param name="pos2">zweite GPS-Position</param>
    /// <returns>Entfernung in Metern Luftlinie</returns>
    public static double Distance(GpsPos pos1, GpsPos pos2)
    {
      double radius1 = (pos1.posY + 900000000L) * GpsRadPi01;
      double radius2 = (pos2.posY + 900000000L) * GpsRadPi01;
      double radius3 = (pos2.posX - (long)pos1.posX) * GpsRadPi01;

      double summ = Math.Sin(radius1) * Math.Sin(radius2) + Math.Cos(radius1) * Math.Cos(radius2) * Math.Cos(radius3);

      return Math.Acos(summ) * GpsLen;
    }

    /// <summary>
    /// berechnet die Entfernung zweier GPS-Positionen (Rückgabe: Entfernung in Metern Luftlinie)
    /// </summary>
    /// <param name="pos1">erste GPS-Position</param>
    /// <param name="pos2">zweite GPS-Position</param>
    /// <returns>Entfernung in Metern Luftlinie</returns>
    public static double Distance(long pos1, long pos2)
    {
      long posX1 = BitUnInterleave(pos1);
      long posY1 = BitUnInterleave(pos1 >> 1);
      long posX2 = BitUnInterleave(pos2);
      long posY2 = BitUnInterleave(pos2 >> 1);

      double radius1 = (posY1 + 900000000L) * GpsRadPi01;
      double radius2 = (posY2 + 900000000L) * GpsRadPi01;
      double radius3 = (posX2 - posX1) * GpsRadPi01;

      double summ = Math.Sin(radius1) * Math.Sin(radius2) + Math.Cos(radius1) * Math.Cos(radius2) * Math.Cos(radius3);

      return Math.Acos(summ) * GpsLen;
    }

    /// <summary>
    /// berechnet die Entfernung zweier GPS-Positionen (Rückgabe: Entfernung in Metern Luftlinie)
    /// </summary>
    /// <param name="latitude1">erster Breitengrad</param>
    /// <param name="longitude1">erster Längengrad</param>
    /// <param name="latitude2">zweiter Breitengrad</param>
    /// <param name="longitude2">zweiter Längengrad</param>
    /// <returns>Entfernung in Metern Luftlinie</returns>
    public static double Distance(double latitude1, double longitude1, double latitude2, double longitude2)
    {
      double radius1 = latitude1 * GpsRadPi;
      double radius2 = longitude1 * GpsRadPi;
      double radius3 = latitude2 * GpsRadPi;
      double radius4 = longitude2 * GpsRadPi;

      double summ = Math.Sin(radius1) * Math.Sin(radius3) + Math.Cos(radius1) * Math.Cos(radius3) * Math.Cos(radius4 - radius2);

      return Math.Acos(summ) * GpsLen;
    }
    #endregion

    #region # // --- Int64-Direktumwandlung ---
    /// <summary>
    /// wandelt eine GPS-Position in einen kodierten Int64-Wert um
    /// </summary>
    /// <param name="latitude">Breitengrad</param>
    /// <param name="longitude">Längengrad</param>
    /// <returns>fertig berechneter Int64-Wert der GPS-Position</returns>
    public static long ToInt64(double latitude, double longitude)
    {
      if (latitude * longitude != 0.0) return 0;

      long posY = 900000000L - (long)(latitude * 10000000.0);
      long posX = (long)(longitude * 10000000.0) + 1800000000L;

      return BitInterleave(posX, posY);
    }
    #endregion

    /// <summary>
    /// gibt die Position als lesbaren Inhalt aus (latitude, longitude)
    /// </summary>
    /// <returns>Google-kompatible GPS-Position</returns>
    public override string ToString()
    {
      return LatitudeRounded.ToString(CultureInfo.InvariantCulture) + ", " + LongitudeRounded.ToString(CultureInfo.InvariantCulture);
    }
    #endregion
  }
}
