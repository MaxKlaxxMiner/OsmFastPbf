using System;
using System.Runtime.CompilerServices;

namespace OsmFastPbf.zlibTuned
{
  public static class Crc32Helper
  {
    static readonly uint[] CrcTable;

    const uint Polynomial = 0xedb88320;

    static Crc32Helper()
    {
      CrcTable = new uint[8 * 256];
      for (uint i = 0; i < 256; i++)
      {
        uint crc = i;
        for (uint j = 0; j < 8; j++) crc = (crc >> 1) ^ ((crc & 1) * Polynomial);
        CrcTable[i] = crc;
      }

      for (uint i = 0; i < 256; i++)
      {
        CrcTable[1 * 256 + i] = (CrcTable[0 * 256 + i] >> 8) ^ CrcTable[(byte)CrcTable[0 * 256 + i]];
        CrcTable[2 * 256 + i] = (CrcTable[1 * 256 + i] >> 8) ^ CrcTable[(byte)CrcTable[1 * 256 + i]];
        CrcTable[3 * 256 + i] = (CrcTable[2 * 256 + i] >> 8) ^ CrcTable[(byte)CrcTable[2 * 256 + i]];
        CrcTable[4 * 256 + i] = (CrcTable[3 * 256 + i] >> 8) ^ CrcTable[(byte)CrcTable[3 * 256 + i]];
        CrcTable[5 * 256 + i] = (CrcTable[4 * 256 + i] >> 8) ^ CrcTable[(byte)CrcTable[4 * 256 + i]];
        CrcTable[6 * 256 + i] = (CrcTable[5 * 256 + i] >> 8) ^ CrcTable[(byte)CrcTable[5 * 256 + i]];
        CrcTable[7 * 256 + i] = (CrcTable[6 * 256 + i] >> 8) ^ CrcTable[(byte)CrcTable[6 * 256 + i]];
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe uint UnsafeCrc32X8(uint crc32, ulong* bufP, int length)
    {
      fixed (uint* crcTable = &CrcTable[0])
      {
        for (int i = 0; i < length; i++)
        {
          ulong val = bufP[i] ^ crc32;
          crc32 = crcTable[7 * 256 + (byte)val] ^
                  crcTable[6 * 256 + (byte)(val >> 8)] ^
                  crcTable[5 * 256 + (byte)(val >> 16)] ^
                  crcTable[4 * 256 + (byte)(val >> 24)] ^
                  crcTable[3 * 256 + (byte)(val >> 32)] ^
                  crcTable[2 * 256 + (byte)(val >> 40)] ^
                  crcTable[1 * 256 + (byte)(val >> 48)] ^
                  crcTable[val >> 56];
        }
      }
      return crc32;
    }

    /// <summary>
    /// berechnet den Crc32-Schlüssel
    /// </summary>
    /// <param name="crc32">vorheriger Crc32-Schlüssel</param>
    /// <param name="buf">Buffer, welcher berechnet werden soll</param>
    /// <param name="offset">Startposition im Buffer</param>
    /// <param name="length">der Daten im Buffer</param>
    /// <returns>fertiger Crc32-Schlüssel</returns>
    public static unsafe uint UpdateCrc32(uint crc32, byte[] buf, int offset, int length)
    {
      if (buf == null) throw new ArgumentNullException("buf");
      if (offset < 0 || length < 0 || offset + length > buf.Length) throw new ArgumentOutOfRangeException();

      fixed (byte* bufP = &buf[offset])
      {
        int len8 = length / 8;
        crc32 = UnsafeCrc32X8(~crc32, (ulong*)bufP, len8);

        for (int i = len8 * 8; i < length; i++)
        {
          crc32 = (crc32 >> 8) ^ CrcTable[(byte)(crc32 ^ bufP[i])];
        }
      }

      return ~crc32;
    }
  }
}
