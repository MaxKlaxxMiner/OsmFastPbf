// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmFastPbf;
using OsmFastPbf.Helper;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

// ReSharper disable UnusedVariable

namespace TestTool
{
  partial class Program
  {
    static string PbfPath
    {
      get
      {
        string path = "planet-latest.osm.pbf";
        for (int i = 0; i < 32 && !File.Exists(path); i++) path = "../" + path;
        if (!File.Exists(path)) throw new FileNotFoundException(path.TrimStart('.', '/'));
        return path;
      }
    }

    static void DecodeNodeBlock(byte[] buf, int bufLen)
    {
      long id = 0;
      long pos = 0;
      for (int p = 0; p < bufLen; )
      {
        ulong tmp;
        p += ProtoBuf.ReadVarInt(buf, p, out tmp);
        id += ProtoBuf.SignedInt64(tmp);
        p += ProtoBuf.ReadVarInt(buf, p, out tmp);
        pos += ProtoBuf.SignedInt64(tmp);
        p += ProtoBuf.ReadVarInt(buf, p, out tmp);
        int valueCount = (int)(uint)tmp;
        for (int i = 0; i < valueCount; i++)
        {
          string key, val;
          p += ProtoBuf.ReadString(buf, p, out key);
          p += ProtoBuf.ReadString(buf, p, out val);
        }
      }
    }

    static void ConverterTest_2_SortNodes()
    {
      var blockFiles = Directory.GetFiles("../tmp/", "node_block_*_*.dat");
      if (blockFiles.Length == 0) throw new Exception();

      var buf = new byte[2000 * 1048576];

      Array.Sort(blockFiles, (x, y) => int.Parse(x.Split('_').Reverse().Skip(1).First()) - int.Parse(y.Split('_').Reverse().Skip(1).First()));
      foreach (var blockFile in blockFiles)
      {
        Console.WriteLine("read : " + blockFile);
        int bufLen;
        using (var rdat = File.OpenRead(blockFile))
        {
          bufLen = rdat.Read(buf, 0, buf.Length);
          if (bufLen != rdat.Length) throw new Exception();
        }
        Console.WriteLine("parse: " + blockFile);
        DecodeNodeBlock(buf, bufLen);
      }
    }

    static void Main(string[] args)
    {
      //BufferTest(); return;
      //HgtTest(); return;
      //ParseTest(); return;
      //DrawTest(); return;
      //ConverterTest_1_ExtractNodes();
      ConverterTest_2_SortNodes();
    }
  }
}
