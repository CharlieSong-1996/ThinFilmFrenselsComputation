using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MaterialsAndEquations
{
    /// <summary>
    /// 使用 Lorentz-Drude 模型表示的金属材料。
    ///
    /// 模型形式（使用角频率 ω）：
    /// ε(ω) = ε_inf - (ω_p^2) / (ω^2 + i γ_D ω) + Σ_j (f_j * ω_p^2) / (ω_j^2 - ω^2 - i γ_j ω)
    /// 其中 f_j 为各个振子强度（无量纲），ω_p 为总体等离子体频率（rad/s），ω_j 和 γ_j 为各振子共振角频率和阻尼（rad/s）。
    ///
    /// 波长输入单位：米（m）。
    /// </summary>
    public class LorentzDrudeMetal : OpticalMaterial
    {
        private const double SpeedOfLight = 299792458.0; // m/s
        private const double ElectronCharge = 1.602176634e-19; // C (J per eV)
        private const double Hbar = 1.054571817e-34; // J*s
        private static readonly double EvToRadPerSec = ElectronCharge / Hbar; // ≈ 1.519267e15 rad/s per eV

        private readonly double _epsInf;
        private readonly double _omegaP; // original plasma rad/s used in oscillator numerators
        private readonly double _omegaPDrude; // effective plasma rad/s used in Drude term (sqrt(f0)*omegaP)
        private readonly double _gammaDrude; // rad/s
        private readonly (double Strength, double Omega0, double Gamma)[] _oscillators;

        /// <summary>
        /// 创建使用 SI 单位（rad/s）参数的 Lorentz-Drude 金属模型。
        /// 波长输入和索引器仍使用米（m）。
        /// </summary>
        /// <param name="name">材料名称</param>
        /// <param name="epsInf">高频介电常数</param>
        /// <param name="omegaP">等离子体角频率，单位：rad/s</param>
        /// <param name="gammaDrude">Drude 阻尼，单位：rad/s</param>
        /// <param name="oscillators">每项为 (strength f_j, omega0_j (rad/s), gamma_j (rad/s))</param>
        public LorentzDrudeMetal(string name, double epsInf, double omegaP, double omegaPDrude, double gammaDrude, IEnumerable<(double Strength, double Omega0, double Gamma)> oscillators)
            : base(name, wl => RefractiveIndexAtWavelength(wl, epsInf, omegaP, omegaPDrude, gammaDrude, oscillators))
        {
            _epsInf = epsInf;
            _omegaP = omegaP;
            _omegaPDrude = omegaPDrude;
            _gammaDrude = gammaDrude;
            _oscillators = oscillators?.ToArray() ?? Array.Empty<(double, double, double)>();
        }

        /// <summary>
        /// 使用能量单位为电子伏特（eV）的常用参数创建模型，构造函数会将 eV 转换为 rad/s。
        /// </summary>
        /// <param name="name">材料名称</param>
        /// <param name="epsInf">高频介电常数</param>
        /// <param name="plasmaEnergyEv">等离子体能量，单位：eV</param>
        /// <param name="drudeGammaEv">Drude 阻尼能量，单位：eV</param>
        /// <param name="oscillatorsEv">振子参数序列，每项为 (strength f_j, resonanceEnergyEv, gammaEv)</param>
        /// <returns>LorentzDrudeMetal 实例</returns>
        public static LorentzDrudeMetal CreateFromEvParameters(string name, double epsInf, double plasmaEnergyEv, double drudeGammaEv, IEnumerable<(double Strength, double ResonanceEnergyEv, double GammaEv)> oscillatorsEv, double drudeStrength = 1.0)
        {
            if (oscillatorsEv == null) throw new ArgumentNullException(nameof(oscillatorsEv));

            double omegaP = plasmaEnergyEv * EvToRadPerSec; // original omega_p
            double gammaDrude = drudeGammaEv * EvToRadPerSec;
            double omegaPDrude = Math.Sqrt(Math.Max(0.0, drudeStrength)) * omegaP; // Ωp = sqrt(f0)*ωp
            var osc = oscillatorsEv.Select(o => (o.Strength, o.ResonanceEnergyEv * EvToRadPerSec, o.GammaEv * EvToRadPerSec));
            return new LorentzDrudeMetal(name, epsInf, omegaP, omegaPDrude, gammaDrude, osc);
        }

        private static Complex RefractiveIndexAtWavelength(double wavelengthMeters, double epsInf, double omegaP, double omegaPDrude, double gammaDrude, IEnumerable<(double Strength, double Omega0, double Gamma)> oscillators)
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

            if (wavelengthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(wavelengthMeters), "wavelength must be positive and in meters");

            // angular frequency
            double omega = 2.0 * Math.PI * SpeedOfLight / wavelengthMeters;
            Complex i = Complex.ImaginaryOne;

            // Drude term: - Omega_p^2 / (omega^2 + i gamma_D omega)
            // Omega_p (effective) = sqrt(f0) * omegaP; use omegaPDrude for Drude numerator
            Complex denomDrude = omega * omega + i * gammaDrude * omega;
            Complex eps = epsInf - (omegaPDrude * omegaPDrude) / denomDrude;

            // Lorentz oscillators: sum_j f_j * omega_p^2 / (omega_j^2 - omega^2 - i gamma_j omega)
            foreach (var (strength, omega0, gamma) in oscillators)
            {
                Complex denomLor = omega0 * omega0 - omega * omega - i * gamma * omega;
                // oscillator numerators use the original omega_p^2
                eps += (strength * omegaP * omegaP) / denomLor;
            }

            // refractive index n = sqrt(eps)
            var n = Complex.Sqrt(eps);
            return n;
        }

        /// <summary>
        /// 返回模型中使用的振子参数（不可变副本）。
        /// Omega 单位为 rad/s。
        /// </summary>
        public IReadOnlyList<(double Strength, double Omega0, double Gamma)> Oscillators => Array.AsReadOnly(_oscillators);

        /// <summary>
        /// 获取等离子体角频率（rad/s）
        /// </summary>
        public double PlasmaOmega => _omegaP;

        /// <summary>
        /// 获取 Drude 阻尼（rad/s）
        /// </summary>
        public double DrudeGamma => _gammaDrude;

        /// <summary>
        /// 获取高频介电常数
        /// </summary>
        public double EpsInf => _epsInf;
    }
}
