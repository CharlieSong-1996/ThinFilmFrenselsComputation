using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MaterialsAndEquations;
using KM = MaterialsAndEquations.KnownMaterials.KnownMaterials;

namespace MaterialsAndEquations.Tests
{
    /// <summary>
    /// Lorentz-Drude 金属与 OpticalMaterial 的单元测试
    /// 包含对 Drude 极限、插值和 GetNk 的简单验证
    /// </summary>
    [TestClass]
    public class LorentzDrudeMetalTests
    {
        public TestContext? TestContext { get; set; }

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

        [TestMethod]
        public void Au_ComplexRefractiveIndex()
        {
            //此段Python代码供参考，为金属金的Lorentz-Drude模型参数示例
            //# Lorentz-Drude (LD) model parameters
            //ωp = 9.03  #eV
            //f0 = 0.760
            //Γ0 = 0.053 #eV
            //f1 = 0.024
            //Γ1 = 0.241 #eV
            //ω1 = 0.415 #eV
            //f2 = 0.010
            //Γ2 = 0.345 #eV
            //ω2 = 0.830 #eV
            //f3 = 0.071
            //Γ3 = 0.870 #eV
            //ω3 = 2.969 #eV
            //f4 = 0.601
            //Γ4 = 2.494 #eV
            //ω4 = 4.304 #eV
            //f5 = 4.384
            //Γ5 = 2.214 #eV
            //ω5 = 13.32 #eV
            //Ωp = f0**.5 * ωp  #eV
            //def LD(ω):  #ω: eV
            //    ε = 1-Ωp**2/(ω*(ω+1j*Γ0))
            //    ε += f1*ωp**2 / ((ω1**2-ω**2)-1j*ω*Γ1)
            //    ε += f2*ωp**2 / ((ω2**2-ω**2)-1j*ω*Γ2)
            //    ε += f3*ωp**2 / ((ω3**2-ω**2)-1j*ω*Γ3)
            //    ε += f4*ωp**2 / ((ω4**2-ω**2)-1j*ω*Γ4)
            //    ε += f5*ωp**2 / ((ω5**2-ω**2)-1j*ω*Γ5)
            //    return ε
            //ev_min=0.2
            //ev_max=5
            //npoints=200
            //eV = np.logspace(np.log10(ev_min), np.log10(ev_max), npoints)
            //μm = 4.13566733e-1*2.99792458/eV
            //ε = LD(eV)
            //n = (ε**.5).real
            //k = (ε**.5).imag


            // Arrange
            Assert.IsTrue(KM.Materials.ContainsKey("Au"), "Known materials must contain Au");
            var material = KM.Materials["Au"];

            var n300 = material[300e-9];
            TestContext.WriteLine($"Au at 300nm: n = {n300.Real:F4} + {n300.Imaginary:F4}i");
            Assert.AreEqual(n300.Real, 1.5199, 0.001);
            Assert.AreEqual(n300.Imaginary, 1.6783, 0.001);

            // Repeat the Python LD calculation in the test and compare
            Complex ComputeLdEv(double energyEv)
            {
                // parameters for Au from LD.csv (in eV)
                double omega_p = 9.03;
                double f0 = 0.760;
                double Gamma0 = 0.053;

                var osc = new (double f, double Gamma, double omega)[]
                {
                    (0.024, 0.241, 0.415),
                    (0.010, 0.345, 0.830),
                    (0.071, 0.870, 2.969),
                    (0.601, 2.494, 4.304),
                    (4.384, 2.214, 13.32)
                };

                Complex i = Complex.ImaginaryOne;
                double OmegaP = Math.Sqrt(f0) * omega_p; // Ωp = sqrt(f0)*ωp

                // Drude term: 1 - Ωp^2 / (E*(E + i Γ0))
                Complex E = new Complex(energyEv, 0.0);
                Complex eps = 1.0 - (OmegaP * OmegaP) / (E * (E + i * Gamma0));

                // Lorentz oscillators: sum f_j * ωp^2 / ((ω_j^2 - E^2) - i E Γ_j)
                foreach (var (f, g, w) in osc)
                {
                    Complex denom = new Complex(w * w - energyEv * energyEv, 0.0) - i * E * g;
                    eps += (f * omega_p * omega_p) / denom;
                }

                return Complex.Sqrt(eps);
            }

            // convert wavelength to photon energy in eV: E(eV) = h*c/(lambda)/e
            double h = 6.62607015e-34; // J*s
            double c = 299792458.0; // m/s
            double eCharge = 1.602176634e-19; // C

            double E300 = (h * c / 300e-9) / eCharge;
            var n300_py = ComputeLdEv(E300);
            TestContext.WriteLine($"Au Python LD at 300nm: n = {n300_py.Real:F4} + {n300_py.Imaginary:F4}i");
            Assert.AreEqual(n300.Real, n300_py.Real, 0.01);
            Assert.AreEqual(n300.Imaginary, n300_py.Imaginary, 0.01);

            var n600 = material[600e-9];
            TestContext.WriteLine($"Au at 600nm: n = {n600.Real:F4} + {n600.Imaginary:F4}i");
            Assert.AreEqual(n600.Real, 0.36216, 0.001);
            Assert.AreEqual(n600.Imaginary, 2.8493, 0.001);

            double E600 = (h * c / 600e-9) / eCharge;
            var n600_py = ComputeLdEv(E600);
            TestContext.WriteLine($"Au Python LD at 600nm: n = {n600_py.Real:F4} + {n600_py.Imaginary:F4}i");
            Assert.AreEqual(n600.Real, n600_py.Real, 0.01);
            Assert.AreEqual(n600.Imaginary, n600_py.Imaginary, 0.01);
        }
    }
}
