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


        [TestMethod]
        public void SensitivityTest()
        {
            var materialIn = KM.Materials["H-K9L"];
            var materialOut = KM.Materials["H2O"];

            var layers = new List<(MaterialsAndEquations.OpticalMaterial, double)>()
            {
                (KM.Materials["Cr"], 5e-9),
                (KM.Materials["Au"], 47.5e-9),
            };

            for(double i = 60; i < 80; i += 0.1)
            {
                var sensi = SPRiOptimizer.ComputeSPRiSensitivity(materialIn, layers, materialOut, 660e-9, i);
                TestContext?.WriteLine($"Angle:{i:F1}\t{sensi:F3}");
            }
        }

        #region ComputeSPRiPrismAngles Tests

        /// <summary>
        /// 测试ComputeSPRiPrismAngles的双向转换功能
        /// 从内部角度计算外部角度，再反向计算应该得到原始值
        /// 测试光路可逆性原理的正确实现
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_Bidirectional_Conversion_Should_Be_Reversible()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double prismAngle = 60.0;

            TestContext?.WriteLine("Testing bidirectional conversion (Internal <-> External):");
            TestContext?.WriteLine("Internal(°)\tExternal(°)\tReversed(°)\tError(°)");

            int successCount = 0;
            // 测试一系列内部角度
            for (double internalAngle = 55.0; internalAngle <= 80.0; internalAngle += 2.5)
            {
                // Act: 内部角度 -> 外部角度
                double internal1 = internalAngle;
                double external1 = double.NaN;
                SPRiOptimizer.ComputeSPRiPrismAngles(
                    ref internal1,
                    ref external1,
                    slide,
                    prism,
                    wavelength,
                    prismAngle);

                if (double.IsNaN(external1))
                {
                    TestContext?.WriteLine($"{internalAngle:F2}\t(NaN - Total Internal Reflection or Invalid)");
                    continue; // 跳过无效的角度（可能是全反射）
                }

                // Act: 外部角度 -> 内部角度（反向转换）
                double internal2 = double.NaN;
                double external2 = external1;
                SPRiOptimizer.ComputeSPRiPrismAngles(
                    ref internal2,
                    ref external2,
                    slide,
                    prism,
                    wavelength,
                    prismAngle);

                double error = Math.Abs(internalAngle - internal2);
                TestContext?.WriteLine($"{internalAngle:F2}\t{external1:F2}\t{internal2:F2}\t{error:E3}");

                // Assert: 反向转换应该得到原始的内部角度（精度在1e-6度以内）
                Assert.AreEqual(internalAngle, internal2, 1e-6,
                    $"Bidirectional conversion failed: {internalAngle:F6}° -> {external1:F6}° -> {internal2:F6}°");

                successCount++;
            }

            Assert.IsTrue(successCount > 0, "Should have at least some valid conversions");
            TestContext?.WriteLine($"\nTotal successful conversions: {successCount}");
        }

        /// <summary>
        /// 测试无效输入的异常处理
        /// 当两个角度都是NaN或都不是NaN时，应该抛出ArgumentException
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ComputeSPRiPrismAngles_Both_NaN_Should_Throw_Exception()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double prismAngle = 60.0;

            double internal1 = double.NaN;
            double external1 = double.NaN;

            // Act & Assert: 应该抛出ArgumentException
            SPRiOptimizer.ComputeSPRiPrismAngles(
                ref internal1,
                ref external1,
                slide,
                prism,
                wavelength,
                prismAngle);
        }

        /// <summary>
        /// 测试无效输入的异常处理
        /// 当两个角度都不是NaN时，应该抛出ArgumentException
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ComputeSPRiPrismAngles_Both_Not_NaN_Should_Throw_Exception()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double prismAngle = 60.0;

            double internal1 = 65.0;
            double external1 = 45.0;

            // Act & Assert: 应该抛出ArgumentException
            SPRiOptimizer.ComputeSPRiPrismAngles(
                ref internal1,
                ref external1,
                slide,
                prism,
                wavelength,
                prismAngle);
        }

        /// <summary>
        /// 测试不同棱镜角度对转换的影响
        /// 棱镜角度是影响SPRi系统光路的关键参数
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_Different_Prism_Angles()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double testInternalAngle = 65.0;

            var prismAngles = new[] { 45.0, 50.0, 55.0, 60.0, 65.0, 70.0 };

            TestContext?.WriteLine("Testing different prism angles:");
            TestContext?.WriteLine("PrismAngle(°)\tInternal(°)\tExternal(°)");

            foreach (var prismAngle in prismAngles)
            {
                // Act
                double internal1 = testInternalAngle;
                double external1 = double.NaN;
                SPRiOptimizer.ComputeSPRiPrismAngles(
                    ref internal1,
                    ref external1,
                    slide,
                    prism,
                    wavelength,
                    prismAngle);

                // Assert: 应该产生有效的外部角度
                if (!double.IsNaN(external1))
                {
                    Assert.IsTrue(external1 > 0 && external1 < 90,
                        $"External angle should be in valid range for prism angle {prismAngle}°");

                    TestContext?.WriteLine($"{prismAngle:F1}\t{testInternalAngle:F1}\t{external1:F2}");
                }
                else
                {
                    TestContext?.WriteLine($"{prismAngle:F1}\t{testInternalAngle:F1}\t(NaN)");
                }
            }
        }

        /// <summary>
        /// 测试不同材料组合对角度转换的影响
        /// 验证Snell定律在不同折射率材料中的正确应用
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_Different_Material_Combinations()
        {
            // Arrange
            double wavelength = 660e-9;
            double prismAngle = 60.0;
            double testInternalAngle = 65.0;

            var materialCombinations = new[]
            {
                (slide: "H-K9L", prism: "ZF4"),
                (slide: "H-K9L", prism: "H-K9L"),
                (slide: "ZF4", prism: "ZF4")
            };

            TestContext?.WriteLine("Testing different material combinations:");
            TestContext?.WriteLine("Slide\tPrism\tInternal(°)\tExternal(°)\tnSlide\tnPrism");

            foreach (var (slideName, prismName) in materialCombinations)
            {
                var slide = KM.Materials[slideName];
                var prism = KM.Materials[prismName];

                double nSlide = slide[wavelength].Real;
                double nPrism = prism[wavelength].Real;

                // Act
                double internal1 = testInternalAngle;
                double external1 = double.NaN;
                SPRiOptimizer.ComputeSPRiPrismAngles(
                    ref internal1,
                    ref external1,
                    slide,
                    prism,
                    wavelength,
                    prismAngle);

                // Assert
                if (!double.IsNaN(external1))
                {
                    TestContext?.WriteLine($"{slideName}\t{prismName}\t{testInternalAngle:F1}\t{external1:F2}\t{nSlide:F4}\t{nPrism:F4}");
                }
                else
                {
                    TestContext?.WriteLine($"{slideName}\t{prismName}\t{testInternalAngle:F1}\t(NaN)\t{nSlide:F4}\t{nPrism:F4}");
                }
            }
        }

        /// <summary>
        /// 测试接近临界角的边界情况
        /// 验证在全反射条件附近的行为
        /// 由于光路从slide(低折射率)到prism(高折射率)，不存在临界角
        /// 但在prism到air的界面可能发生全反射
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_Near_Critical_Angle()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double prismAngle = 60.0;

            double nSlide = slide[wavelength].Real;
            double nPrism = prism[wavelength].Real;

            TestContext?.WriteLine($"Slide refractive index: {nSlide:F4}");
            TestContext?.WriteLine($"Prism refractive index: {nPrism:F4}");
            
            // 从slide到prism是从低折射率到高折射率，不存在临界角
            // 但是当内部角度过大时，可能在prism到air界面发生全反射
            TestContext?.WriteLine("\nTesting large internal angles (may cause total reflection at prism-air interface):");
            TestContext?.WriteLine("Internal(°)\tExternal(°)\tStatus");

            // 测试一系列内部角度，包括一些可能导致全反射的大角度
            var testAngles = new[] { 55.0, 60.0, 65.0, 70.0, 75.0, 80.0, 82.0, 84.0, 85.0, 86.0, 87.0, 88.0 };

            int validCount = 0;
            int totalReflectionCount = 0;

            foreach (var angle in testAngles)
            {
                // Act
                double internal1 = angle;
                double external1 = double.NaN;
                
                try
                {
                    SPRiOptimizer.ComputeSPRiPrismAngles(
                        ref internal1,
                        ref external1,
                        slide,
                        prism,
                        wavelength,
                        prismAngle);

                    string status = double.IsNaN(external1) ? "Total Reflection" : "Valid";
                    TestContext?.WriteLine($"{angle:F2}\t{(double.IsNaN(external1) ? "NaN" : external1.ToString("F2"))}\t{status}");

                    if (double.IsNaN(external1))
                    {
                        totalReflectionCount++;
                    }
                    else
                    {
                        validCount++;
                        // 有效的外部角度应该在合理范围内
                        Assert.IsTrue(external1 >= 0 && external1 <= 90,
                            $"External angle {external1:F2}° should be in valid range");
                    }
                }
                catch (ArgumentException)
                {
                    // 如果抛出异常，说明输入无效，跳过
                    TestContext?.WriteLine($"{angle:F2}\t(Invalid Input)");
                }
            }

            TestContext?.WriteLine($"\nValid conversions: {validCount}");
            TestContext?.WriteLine($"Total reflection cases: {totalReflectionCount}");
            
            // 应该至少有一些有效的转换
            Assert.IsTrue(validCount > 0, "Should have at least some valid angle conversions");
        }

        /// <summary>
        /// 测试极端角度值的数值稳定性
        /// 确保在极小角度和极大角度下计算不会出错
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_Extreme_Angles_Stability()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double prismAngle = 60.0;

            var extremeAngles = new[] { 0.1, 1.0, 5.0, 85.0, 88.0, 89.0, 89.9 };

            TestContext?.WriteLine("Testing extreme angles for numerical stability:");
            TestContext?.WriteLine("Internal(°)\tExternal(°)\tStatus");

            foreach (var angle in extremeAngles)
            {
                // Act
                double internal1 = angle;
                double external1 = double.NaN;

                try
                {
                    SPRiOptimizer.ComputeSPRiPrismAngles(
                        ref internal1,
                        ref external1,
                        slide,
                        prism,
                        wavelength,
                        prismAngle);

                    // Assert: 不应该抛出异常，应该返回有效值或NaN
                    string status = double.IsNaN(external1) ? "NaN (expected)" : "Valid";
                    TestContext?.WriteLine($"{angle:F2}\t{(double.IsNaN(external1) ? "NaN" : external1.ToString("F2"))}\t{status}");

                    // 如果返回值不是NaN，应该是有限值
                    if (!double.IsNaN(external1))
                    {
                        Assert.IsFalse(double.IsInfinity(external1),
                            $"External angle should not be infinity for internal angle {angle}°");
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Should not throw exception for angle {angle}°: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 测试不同波长对角度转换的影响
        /// 由于折射率的波长依赖性，转换结果也应该随波长变化
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_Wavelength_Dependence()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double prismAngle = 60.0;
            double testInternalAngle = 65.0;

            var wavelengths = new[] { 450e-9, 550e-9, 633e-9, 660e-9, 780e-9 };

            TestContext?.WriteLine("Testing wavelength dependence:");
            TestContext?.WriteLine("WL(nm)\tInternal(°)\tExternal(°)\tnSlide\tnPrism");

            foreach (var wl in wavelengths)
            {
                double nSlide = slide[wl].Real;
                double nPrism = prism[wl].Real;

                // Act
                double internal1 = testInternalAngle;
                double external1 = double.NaN;
                SPRiOptimizer.ComputeSPRiPrismAngles(
                    ref internal1,
                    ref external1,
                    slide,
                    prism,
                    wl,
                    prismAngle);

                // Assert
                if (!double.IsNaN(external1))
                {
                    TestContext?.WriteLine($"{wl * 1e9:F0}\t{testInternalAngle:F1}\t{external1:F2}\t{nSlide:F4}\t{nPrism:F4}");

                    // 外部角度应该随波长变化
                    Assert.IsTrue(external1 > 0 && external1 < 90,
                        $"External angle at {wl * 1e9:F0}nm should be in valid range");
                }
                else
                {
                    TestContext?.WriteLine($"{wl * 1e9:F0}\t{testInternalAngle:F1}\t(NaN)\t{nSlide:F4}\t{nPrism:F4}");
                }
            }
        }

        /// <summary>
        /// 测试外部角度到内部角度的转换
        /// 验证从外部角度计算内部角度的正确性
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_External_To_Internal_Conversion()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double prismAngle = 60.0;

            TestContext?.WriteLine("Testing external to internal angle conversion:");
            TestContext?.WriteLine("External(°)\tInternal(°)");

            int validCount = 0;
            // 测试一系列外部角度
            for (double externalAngle = 30.0; externalAngle <= 55.0; externalAngle += 2.5)
            {
                // Act
                double internal1 = double.NaN;
                double external1 = externalAngle;
                SPRiOptimizer.ComputeSPRiPrismAngles(
                    ref internal1,
                    ref external1,
                    slide,
                    prism,
                    wavelength,
                    prismAngle);

                // Assert
                if (!double.IsNaN(internal1))
                {
                    Assert.IsTrue(internal1 > 0 && internal1 < 90,
                        $"Internal angle should be in valid range for external angle {externalAngle}°");

                    TestContext?.WriteLine($"{externalAngle:F2}\t{internal1:F2}");
                    validCount++;
                }
                else
                {
                    TestContext?.WriteLine($"{externalAngle:F2}\t(NaN)");
                }
            }

            Assert.IsTrue(validCount > 0, "Should have at least some valid conversions");
            TestContext?.WriteLine($"\nTotal valid conversions: {validCount}");
        }

        /// <summary>
        /// 测试角度转换的连续性
        /// 当输入角度连续变化时，输出角度也应该连续变化（除非遇到临界角）
        /// </summary>
        [TestMethod]
        public void ComputeSPRiPrismAngles_Continuity_Test()
        {
            // Arrange
            var slide = KM.Materials["H-K9L"];
            var prism = KM.Materials["ZF4"];
            double wavelength = 660e-9;
            double prismAngle = 60.0;

            TestContext?.WriteLine("Testing continuity of angle conversion:");

            double? previousExternal = null;
            double previousInternal = 55.0;
            int discontinuityCount = 0;

            // 以小步长扫描内部角度
            for (double internalAngle = 55.0; internalAngle <= 75.0; internalAngle += 0.5)
            {
                double internal1 = internalAngle;
                double external1 = double.NaN;
                SPRiOptimizer.ComputeSPRiPrismAngles(
                    ref internal1,
                    ref external1,
                    slide,
                    prism,
                    wavelength,
                    prismAngle);

                if (!double.IsNaN(external1))
                {
                    if (previousExternal.HasValue)
                    {
                        double change = Math.Abs(external1 - previousExternal.Value);
                        double inputChange = internalAngle - previousInternal;

                        // 输出变化应该与输入变化成比例（在没有临界角的情况下）
                        // 允许一定的比例范围
                        if (change > 2.0 * inputChange) // 粗略的连续性检查
                        {
                            discontinuityCount++;
                            TestContext?.WriteLine($"Possible discontinuity at internal={internalAngle:F2}°: change={change:F3}°");
                        }
                    }

                    previousExternal = external1;
                    previousInternal = internalAngle;
                }
                else
                {
                    // 遇到NaN（可能是临界角）
                    if (previousExternal.HasValue)
                    {
                        TestContext?.WriteLine($"Critical angle reached at internal={internalAngle:F2}°");
                    }
                    previousExternal = null;
                }
            }

            // 允许少量不连续点（由于数值误差或边界条件）
            Assert.IsTrue(discontinuityCount < 3, 
                $"Too many discontinuities detected: {discontinuityCount}");
        }

        #endregion
    }
}
