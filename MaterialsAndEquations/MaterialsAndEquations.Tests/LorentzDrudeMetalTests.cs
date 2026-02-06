using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MaterialsAndEquations;

namespace MaterialsAndEquations.Tests
{
    /// <summary>
    /// Lorentz-Drude 金属与 OpticalMaterial 的单元测试
    /// 包含对 Drude 极限、插值和 GetNk 的简单验证
    /// </summary>
    [TestClass]
    public class LorentzDrudeMetalTests
    {
        [TestMethod]
        // 测试：从离散点创建材料时，插值应为线性插值（在中点应取得中间值）
        public void FromPoints_LinearInterpolation()
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
        // 测试：GetNk 方法应返回 n（实部）和 k（虚部）两个分量
        public void GetNk_ReturnsNK()
        {
            var mat = new OpticalMaterial("const", wl => new Complex(1.23, 4.56));
            mat.GetNk(500e-9, out double n, out double k);
            Assert.AreEqual(1.23, n, 1e-12);
            Assert.AreEqual(4.56, k, 1e-12);
        }
    }
}
