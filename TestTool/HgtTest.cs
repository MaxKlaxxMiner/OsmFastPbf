// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf;
using OsmFastPbf.zlibTuned;
using OsmFastPbf.zlibTuned.FastInflater;

namespace TestTool
{
  partial class Program
  {
    static int GZipTest(byte[] data)
    {
      using (var src = new MemoryStream(data))
      using (var dst = new MemoryStream())
      using (var zip = new GZipXStream(dst))
      {
        src.CopyTo(zip);
        zip.Flush();
        return (int)dst.Length;
      }
    }

    static unsafe void HgtTest()
    {
      const string TestFile = "../../../N00E032.hgt";
      if (!File.Exists(TestFile)) throw new FileNotFoundException(TestFile);

      var raw = File.ReadAllBytes(TestFile);
      var rawValues = Enumerable.Range(0, raw.Length / 2).Select(i => (short)(ushort)(raw[i * 2] << 8 | raw[i * 2 + 1])).ToArray();
      Console.WriteLine();

      // --- Raw-Data ---
      Console.WriteLine("    Raw Size: {0:N0} Bytes", raw.Length);
      Console.WriteLine();

      // --- GZip (level 9) ---
      Console.WriteLine("     Gnu-Zip: {0:N0} Bytes", GZipTest(raw));
      Console.WriteLine();

      // --- Delta-GZip ---
      var deltaValues = new short[rawValues.Length];
      deltaValues[0] = rawValues[0];
      for (int i = 1; i < deltaValues.Length; i++) deltaValues[i] = (short)(rawValues[i] - rawValues[i - 1]);
      var deltaData = new byte[raw.Length];
      fixed (byte* deltaDataP = deltaData)
      fixed (short* deltaValuesP = deltaValues)
      {
        OutputWindow.CopyBytes((byte*)deltaValuesP, deltaDataP, deltaData.Length);
      }
      Console.WriteLine("  Delta-GZip: {0:N0} Bytes", GZipTest(deltaData));
      Console.WriteLine();

      // --- Protobuffer-Delta-GZip ---
      int outputProtoBytes = 0;
      var protoBuf = new byte[deltaData.Length * 2];
      foreach (var val in deltaValues)
      {
        uint u = (uint)(val << 1 ^ val >> 31); // ZigZag
        if (u >= 128)
        {
          protoBuf[outputProtoBytes++] = (byte)(128 | u & 127);
          if ((u >> 7) >= 128)
          {
            protoBuf[outputProtoBytes++] = (byte)(128 | u >> 7 & 127);
            if ((u >> 14) >= 128) throw new IndexOutOfRangeException();
            protoBuf[outputProtoBytes++] = (byte)(u >> 14);
          }
          else protoBuf[outputProtoBytes++] = (byte)(u >> 7);
        }
        else protoBuf[outputProtoBytes++] = (byte)u;
      }
      Array.Resize(ref protoBuf, outputProtoBytes);
      Console.WriteLine("  Proto-GZip: {0:N0} Bytes", GZipTest(protoBuf));
      Console.WriteLine();

      // --- Stats ---
      var stats = string.Join("\r\n", rawValues.GroupBy(x => x).OrderBy(x => -x.Count()).Select(x => x.Key + ": " + x.Count()));
      var statsDelta = string.Join("\r\n", deltaValues.GroupBy(x => x).OrderBy(x => -x.Count()).Select(x => x.Key + ": " + x.Count()));
    }
  }
}
