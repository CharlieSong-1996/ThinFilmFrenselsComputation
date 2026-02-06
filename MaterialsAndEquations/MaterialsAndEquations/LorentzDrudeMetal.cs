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
        private readonly double _omegaP; // rad/s
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
        public LorentzDrudeMetal(string name, double epsInf, double omegaP, double gammaDrude, IEnumerable<(double Strength, double Omega0, double Gamma)> oscillators)
            : base(name, wl => RefractiveIndexAtWavelength(wl, epsInf, omegaP, gammaDrude, oscillators))
        {
            _epsInf = epsInf;
            _omegaP = omegaP;
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
        public static LorentzDrudeMetal CreateFromEvParameters(string name, double epsInf, double plasmaEnergyEv, double drudeGammaEv, IEnumerable<(double Strength, double ResonanceEnergyEv, double GammaEv)> oscillatorsEv)
        {
            if (oscillatorsEv == null) throw new ArgumentNullException(nameof(oscillatorsEv));

            double omegaP = plasmaEnergyEv * EvToRadPerSec;
            double gammaDrude = drudeGammaEv * EvToRadPerSec;
            var osc = oscillatorsEv.Select(o => (o.Strength, o.ResonanceEnergyEv * EvToRadPerSec, o.GammaEv * EvToRadPerSec));
            return new LorentzDrudeMetal(name, epsInf, omegaP, gammaDrude, osc);
        }

        private static Complex RefractiveIndexAtWavelength(double wavelengthMeters, double epsInf, double omegaP, double gammaDrude, IEnumerable<(double Strength, double Omega0, double Gamma)> oscillators)
        {
            if (wavelengthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(wavelengthMeters), "wavelength must be positive and in meters");

            // angular frequency
            double omega = 2.0 * Math.PI * SpeedOfLight / wavelengthMeters;
            Complex i = Complex.ImaginaryOne;

            // Drude term: - omega_p^2 / (omega^2 + i gamma_D omega)
            Complex denomDrude = omega * omega + i * gammaDrude * omega;
            Complex eps = epsInf - (omegaP * omegaP) / denomDrude;

            // Lorentz oscillators: sum_j f_j * omega_p^2 / (omega_j^2 - omega^2 - i gamma_j omega)
            foreach (var (strength, omega0, gamma) in oscillators)
            {
                Complex denomLor = omega0 * omega0 - omega * omega - i * gamma * omega;
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
