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


namespace MaterialsAndEquations.Tests
{
    [TestClass]
    public class Sprimodeling
    {
        // Returns a directory path where test plots can be stored. Creates the directory if necessary.
        public string GetPlotsDirectory(string folderName = "TestPlots")
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string folder = Path.Combine(baseDir, folderName);
            Directory.CreateDirectory(folder);
            return folder;
        }

        // Kept for compatibility; saving is handled by user code. Use GetPlotsDirectory to obtain where to write files.
        public void SavePlot(Plot p,[CallerMemberName]string testName = "") 
        {
            try
            {
                p.SavePng(Path.Combine(GetPlotsDirectory(),$"\\{testName}.png"), 600, 400);
            }
            catch (Exception ex) 
            { 
            }
        }


        public void ClassicalCrAuSPRi()
        {
             
        }
    }
}
