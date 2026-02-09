using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KM = MaterialsAndEquations.KnownMaterials.KnownMaterials;

namespace MaterialsAndEquations.Tests
{
    [TestClass]
    public class EquationsTests
    {
        public TestContext? TestContext { get; set; }

        [TestMethod]
        public void Reflection_ZF4_Au_Water_660nm_40_to_85_step_0_1()
        {
            // Arrange
            Assert.IsTrue(KM.Materials.ContainsKey("ZF4"), "Known materials must contain ZF4");
            Assert.IsTrue(KM.Materials.ContainsKey("Au"), "Known materials must contain Au (Gold)");

            var materialIn = KM.Materials["ZF4"];
            var gold = KM.Materials["Au"];
            var materialOut = new MaterialsAndEquations.OpticalMaterial("Water", wl => new Complex(1.33, 0.0));

            double wavelength = 660e-9; // 660 nm
            double thicknessGold = 45e-9; // 45 nm

            var layers = new List<(MaterialsAndEquations.OpticalMaterial, double)>()
            {
                (gold, thicknessGold)
            };

            // Act
            var angles = new List<double>();
            var reflectionsS = new List<double>();
            var transmissionsS = new List<double>();
            var reflectionsP = new List<double>();
            var transmissionsP = new List<double>();

            for (int i = 400; i <= 850; i++) // 40.0 .. 85.0 by 0.1 -> multiply by 10
            {
                double angle = i / 10.0;
                angles.Add(angle);

                MaterialsAndEquations.Equations.ComputeReflectionTransmission(
                    out double thetaOutS,
                    out double reflectionS,
                    out double transmissionS,
                    wavelength,
                    materialIn,
                    layers,
                    materialOut,
                    angle,
                    polarizationS: true);

                MaterialsAndEquations.Equations.ComputeReflectionTransmission(
                    out double thetaOutP,
                    out double reflectionP,
                    out double transmissionP,
                    wavelength,
                    materialIn,
                    layers,
                    materialOut,
                    angle,
                    polarizationS: false);

                // Collect results
                reflectionsS.Add(reflectionS);
                transmissionsS.Add(transmissionS);
                reflectionsP.Add(reflectionP);
                transmissionsP.Add(transmissionP);

                // Basic sanity checks: reflection & transmission are finite and in [0, 1+eps]
                Assert.IsFalse(double.IsNaN(reflectionS) || double.IsInfinity(reflectionS));
                Assert.IsFalse(double.IsNaN(transmissionS) || double.IsInfinity(transmissionS));
                Assert.IsFalse(double.IsNaN(reflectionP) || double.IsInfinity(reflectionP));
                Assert.IsFalse(double.IsNaN(transmissionP) || double.IsInfinity(transmissionP));

                Assert.IsTrue(reflectionS >= -1e-6 && reflectionS <= 10.0, "ReflectionS out of expected range");
                Assert.IsTrue(reflectionP >= -1e-6 && reflectionP <= 10.0, "ReflectionP out of expected range");
            }

            // Log a few sample outputs
            TestContext?.WriteLine("angle (deg)\t R_s\t T_s\t R_p\t T_p");
            for (int idx = 0; idx < angles.Count; idx += Math.Max(1, angles.Count / 20))
            {
                TestContext?.WriteLine($"{angles[idx]:F1}\t {reflectionsS[idx]:F3}\t {transmissionsS[idx]:F3}\t {reflectionsP[idx]:F3}\t {transmissionsP[idx]:F3}");
            }

            // Optionally, write full CSV to TestContext output
            TestContext?.WriteLine("Full results (angle, R_s, T_s, R_p, T_p):");
            for (int idx = 0; idx < angles.Count; idx++)
            {
                TestContext?.WriteLine($"{angles[idx]:F1}\t{reflectionsS[idx]:F3}\t{transmissionsS[idx]:F3}\t{reflectionsP[idx]:F3}\t{transmissionsP[idx]:F3}");
            }

            // Assert that we produced the expected number of samples
            Assert.AreEqual((int)((85.0 - 40.0) / 0.1) + 1, angles.Count);
        }
    }
}
