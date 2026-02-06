using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MaterialsAndEquations
{
    /// <summary>
    /// 用光学材料的类
    /// 用于计算材料在任意给定波长下的复数折射率
    /// 
    /// 提供：
    /// - 使用波长（单位：米）作为索引器查询复数折射率（n + i k）
    /// - 可以从任意函数或离散数据点创建材料（离散数据点将做线性插值）
    /// </summary>
    public class OpticalMaterial
    {
        /// <summary>
        /// 材料名称（可选）
        /// </summary>
        public string Name { get; }

        private readonly Func<double, Complex> _refractiveIndexAtWavelength;

        /// <summary>
        /// 创建一个使用自定义函数计算折射率的材料。
        /// 波长单位约定为米（m）。
        /// </summary>
        /// <param name="name">材料名称</param>
        /// <param name="refractiveIndexFunc">给定波长（m）返回复数折射率的函数</param>
        public OpticalMaterial(string name, Func<double, Complex> refractiveIndexFunc)
        {
            Name = name ?? string.Empty;
            _refractiveIndexAtWavelength = refractiveIndexFunc ?? throw new ArgumentNullException(nameof(refractiveIndexFunc));
        }

        /// <summary>
        /// 使用离散数据点创建材料，点集会根据波长升序排列，并在查询时对复数折射率做线性插值。
        /// 波长单位约定为米（m）。
        /// </summary>
        /// <param name="name">材料名称</param>
        /// <param name="points">波长（m）到复数折射率的映射</param>
        /// <returns>OpticalMaterial 实例</returns>
        public static OpticalMaterial FromPoints(string name, IDictionary<double, Complex> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count == 0) throw new ArgumentException("points must contain at least one entry", nameof(points));

            var sorted = points.OrderBy(p => p.Key).ToArray();
            var keys = sorted.Select(p => p.Key).ToArray();
            var values = sorted.Select(p => p.Value).ToArray();

            Complex Interpolator(double wl)
            {
                // If outside range, clamp to endpoints
                if (wl <= keys[0]) return values[0];
                if (wl >= keys[keys.Length - 1]) return values[values.Length - 1];

                int idx = Array.BinarySearch(keys, wl);
                if (idx >= 0) return values[idx];
                idx = ~idx; // first index greater than wl

                var leftIdx = idx - 1;
                var rightIdx = idx;
                var leftWl = keys[leftIdx];
                var rightWl = keys[rightIdx];
                var t = (wl - leftWl) / (rightWl - leftWl);

                // 复数按实部和虚部分别线性插值
                var leftVal = values[leftIdx];
                var rightVal = values[rightIdx];
                return leftVal + (rightVal - leftVal) * t;
            }

            return new OpticalMaterial(name, Interpolator);
        }

        /// <summary>
        /// 以波长（m）为索引查询复数折射率（n + i k）。
        /// </summary>
        /// <param name="wavelengthMeters">波长，单位：米</param>
        /// <returns>复数折射率</returns>
        public Complex this[double wavelengthMeters] => _refractiveIndexAtWavelength(wavelengthMeters);

        /// <summary>
        /// 返回在给定波长处的实部（n）和虚部（k）。便于直接获取两者。
        /// </summary>
        /// <param name="wavelengthMeters">波长（m）</param>
        /// <param name="n">输出：实部</param>
        /// <param name="k">输出：虚部（吸收项）</param>
        public void GetNk(double wavelengthMeters, out double n, out double k)
        {
            var c = this[wavelengthMeters];
            n = c.Real;
            k = c.Imaginary;
        }



    }
}
