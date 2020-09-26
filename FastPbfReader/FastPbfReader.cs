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
    /// Buffer-Position innerhalb des Streams
    /// </summary>
    long bufferStreamPos;
    /// <summary>
    /// Lesebuffer für die PBF-Datei
    /// </summary>
    public readonly byte[] buffer;

    /// <summary>
    /// gibt an, ob der Buffer für kleinere randomisierte kleine Zugriffe optimiert sein soll (nicht empfohlen für sequentielle Abfragen)
    /// </summary>
    bool randomBuffering;

    /// <summary>
    /// fragt ab, ob der Buffer für kleinere randomisierte Zugriffe optimiert sein soll oder diese fest (nicht empfohlen für sequentielle Abfragen)
    /// </summary>
    public bool RandomBuffering
    {
      get
      {
        return randomBuffering;
      }
      set
      {
        if (value == randomBuffering) return;
        randomBuffering = value;
        bufferStreamPos = long.MaxValue; // Buffersystem zurücksetzen
      }
    }

    /// <summary>
    /// aktualisiert den Inhalt des Lesebuffers
    /// </summary>
    /// <param name="pbfPos">gewünschte Position im Stream, womit die Daten im Buffer beginnen sollen</param>
    /// <param name="minBytes">minimale Anzahl der vorausgeladenen Bytes</param>
    void UpdateBuffer(long pbfPos, int minBytes)
    {
      if (pbfPos > pbfSize - buffer.Length) pbfPos = pbfSize - buffer.Length; // verhindern, dass hinter dem Buffer gelesen werden kann

      if (pbfPos < bufferStreamPos + buffer.Length & pbfPos >= bufferStreamPos) // kenn ein Teil des Buffers weiter verwendet werden?
      {
        long midPos = pbfPos - bufferStreamPos;
        long midLen = buffer.LongLength - midPos;
        bufferStreamPos = pbfPos;
        Array.Copy(buffer, midPos, buffer, 0, midLen);
        pbfStream.Seek(bufferStreamPos + midLen, SeekOrigin.Begin);
        if (pbfStream.Read(buffer, (int)midLen, buffer.Length - (int)midLen) != buffer.Length - (int)midLen) throw new IOException();
        return;
      }

      // --- den Buffer vollständig neu befüllen ---
      if (pbfPos < bufferStreamPos && bufferStreamPos - pbfPos < buffer.Length / 4 && minBytes < buffer.Length / 4)
      {
        // Leseposition ein viertel Buffer nach vorne setzen, falls wieder rückwärts gelesen werden sollte
        pbfPos = Math.Max(0, pbfPos - buffer.Length / 4);
      }
      bufferStreamPos = pbfPos;
      pbfStream.Seek(bufferStreamPos, SeekOrigin.Begin);
      if (pbfStream.Read(buffer, 0, buffer.Length) != buffer.Length) throw new IOException();
    }

    /// <summary>
    /// aktualisiert nur den minimalen Teil eines Buffers (wird ans Ende gesetzt)
    /// </summary>
    /// <param name="pbfPos">gewünschte Leseposition innerhalb der PBF-Datei</param>
    /// <param name="minBytes">minimale Anzahl der vorausgeladenen Bytes</param>
    void UpdateRandomBuffer(long pbfPos, int minBytes)
    {
      pbfStream.Seek(pbfPos, SeekOrigin.Begin);
      if (pbfStream.Read(buffer, buffer.Length - minBytes, minBytes) != minBytes) throw new IOException();
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
        if ((uint)minBytes > buffer.Length) throw new ArgumentOutOfRangeException("minBytes");
        if (pbfPos < 0 || pbfPos + minBytes > pbfSize) throw new ArgumentOutOfRangeException("pbfPos");

        if (randomBuffering) // Random-Buffering Modus?
        {
          UpdateRandomBuffer(pbfPos, minBytes);
          return buffer.Length - minBytes;
        }

        UpdateBuffer(pbfPos, minBytes);
      }

      return (int)(uint)((ulong)pbfPos - (ulong)bufferStreamPos);
    }

    /// <summary>
    /// prüft, ob ein gewünschter Buffer bereit liegt, Rückgabe: Startposition innerhalb des Buffers oder -1 wenn nicht verfügbar
    /// </summary>
    /// <param name="pbfPos">gewünschte Leseposition innerhalb der PBF-Datei</param>
    /// <param name="minBytes">minimale Anzahl der vorausgeladenen Bytes</param>
    /// <returns>Startposition innerhalb des Buffers oder -1 wenn nicht verfügbar</returns>
    public int CheckFastBuffer(long pbfPos, int minBytes)
    {
      if (pbfPos < bufferStreamPos || pbfPos + minBytes > bufferStreamPos + buffer.Length) return -1;
      return (int)(uint)((ulong)pbfPos - (ulong)bufferStreamPos);
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="pbfFile">Open Streetmap PBF-Datei, welche gelesen werden soll</param>
    /// <param name="maxBufferSize">optional: maximale Buffergröße (default: 16 MByte)</param>
    public FastPbfReader(string pbfFile, int maxBufferSize = 1048576 * 16)
    {
      if (string.IsNullOrEmpty(pbfFile)) throw new NullReferenceException("pbfFile");
      if (!File.Exists(pbfFile)) throw new FileNotFoundException(pbfFile);
      if (maxBufferSize < 256) throw new ArgumentOutOfRangeException("maxBufferSize");

      pbfStream = new FileStream(pbfFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      pbfSize = pbfStream.Length;
      bufferStreamPos = long.MaxValue;
      buffer = new byte[Math.Min(maxBufferSize, pbfSize)];
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