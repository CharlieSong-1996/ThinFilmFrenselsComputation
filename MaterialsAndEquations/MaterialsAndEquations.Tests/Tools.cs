using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KM = MaterialsAndEquations.KnownMaterials.KnownMaterials;
using ScottPlot;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics;
using System.Collections;

namespace MaterialsAndEquations.Tests
{
    internal static class Tools
    {
        // Returns a directory path where test plots can be stored. Creates the directory if necessary.
        public static string GetPlotsDirectory(string folderName = "TestPlots")
        {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
            return folderName;
        }

        // Kept for compatibility; saving is handled by user code. Use GetPlotsDirectory to obtain where to write files.
        public static void SavePlot(Plot p, string appendix = "", [CallerMemberName] string testName = "")
        {
            var saveDir = Path.Combine(GetPlotsDirectory(), $"{testName}{appendix}.png");
            Console.WriteLine(saveDir);
            p.SavePng(saveDir, 1200, 800);
        }
    }
}
