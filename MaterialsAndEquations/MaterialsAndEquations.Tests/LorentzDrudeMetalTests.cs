using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MaterialsAndEquations;

namespace MaterialsAndEquations.Tests
{
    [TestClass]
    public class LorentzDrudeMetalTests
    {
        [TestMethod]
        public void DrudeOnly_EpsAtLongWavelengths_IsApproximatelyEpsInf()
        {
            // Drude only: set oscillators empty
            double epsInf = 1.0;
            double plasmaEv = 9.0; // arbitrary
            double gammaEv = 0.1;
            var metal = LorentzDrudeMetal.CreateFromEvParameters("test", epsInf, plasmaEv, gammaEv, Array.Empty<(double, double, double)>());

            // very long wavelength -> omega -> 0, dielectric ~ epsInf
            double wavelength = 1.0; // 1 meter (very long)
            var n = metal[wavelength];
            var eps = n * n;

            Assert.AreEqual(epsInf, eps.Real, 1e-12, "Real part of epsilon should approach epsInf at low omega");
            Assert.AreEqual(0.0, eps.Imaginary, 1e-12, "Imaginary part should be ~0 at low omega");
        }

        [TestMethod]
        public void FromPoints_Interpolation_ReturnsLinearInterpolatedValue()
        {
            // Create a simple material where refractive index equals wavelength (real) for test
            var points = new System.Collections.Generic.Dictionary<double, Complex>
            {
                [400e-9] = new Complex(1.0, 0.0),
                [500e-9] = new Complex(2.0, 0.0)
            };

            var mat = OpticalMaterial.FromPoints("lin", points);
            var q = mat[450e-9];
            Assert.AreEqual(1.5, q.Real, 1e-12);
            Assert.AreEqual(0.0, q.Imaginary, 1e-12);
        }

        [TestMethod]
        public void GetNk_ReturnsComponents()
        {
            var mat = new OpticalMaterial("const", wl => new Complex(1.23, 4.56));
            mat.GetNk(500e-9, out double n, out double k);
            Assert.AreEqual(1.23, n, 1e-12);
            Assert.AreEqual(4.56, k, 1e-12);
        }
    }
}
