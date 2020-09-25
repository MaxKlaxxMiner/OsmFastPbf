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
          for (long i = 0; i < 625000000; i++)
          {
            wdat.Write(BitConverter.GetBytes(i * sizeof(long)), 0, sizeof(long));
          }
        }
      }

      using (var test = new FastPbfReader(TestFile))
      {
        var rnd = new Random();
        const int CheckBlocks = 65536;

        for (long pos = 0; pos + CheckBlocks * sizeof(long) < test.pbfSize; pos += (rnd.Next(10000) - 4000) * sizeof(long))
        {
          if (pos < 0) continue;
          Console.WriteLine("{0:N0} / {1:N0}", pos, test.pbfSize);
          int ofs = test.PrepareBuffer(pos, CheckBlocks * sizeof(long));
          var buf = test.buffer;
          for (int i = 0; i < CheckBlocks; i++) if (BitConverter.ToInt64(buf, ofs + i * sizeof(long)) != pos + i * sizeof(long)) throw new Exception();
        }
      }

    }
  }
}
