using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MaterialsAndEquations
{
    /// <summary>
    /// 使用 Schott 公式表示的透明介质折射率。
    ///
    /// 公式（波长单位：micrometer, μm）：
    /// n(λ) = sqrt( A0 + A1*λ^2 + A2*λ^-2 + A3*λ^-4 + A4*λ^-6 + A5*λ^-8 )
    ///
    /// 构造时提供系数 A0..A5（可以提供少于 6 项，将用 0 补齐）。
    /// 索引器使用波长单位为米（m），内部会转换为 μm。
    /// </summary>
    public class SchottMaterial : OpticalMaterial
    {
        private readonly double[] _coeffs; // length 6

        /// <summary>
        /// 返回 Schott 系数 (A0..A5) 的只读副本。
        /// </summary>
        public IReadOnlyList<double> Coefficients => Array.AsReadOnly(_coeffs);

        /// <summary>
        /// 创建一个 SchottMaterial。
        /// coeffs 可提供 1 到 6 个系数，缺省项将被视为 0。
        /// </summary>
        /// <param name="name">材料名</param>
        /// <param name="coeffs">Schott 系数 A0..A5</param>
        public SchottMaterial(string name, params double[] coeffs)
            : base(name, MakeFunc((coeffs ?? throw new ArgumentNullException(nameof(coeffs)))))
        {
            var arr = coeffs ?? throw new ArgumentNullException(nameof(coeffs));
            if (arr.Length == 0) throw new ArgumentException("At least one Schott coefficient must be provided", nameof(coeffs));

            _coeffs = new double[6];
            for (int i = 0; i < Math.Min(6, arr.Length); i++) _coeffs[i] = arr[i];
        }

        /// <summary>
        /// 创建 SchottMaterial，从 IEnumerable<double> 获取系数。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="coeffsEnum"></param>
        public SchottMaterial(string name, IEnumerable<double> coeffsEnum)
            : this(name, (coeffsEnum ?? throw new ArgumentNullException(nameof(coeffsEnum))).ToArray())
        {
        }

        private static Func<double, Complex> MakeFunc(double[] coeffs)
        {
            // copy to ensure immutability
            var cpy = (coeffs ?? throw new ArgumentNullException(nameof(coeffs))).ToArray();
            return wl => RefractiveIndexAtWavelength(wl, cpy);
        }

        private static Complex RefractiveIndexAtWavelength(double wavelengthMeters, double[] coeffs)
        {
            if (wavelengthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(wavelengthMeters), "wavelength must be positive and in meters");

            // Convert meters to micrometers (1 m = 1e6 um)
            double wlUm = wavelengthMeters * 1e6;

            double wl2 = wlUm * wlUm; // λ^2
            double wlMinus2 = wl2 != 0.0 ? 1.0 / wl2 : double.PositiveInfinity; // λ^-2

            // compute powers
            double wlMinus4 = wlMinus2 * wlMinus2; // λ^-4
            double wlMinus6 = wlMinus4 * wlMinus2; // λ^-6
            double wlMinus8 = wlMinus4 * wlMinus4; // λ^-8

            double a0 = coeffs.Length > 0 ? coeffs[0] : 0.0;
            double a1 = coeffs.Length > 1 ? coeffs[1] : 0.0;
            double a2 = coeffs.Length > 2 ? coeffs[2] : 0.0;
            double a3 = coeffs.Length > 3 ? coeffs[3] : 0.0;
            double a4 = coeffs.Length > 4 ? coeffs[4] : 0.0;
            double a5 = coeffs.Length > 5 ? coeffs[5] : 0.0;

            double value = a0 + a1 * wl2 + a2 * wlMinus2 + a3 * wlMinus4 + a4 * wlMinus6 + a5 * wlMinus8;

            return Complex.Sqrt(value);
        }
    }
}
