using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MaterialsAndEquations
{
    /// <summary>
    /// 使用 Sellmeier 单项式（Sellmeier 1）表示的透明介质（例如玻璃）折射率。
    ///
    /// 公式（波长单位：米 m）：
    /// n(λ) = sqrt( 1 + Σ_i (K_i * λ^2) / (λ^2 - L_i) )
    ///
    /// 参数约定：每一项为 (K_i, L_i)，L_i 的单位应为米的平方（m^2），K_i 为无量纲系数。
    /// </summary>
    public class SellmeierMaterial : OpticalMaterial
    {
        private readonly (double K, double L)[] _terms;

        /// <summary>
        /// 返回用于 Sellmeier 计算的参数列表（只读副本）。
        /// 每项为 (K, L)；L 单位为 m^2。
        /// </summary>
        public IReadOnlyList<(double K, double L)> Terms => Array.AsReadOnly(_terms);

        /// <summary>
        /// 创建一个 SellmeierMaterial。
        /// </summary>
        /// <param name="name">材料名称</param>
        /// <param name="terms">色散参数序列，每项为 (K_i, L_i)。L_i 单位应为 m^2。</param>
        public SellmeierMaterial(string name, IEnumerable<(double K, double L)> terms)
            : base(name, MakeFunc((terms ?? throw new ArgumentNullException(nameof(terms))).ToArray()))
        {
            var arr = (terms ?? throw new ArgumentNullException(nameof(terms))).ToArray();
            if (arr.Length == 0) throw new ArgumentException("At least one Sellmeier term must be provided", nameof(terms));
            _terms = arr;
        }

        private static Func<double, Complex> MakeFunc((double K, double L)[] arr)
        {
            return wl => RefractiveIndexAtWavelength(wl, arr);
        }

        private static Complex RefractiveIndexAtWavelength(double wavelengthMeters, (double K, double L)[] terms)
        {
            if (wavelengthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(wavelengthMeters), "wavelength must be positive and in meters");

            // Sellmeier 公式通常使用波长的平方，因此先计算 λ^2。
            // 为了避免数值问题，内部使用 μm 单位进行计算。
            double wl2 = wavelengthMeters * 1E+6;
            wl2 *= wl2; // λ^2 in um^2
            Complex sum = Complex.Zero;

            foreach (var (K, L) in terms)
            {
                double denom = wl2 - L;
                if (denom == 0.0)
                {
                    //避免出现分母为零的情况，这会导致折射率趋于无穷大（物理上对应于强烈的吸收峰）。
                    //在实际应用中，L 通常不会恰好等于某个波长的平方，但我们应该处理这个边界情况。
                    throw new ArgumentException($"分母为0", nameof(wavelengthMeters));
                }

                sum += (K * wl2) / denom;
            }

            return Complex.Sqrt(1.0 + sum);
        }
    }
}
