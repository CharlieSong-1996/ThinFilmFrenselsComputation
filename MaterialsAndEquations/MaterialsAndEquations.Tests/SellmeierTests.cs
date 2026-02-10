using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KM = MaterialsAndEquations.KnownMaterials.KnownMaterials;

namespace MaterialsAndEquations.Tests
{
    [TestClass]
    public class SellmeierTests
    {
        public TestContext? TestContext { get; set; }

        [TestMethod]
        public void K9_RefractiveIndex()
        {
            // Arrange
            Assert.IsTrue(KM.Materials.ContainsKey("H-K9L"), "Known materials must contain K9");
            var material = KM.Materials["H-K9L"];

            int points = 701; // 300,301,...,1000 nm
            double[] xs = new double[points]; // wavelength in nm for plotting
            double[] ys = new double[points]; // refractive index (real)

            double startNm = 300.0;
            double endNm = 1000.0;
            double step = (endNm - startNm) / (points - 1);

            for (int i = 0; i < points; i++)
            {
                double wlNm = startNm + i * step;
                double wlM = wlNm * 1e-9; // convert nm to meters
                xs[i] = wlNm;
                var c = material[wlM];
                ys[i] = c.Real;

                if (i % 20 == 0) // Log every 20th point
                {
                    TestContext?.WriteLine($"Wavelength: {wlNm} nm, Refractive Index: {c.Real}");
                }
            }
        }

        [TestMethod]
        public void ZF4_RefractiveIndex()
        {
            // Arrange
            Assert.IsTrue(KM.Materials.ContainsKey("ZF4"), "Known materials must contain ZF4");
            var material = KM.Materials["ZF4"];

            int points = 701; // 300,301,...,1000 nm
            double[] xs = new double[points]; // wavelength in nm for plotting
            double[] ys = new double[points]; // refractive index (real)

            double startNm = 300.0;
            double endNm = 1000.0;
            double step = (endNm - startNm) / (points - 1);

            for (int i = 0; i < points; i++)
            {
                double wlNm = startNm + i * step;
                double wlM = wlNm * 1e-9; // convert nm to meters
                xs[i] = wlNm;
                var c = material[wlM];
                ys[i] = c.Real;

                if (i % 20 == 0) // Log every 20th point
                {
                    TestContext?.WriteLine($"Wavelength: {wlNm} nm, Refractive Index: {c.Real}");
                }
            }
        }


        [TestMethod]
        public void Water_RefractiveIndex()
        {
            // Arrange
            Assert.IsTrue(KM.Materials.ContainsKey("H2O"), "Known materials must contain water");
            var material = KM.Materials["H2O"];

            int points = 701; // 300,301,...,1000 nm
            double[] xs = new double[points]; // wavelength in nm for plotting
            double[] ys = new double[points]; // refractive index (real)

            double startNm = 300.0;
            double endNm = 1000.0;
            double step = (endNm - startNm) / (points - 1);

            for (int i = 0; i < points; i++)
            {
                double wlNm = startNm + i * step;
                double wlM = wlNm * 1e-9; // convert nm to meters
                xs[i] = wlNm;
                var c = material[wlM];
                ys[i] = c.Real;

                if (i % 20 == 0) // Log every 20th point
                {
                    TestContext?.WriteLine($"Wavelength: {wlNm} nm, Refractive Index: {c.Real}");
                }
            }
        }
    }
}
