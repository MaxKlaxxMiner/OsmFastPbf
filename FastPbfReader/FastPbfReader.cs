using System;
using System.IO;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassCanBeSealed.Global
#pragma warning disable 414

namespace OsmFastPbf
{
  /// <summary>
  /// Klasse zum Einlesen von Open Streetmap PBF-Dateien
  /// </summary>
  public class FastPbfReader : IDisposable
  {
    /// <summary>
    /// merkt sich den geöffneten Stream zur PBF-Datei
    /// </summary>
    readonly FileStream pbfStream;
    /// <summary>
    /// merkt sich die gesamte Größe der PBF-Datei
    /// </summary>
    public readonly long pbfSize;

    /// <summary>
    /// maximale Größe des Buffers
    /// </summary>
    const int MaxBuffer = 1048576 * 16; // 16 MByte

    /// <summary>
    /// Buffer-Position innerhalb des Streams
    /// </summary>
    long bufferStreamPos;
    /// <summary>
    /// Lesebuffer für die PBF-Datei
    /// </summary>
    public readonly byte[] buffer;

    /// <summary>
    /// aktualisiert den Inhalt des Lesebuffers
    /// </summary>
    /// <param name="pbfPos">gewünschte Position im Stream, womit die Daten im Buffer beginnen sollen</param>
    void UpdateBuffer(long pbfPos)
    {
      if (pbfPos > pbfSize - buffer.Length) pbfPos = pbfSize - buffer.Length; // verhindern, dass hinter dem Buffer gelesen wird

      if (pbfPos < bufferStreamPos + buffer.Length & pbfPos >= bufferStreamPos) // ein Teilbuffer kann weiter verwendet werden?
      {
        long midPos = pbfPos - bufferStreamPos;
        long midLen = buffer.LongLength - midPos;
        bufferStreamPos = pbfPos;
        Array.Copy(buffer, midPos, buffer, 0, midLen);
        pbfStream.Seek(bufferStreamPos + midLen, SeekOrigin.Begin);
        if (pbfStream.Read(buffer, (int)midLen, buffer.Length - (int)midLen) != buffer.Length - (int)midLen) throw new IOException();
        return;
      }

      // --- vollständig neu den Buffer befüllen ---
      bufferStreamPos = pbfPos;
      pbfStream.Seek(bufferStreamPos, SeekOrigin.Begin);
      if (pbfStream.Read(buffer, 0, buffer.Length) != buffer.Length) throw new IOException();
    }

    /// <summary>
    /// bereitet den Buffer vor, um einen bestimmten Bereich des Streams auszulesen (Rückgabe: Startposition innerhalb des Buffers)
    /// </summary>
    /// <param name="pbfPos">gewünschte Leseposition innerhalb der PBF-Datei</param>
    /// <param name="minBytes">minimale Anzahl der vorausgeladenen Bytes</param>
    /// <returns>Startposition innerhalb des Buffers</returns>
    public int PrepareBuffer(long pbfPos, int minBytes)
    {
      if (pbfPos < bufferStreamPos || pbfPos + minBytes > bufferStreamPos + buffer.Length)
      {
        if (minBytes > buffer.Length) throw new ArgumentOutOfRangeException("minBytes");
        UpdateBuffer(pbfPos);
      }

      return (int)(uint)((ulong)pbfPos - (ulong)bufferStreamPos);
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="pbfFile">Open Streetmap PBF-Datei, welche gelesen werden soll</param>
    public FastPbfReader(string pbfFile)
    {
      if (string.IsNullOrEmpty(pbfFile)) throw new NullReferenceException("pbfFile");
      if (!File.Exists(pbfFile)) throw new FileNotFoundException(pbfFile);

      pbfStream = new FileStream(pbfFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      pbfSize = pbfStream.Length;
      bufferStreamPos = long.MaxValue;
      buffer = new byte[Math.Min(MaxBuffer, pbfSize)];
      UpdateBuffer(0);
    }

    /// <summary>
    /// gibt alle Ressourcen wieder frei
    /// </summary>
    public void Dispose()
    {
      try
      {
        pbfStream.Dispose();
      }
      catch
      {
        // ignored
      }
    }
  }
}