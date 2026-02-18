using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MaterialsAndEquations
{
    /// <summary>
    /// 基于 Debye-Lorentz 模型表示的纯水（三倍蒸馏水）折射率计算。
    /// 
    /// 模型包含：
    /// 1. Debye 部分（适用于微波频率）：
    ///    ε_D = 1 + Σ_k [a_k / ((1 - i * τ_j,k * ω)^(1 - ν_k) * (1 - i * τ_f,k * ω))]
    /// 2. Lorentz 部分（适用于红外和可见光频率）：
    ///    ε_L = Σ_k [f_k * ω_p^2 / (ω_k^2 - ω^2 - i * γ_k * ω)]
    /// 
    /// 参数参考自：
    /// [3] J. E. K. Laurens and K. E. Oughstun, Electromagnetic impulse,
    ///     response of triply distilled water, Ultra-Wideband /
    ///     Short-Pulse Electromagnetics (1999)
    /// </summary>
    public class DebyeLorentzWater : OpticalMaterial
    {
        private const double SpeedOfLight = 299792458.0; // m/s
        private const double EvToRadPerSec = 1.51926751447914e+15; // e/hbar

        // Debye parameters
        private static readonly double[] A = { 74.65, 2.988 };
        private static readonly double[] TauJ = { 8.30e-12, 5.91e-14 };
        private static readonly double[] TauF = { 1.09e-13, 8.34e-15 };
        private static readonly double[] Nu = { 0, -0.5 };

        // Lorentz parameters
        private static readonly double OmegaP = EvToRadPerSec; // "virtual" plasma frequency (1 eV in rad/s)
        private static readonly double[] F = { 0, 1.0745e-05, 3.1155e-03, 1.6985e-04, 1.1795e-02, 1.7504e+02 };
        private static readonly double[] GammaEv = { 0, 0.0046865, 0.059371, 0.0040546, 0.037650, 7.66167 };
        private static readonly double[] OmegaEv = { 0, 0.013691, 0.069113, 0.21523, 0.40743, 15.1390 };

        public DebyeLorentzWater() : base("Pure Water (H2O) - Debye-Lorentz", CalculateRefractiveIndex)
        {
        }

        private static Complex CalculateRefractiveIndex(double wavelengthMeters)
        {
            if (wavelengthMeters <= 0) 
                throw new ArgumentOutOfRangeException(nameof(wavelengthMeters), "Wavelength must be positive.");

            double omega = 2.0 * Math.PI * SpeedOfLight / wavelengthMeters;
            Complex i = Complex.ImaginaryOne;

            // --- Debye Model (Microwave frequencies) ---
            // epsilon_D = 1
            Complex epsD = 1.0;
            for (int k = 0; k < A.Length; k++)
            {
                // (((1 - i*tauj*omega)^(1-nu)) * (1 - i*tauf*omega))^(-1)
                Complex factor1 = Complex.Pow(1.0 - i * TauJ[k] * omega, 1.0 - Nu[k]);
                Complex factor2 = 1.0 - i * TauF[k] * omega;
                epsD += A[k] / (factor1 * factor2);
            }

            // --- Lorentz Model (Infrared and Optical frequencies) ---
            Complex epsL = 0;
            // MATLAB code skips k=1 for Lorentz as parameters are 0
            for (int k = 1; k < F.Length; k++)
            {
                double omegaRes = OmegaEv[k] * EvToRadPerSec;
                double gamma = GammaEv[k] * EvToRadPerSec;

                // (f*omegap^2) / (omegaRes^2 - omega^2 - i*gamma*omega)
                Complex denom = (omegaRes * omegaRes) - (omega * omega) - (i * gamma * omega);
                epsL += (F[k] * OmegaP * OmegaP) / denom;
            }

            Complex epsTotal = epsD + epsL;

            // n = sqrt(eps)
            return Complex.Sqrt(epsTotal);
        }
    }
}