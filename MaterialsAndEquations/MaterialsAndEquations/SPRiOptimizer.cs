using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace MaterialsAndEquations
{
    public static class SPRiOptimizer
    {
        /// <summary>
        /// 
        /// 用于计算特定SPRi光学系统的检测灵敏度
        /// 
        /// SPRi光学检测灵敏度的定义为：
        /// 当materialOut的折射率发生微变化时，
        /// 反射光强度的相对变化率（即 dR/R）与materialOut折射率变化率（dnOut）的比值
        /// 
        /// </summary>
        /// <param name="materialIn"></param>
        /// <param name="thinLayers"></param>
        /// <param name="materialOut"></param>
        /// <param name="wavelength_Meters"></param>
        /// <param name="thetaIn">角度!!</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static double ComputeSPRiSensitivity(
            OpticalMaterial materialIn,
            IEnumerable<(OpticalMaterial layerMaterial, double thickness_Meters)> thinLayers,
            OpticalMaterial materialOut,
            double wavelength_Meters,
            double thetaIn,
            bool absoluteSensitivity = true)
        {
            //默认的折射率变化是Biacor 1RU
            var dRI = 1e-6;
            var material_dRI = new OpticalMaterial(
                "materialOut_dRI",
                wl => materialOut![wl] + dRI
            );

            // Validate inputs
            if (materialIn == null) throw new ArgumentNullException(nameof(materialIn));
            if (materialOut == null) throw new ArgumentNullException(nameof(materialOut));

            // Use p-polarization (TM) for SPRi sensitivity
            // Compute baseline reflectance R0
            Equations.ComputeReflectionTransmission(
                out double thetaOut0,
                out double reflection0,
                out double transmission0,
                wavelength_Meters,
                materialIn,
                thinLayers,
                materialOut,
                thetaIn,
                polarizationS: false);

            // Compute reflectance after small change in outer material refractive index
            Equations.ComputeReflectionTransmission(
                out double thetaOut1,
                out double reflection1,
                out double transmission1,
                wavelength_Meters,
                materialIn,
                thinLayers,
                material_dRI,
                thetaIn,
                polarizationS: false);

            double dR = reflection1 - reflection0;

            // relative change dR/R (if R is extremely small, fall back to absolute dR)
            double relChange;


            if (absoluteSensitivity)
            {
                relChange = dR; // use absolute change in reflectance
            }
            else
            {
                if (reflection0 < 1e-15)
                {
                    // If baseline reflectance is very small, sensitivity becomes ill-defined
                    // In this case, we can either return NaN or use absolute change as a fallback
                    return double.NaN; // or relChange = dR;
                }
                else
                {
                    relChange = dR / reflection0; // use relative change in reflectance
                }
            }

            // change in refractive index (real part)
            System.Numerics.Complex n0 = materialOut[wavelength_Meters];
            System.Numerics.Complex n1 = material_dRI[wavelength_Meters];
            double dnOut = (n1 - n0).Real;

            if (Math.Abs(dnOut) < 1e-20)
            {
                // can't compute sensitivity for zero index change
                return double.NaN;
            }

            double sensitivity = relChange / dnOut;
            return sensitivity;
        }


        /// <summary>
        /// 光学模型：
        /// 
        /// 假设有一个等腰三角形的棱镜，其底边长度非常长
        /// 透过棱镜底边，下方是一个比较薄的光学平板（slide）
        /// 厚度远大于光的波长但远小于棱镜底边长。
        /// 
        /// 需要计算从slide中心射向棱镜入射角为spriAngle的光，
        /// 折射后从棱镜腰边射出到真空(折射率恒为1.0)的光线
        /// 其与底边的中垂线的夹角。
        /// 
        /// </summary>
        /// <param name="spriInternalAngle"></param>
        /// <param name="spriExternalAngle"></param>
        /// <param name="slideMaterial"></param>
        /// <param name="prismMaterial"></param>
        /// <param name="wavelength_Meters"></param>
        /// <param name="prismAngle">
        /// prism angle是等边三角形的底角的角度。
        /// prism angle可以大于90度，此时棱镜是一个倒置的等腰梯形
        /// 且腰长近似无限大。
        /// 
        /// 
        /// </param>
        /// <exception cref="ArgumentException"></exception>
        public static void ComputeSPRiPrismAngles(
            ref double spriInternalAngle,
            ref double spriExternalAngle,
            OpticalMaterial slideMaterial,
            OpticalMaterial prismMaterial,
            double wavelength_Meters,
            double prismAngle = 60)
        {
            // 验证输入参数：必须有且仅有一个角度是NaN
            if (double.IsNaN(spriInternalAngle) == double.IsNaN(spriExternalAngle))
                throw new ArgumentException("Exactly one of spriInternalAngle or spriExternalAngle must be NaN");

            // helper
            static double Deg2Rad(double d) => d * Math.PI / 180.0;
            static double Rad2Deg(double r) => r * 180.0 / Math.PI;
            static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

            // get real refractive indices (use real part)
            double nSlide = slideMaterial[wavelength_Meters].Real;
            double nPrism = prismMaterial[wavelength_Meters].Real;
            var prismRad = Deg2Rad(prismAngle);

            //棱镜外角度为NaN -> 已知SPRi光学角度计算棱镜外角度
            if (double.IsNaN(spriExternalAngle))
            {
                //根据SPRi芯片入射角度(入射光和芯片法线的夹角)
                //计算SPRi棱镜外入射角度(入射光和中垂线的夹角)

                //棱镜外角度未知需要计算SPRi棱镜外入射角度
                if (double.IsNaN(spriInternalAngle))
                {
                    spriExternalAngle = double.NaN;
                    return;
                }

                //光路可逆：从slide到棱镜的折射
                var slidePrismRad = Deg2Rad(spriInternalAngle);
                var sinSlidePrism = Math.Sin(slidePrismRad);
                var sinPrismSlide = nSlide * sinSlidePrism / nPrism;

                //检查全反射
                if (Math.Abs(sinPrismSlide) > 1.0)
                {
                    spriExternalAngle = double.NaN;
                    return;
                }

                var prismSlideRad = Math.Asin(Clamp(sinPrismSlide, -1.0, 1.0));

                //从几何关系计算棱镜内光线射向侧面的角度
                //prismSlideRad + prismAirRad === prismRad
                var prismAirRad = prismRad - prismSlideRad;

                //从棱镜到空气的折射
                var sinPrismAir = Math.Sin(prismAirRad);
                var sinAirPrism = nPrism * sinPrismAir;

                //检查全反射
                if (Math.Abs(sinAirPrism) > 1.0)
                {
                    spriExternalAngle = double.NaN;
                    return;
                }

                var airPrismRad = Math.Asin(Clamp(sinAirPrism, -1.0, 1.0));

                //从几何关系计算外部入射角
                //airPrismRad === prismAngle - spriExternalAngle
                spriExternalAngle = Rad2Deg(prismRad - airPrismRad);
            }
            else
            {
                //根据SPRi棱镜外入射角度(入射光和中垂线的夹角)
                //计算SPRi芯片入射角度(入射光和芯片法线的夹角)

                //棱镜外角度已知需要计算SPRi芯片入射角度
                if (double.IsNaN(spriExternalAngle))
                {
                    spriInternalAngle = double.NaN;
                    return;
                }

                //计算棱镜的入射角
                //-------------------
                //      \ prismAngle
                //       \
                //       /\
                //      /  \
                //     /....\_______
                //    /|
                //   / |
                //  /  |
                // /   |
                // spriAngle
                //
                // (90 - airPrismAngle) + (90 - spriAngle)
                // === (180 - prismAngle)
                //
                // prismAngle - spriAngle === airPrismAngle

                double airPrismRad = Deg2Rad(prismAngle - spriExternalAngle);
                var sinAirPrism = Math.Sin(airPrismRad);
                var sinPrismAir = sinAirPrism / nPrism;
                var prismAirRad = Math.Asin(sinPrismAir);


                //计算棱镜的入射角
                //-------------------
                //      \    /
                //       \  /
                //        \/
                //         \
                //          \_______
                //PrismAngle + (90 - PrismSlideIncidenceAngle) ===
                //(90 + PrismAirIncidenceAngle)
                //
                //PrismSlideAngle === PrismAngle - PrismAirIncidenceAngle

                var prismSlideRad = prismRad - prismAirRad;
                var sinPrismSlide = Math.Sin(prismSlideRad);
                var sinSlidePrism = nPrism * sinPrismSlide / nSlide;
                var slidePrismRad = Math.Asin(sinSlidePrism);
                spriInternalAngle = Rad2Deg(slidePrismRad);
            }
        }

        public static double OptimizeSPRiAngleIn(
            OpticalMaterial materialIn,
            IEnumerable<(OpticalMaterial material, double thickness_Meters)> thinLayers,
            OpticalMaterial materialOut,
            double wavelength_Meters,
            double thetaIn_Guess,
            double thetaIn_Min = 5,
            double thetaIn_Max = 85,
            bool absoluteSensitivity = true,
            int maxIterations = 100,
            double tolerance = 1e-6)
        {
            // Validate inputs
            if (materialIn == null) throw new ArgumentNullException(nameof(materialIn));
            if (materialOut == null) throw new ArgumentNullException(nameof(materialOut));
            if (thinLayers == null) throw new ArgumentNullException(nameof(thinLayers));

            // Clamp the guess to be within bounds
            var thetaIn_Initial = Math.Max(thetaIn_Min, Math.Min(thetaIn_Max, thetaIn_Guess));
            var thinLayersList = thinLayers.ToList();

            // Define the objective function to be minimized (negative sensitivity)
            double GetNegativeSensitivity(Vector<double> parameters)
            {
                try
                {
                    double thetaIn = parameters[0];

                    // Validate bounds
                    if (thetaIn < thetaIn_Min || thetaIn > thetaIn_Max)
                        return 1e10;

                    // Compute sensitivity with current angle
                    double sensitivity = ComputeSPRiSensitivity(
                        materialIn,
                        thinLayersList,
                        materialOut,
                        wavelength_Meters,
                        thetaIn,
                        absoluteSensitivity);

                    // Return NaN or invalid sensitivity as a large penalty
                    if (double.IsNaN(sensitivity) || double.IsInfinity(sensitivity))
                        return 1e10;

                    // Return negative sensitivity (optimizer minimizes, we want to maximize)
                    return -sensitivity;
                }
                catch
                {
                    return 1e10;
                }
            }

            // Create bounds and initial guess vectors
            var lowerBounds = Vector<double>.Build.DenseOfArray(new[] { thetaIn_Min });
            var upperBounds = Vector<double>.Build.DenseOfArray(new[] { thetaIn_Max });
            var initialGuess = Vector<double>.Build.DenseOfArray(new[] { thetaIn_Initial });

            // Perform optimization
            Vector<double> resultPoint = initialGuess;
            try
            {
                resultPoint = FindMinimum.OfFunctionConstrained(
                    GetNegativeSensitivity,
                    lowerBounds,
                    upperBounds,
                    initialGuess,
                    gradientTolerance: tolerance,
                    parameterTolerance: tolerance,
                    functionProgressTolerance: tolerance,
                    maxIterations: maxIterations);
            }
            catch (MathNet.Numerics.Optimization.MaximumIterationsException)
            {
                // Optimization reached max iterations but may have found a reasonable solution
                // Continue with best result found so far
            }
            catch (Exception ex)
            {
                // Log or handle other exceptions
                throw new InvalidOperationException("Optimization failed", ex);
            }

            return resultPoint[0];
        }

        public static double OptimizeSPRiAngleInByEnumeration(
            OpticalMaterial materialIn,
            IEnumerable<(OpticalMaterial material, double thickness_Meters)> thinLayers,
            OpticalMaterial materialOut,
            double wavelength_Meters,
            double thetaIn_Min = 5,
            double thetaIn_Max = 85,
            double thetaIn_Steps = 401,
            bool absoluteSensitivity = true,
            int maxIterations = 100,
            double tolerance = 1e-6)
        {
            //TODO
            //通过遍历的方法，调用函数计算每个遍历角度的灵敏度
            //作为使用Minimization方法优化的前一步先进行全局搜索避免卡在局部最优解

            throw new NotImplementedException();

        }

        public static double[] OptimizeSPRiLayerThicknessesAndAngleIn(
            OpticalMaterial materialIn,
            IEnumerable<OpticalMaterial> thinLayerMaterials,
            IEnumerable<(double initialGuess, double lowerBound, double upperBound)> optimizations_Meters,
            OpticalMaterial materialOut,
            double wavelength_Meters,
            double thetaIn_Guess,
            double thetaIn_Min = 5,
            double thetaIn_Max = 85,
            bool absoluteSensitivity = true)
        {
            //在给定多层膜构成的基础上，优化每层膜的厚度以最大化SPRi检测灵敏度
            //对于每个厚度组合，使用OptimizeSPRiAngleIn函数计算最优的入射角

            //检查所有材料有效
            if (materialIn == null) throw new ArgumentNullException(nameof(materialIn));
            if (materialOut == null) throw new ArgumentNullException(nameof(materialOut));
            if (thinLayerMaterials == null) throw new ArgumentNullException(nameof(thinLayerMaterials));
            if (optimizations_Meters == null) throw new ArgumentNullException(nameof(optimizations_Meters));

            var thinLayersList = thinLayerMaterials.ToList();
            var optimizationsList = optimizations_Meters.ToList();

            //至少需要有一层膜
            if (thinLayersList.Count == 0)
                throw new ArgumentException("At least one layer must be provided");
            //检查层数数量正确
            if (thinLayersList.Count != optimizationsList.Count)
                throw new ArgumentException(
                    "Number of layers must match number of optimizations");

            //使用nm作为优化单位以提高数值稳定性
            //避免数值相对优化步长过大导致优化算法无法正确更新参数
            var initialGuess = Vector<double>.Build
                .DenseOfEnumerable(optimizationsList
                    .Select(o => o.initialGuess * 1e+9));
            var lowerBounds = Vector<double>.Build
                .DenseOfEnumerable(optimizationsList
                    .Select(o => o.lowerBound * 1e+9));
            var upperBounds = Vector<double>.Build
                .DenseOfEnumerable(optimizationsList
                    .Select(o => o.upperBound * 1e+9));

            int numLayers = thinLayersList.Count;

            //定义优化函数
            double GetNegativeSensitivity(Vector<double> parameters)
            {
                try
                {
                    //重构当前层厚度组合
                    var updatedLayers = new List<(OpticalMaterial layerMaterial, double thickness_Meters)>();
                    for (int i = 0; i < numLayers; i++)
                        updatedLayers.Add((thinLayersList[i], parameters[i] * 1e-9));

                    // 使用OptimizeSPRiAngleIn函数计算最优的入射角
                    double optimalAngle = OptimizeSPRiAngleIn(
                        materialIn,
                        updatedLayers,
                        materialOut,
                        wavelength_Meters,
                        thetaIn_Guess: thetaIn_Guess,
                        thetaIn_Min: thetaIn_Min,
                        thetaIn_Max: thetaIn_Max,
                        absoluteSensitivity: absoluteSensitivity);

                    //计算所在优化角度的灵敏度
                    double sensitivity = ComputeSPRiSensitivity(
                        materialIn,
                        updatedLayers,
                        materialOut,
                        wavelength_Meters,
                        optimalAngle,
                        absoluteSensitivity);

                    // Return NaN or invalid sensitivity as a large penalty
                    if (double.IsNaN(sensitivity) || double.IsInfinity(sensitivity))
                    {
                        System.Diagnostics.Debug.Assert(false);
                        return 1e10;
                    }
                       

                    // Return negative sensitivity (optimizer minimizes, we want to maximize)
                    return -sensitivity;
                }
                catch
                {
                    System.Diagnostics.Debug.Assert(false);
                    return 1e10;
                }
            }

            //优化参数层参数
            Vector<double> resultPoint = initialGuess;
            try
            {
                resultPoint = FindMinimum.OfFunctionConstrained(
                    GetNegativeSensitivity,
                    lowerBounds,
                    upperBounds,
                    initialGuess);
            }
            catch (MathNet.Numerics.Optimization.MaximumIterationsException)
            {
                // Optimization reached max iterations but may have found a reasonable solution
                // Continue with best result found so far
            }
            catch (Exception ex)
            {
                // Log or handle other exceptions
                throw new InvalidOperationException("Optimization failed", ex);
            }

            // 返回优化后的层厚度组合
            return (resultPoint * 1e-9).ToArray();
        }


        public static double OptimizeSPRiWaveLength(
            OpticalMaterial materialIn,
            IEnumerable<OpticalMaterial> thinLayerMaterials,
            IEnumerable<(double initialGuess, double lowerBound, double upperBound)> optimizations_Meters,
            OpticalMaterial materialOut,
            double wavelength_Guess_Meters,
            double thetaIn_Guess,
            double wavelength_Min_Meters = 400e-9,
            double wavelength_Max_Meters = 1100e-9,
            double thetaIn_Min = 5,
            double thetaIn_Max = 85,
            bool absoluteSensitivity = true)
        {
            // 检查所有材料有效
            if (materialIn == null) throw new ArgumentNullException(nameof(materialIn));
            if (materialOut == null) throw new ArgumentNullException(nameof(materialOut));
            if (thinLayerMaterials == null) throw new ArgumentNullException(nameof(thinLayerMaterials));
            if (optimizations_Meters == null) throw new ArgumentNullException(nameof(optimizations_Meters));

            var thinLayersList = thinLayerMaterials.ToList();
            var optimizationsList = optimizations_Meters.ToList();

            // 至少需要有一层膜
            if (thinLayersList.Count == 0)
                throw new ArgumentException("At least one layer must be provided");
            // 检查层数数量正确
            if (thinLayersList.Count != optimizationsList.Count)
                throw new ArgumentException(
                    "Number of layers must match number of optimizations");

            // 使用 nm 作为波长优化单位以提高数值稳定性
            var wavelength_Initial = Math.Max(wavelength_Min_Meters, Math.Min(wavelength_Max_Meters, wavelength_Guess_Meters));
            var initialGuess = Vector<double>.Build
                .DenseOfArray(new[] { wavelength_Initial * 1e+9 });
            var lowerBounds = Vector<double>.Build
                .DenseOfArray(new[] { wavelength_Min_Meters * 1e+9 });
            var upperBounds = Vector<double>.Build
                .DenseOfArray(new[] { wavelength_Max_Meters * 1e+9 });

            // 定义优化函数
            double GetNegativeSensitivity(Vector<double> parameters)
            {
                try
                {
                    // 从 nm 转换为米
                    double currentWavelength = parameters[0] * 1e-9;

                    // 对于当前波长，优化层厚度和入射角
                    double[] optimizedThicknesses = OptimizeSPRiLayerThicknessesAndAngleIn(
                        materialIn,
                        thinLayersList,
                        optimizationsList,
                        materialOut,
                        currentWavelength,
                        thetaIn_Guess,
                        thetaIn_Min,
                        thetaIn_Max,
                        absoluteSensitivity);

                    // 重构层配置
                    var updatedLayers = new List<(OpticalMaterial layerMaterial, double thickness_Meters)>();
                    for (int i = 0; i < thinLayersList.Count; i++)
                        updatedLayers.Add((thinLayersList[i], optimizedThicknesses[i]));

                    // 计算最优入射角
                    double optimalAngle = OptimizeSPRiAngleIn(
                        materialIn,
                        updatedLayers,
                        materialOut,
                        currentWavelength,
                        thetaIn_Guess: thetaIn_Guess,
                        thetaIn_Min: thetaIn_Min,
                        thetaIn_Max: thetaIn_Max,
                        absoluteSensitivity: absoluteSensitivity);

                    // 计算当前波长和优化参数下的灵敏度
                    double sensitivity = ComputeSPRiSensitivity(
                        materialIn,
                        updatedLayers,
                        materialOut,
                        currentWavelength,
                        optimalAngle,
                        absoluteSensitivity);

                    // Return NaN or invalid sensitivity as a large penalty
                    if (double.IsNaN(sensitivity) || double.IsInfinity(sensitivity))
                    {
                        return 1e10;
                    }

                    // Return negative sensitivity (optimizer minimizes, we want to maximize)
                    return -sensitivity;
                }
                catch
                {
                    return 1e10;
                }
            }

            // 优化波长参数
            Vector<double> resultPoint = initialGuess;
            try
            {
                resultPoint = FindMinimum.OfFunctionConstrained(
                    GetNegativeSensitivity,
                    lowerBounds,
                    upperBounds,
                    initialGuess);
            }
            catch (MathNet.Numerics.Optimization.MaximumIterationsException)
            {
                // Optimization reached max iterations but may have found a reasonable solution
                // Continue with best result found so far
            }
            catch (Exception ex)
            {
                // Log or handle other exceptions
                throw new InvalidOperationException("Optimization failed", ex);
            }

            // 返回优化后的波长（米单位）
            return resultPoint[0] * 1e-9;
        }
    }
}
