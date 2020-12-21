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

    static void ConverterTest_3_MergeNodes()
    {

    }

    static void Main(string[] args)
    {
      //BufferTest(); return;
      //HgtTest(); return;
      //ParseTest(); return;
      //DrawTest(); return;
      //ConverterTest_1_ExtractNodes();
      //ConverterTest_2_SortNodes();
      ConverterTest_3_MergeNodes();
    }
  }
}
