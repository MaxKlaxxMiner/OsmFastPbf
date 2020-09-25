// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf;

namespace TestTool
{
  partial class Program
  {
    static void BufferTest()
    {
      const string TestFile = "../test5g.bin";
      if (!File.Exists(TestFile))
      {
        using (var wdat = File.Create(TestFile))
        {
          const long TestDataLength = 625000000;
          for (long i = 0; i < TestDataLength; i++)
          {
            if ((i & 0xffffff) == 0) Console.WriteLine("write testdata: {0:N0} MB ({1:N1} %)", i * 8 / 1048576, 100.0 / TestDataLength * i);
            wdat.Write(BitConverter.GetBytes(i * sizeof(long)), 0, sizeof(long));
          }
        }
      }

      using (var test = new FastPbfReader(TestFile, 2000000000))
      {
        test.RandomBuffering = true;
        var rnd = new Random(12345);
        const int CheckBlocks = 1543; // 1543 = 12344 Bytes
        int tim = 0;

        for (long pos = 0; pos + CheckBlocks * sizeof(long) < test.pbfSize; pos += (rnd.Next(-990000, 1000000)) * sizeof(long))
        {
          if (pos < 0) continue;
          if (tim != Environment.TickCount)
          {
            Console.WriteLine("{0:N0} / {1:N0}", pos, test.pbfSize);
            tim = Environment.TickCount;
          }
          int ofs = test.PrepareBuffer(pos, CheckBlocks * sizeof(long));
          var buf = test.buffer;
          for (int i = 0; i < CheckBlocks; i++) if (BitConverter.ToInt64(buf, ofs + i * sizeof(long)) != pos + i * sizeof(long)) throw new Exception();
        }
      }

    }
  }
}
