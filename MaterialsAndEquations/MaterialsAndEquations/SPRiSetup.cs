using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MaterialsAndEquations.KnownMaterials.KnownMaterials;

namespace MaterialsAndEquations
{
    public class SPRiSetup
    {
        #region 属性

        public OpticalMaterial PrismMaterial { get; set; }
            = new("Dummy Material", s => 1.715);

        public OpticalMaterial SlideMaterial { get; set; }
            = new("Dummy Material", s => 1.521);

        public OpticalMaterial AnalyteMaterial { get; set; }
            = new("Dummy Material", s => 1.330);

        /// <summary>
        /// 计算灵敏度时候所使用的穿透深度，
        /// 基于理想建模穿透深度内的
        /// </summary>
        public double PenetrationDepth
        {
            get; set;
        } = double.PositiveInfinity;

        public IList<(OpticalMaterial material, double thickness_Meters)> SPRiLayers
        {
            get;
            private init;
        } = new List<(OpticalMaterial material, double thickness_Meters)>();

        public IList<(double lowerLimit, double upperLimit)>
            LayerOptimizationRanges
        {
            get;
            init;
        } = new List<(double, double)>();

        public double Wavelength_Meters { get; set; }

        public double DefaultThetaIn { get; set; }

        public double ThetaOptimizationRangeMin { get; set; } = 5;
        public double ThetaOptimizationRangeMax { get; set; } = 85;

        #endregion

        #region 工具方法

        public double GetExternalSPRiAngle(double internalAngle = double.NaN)
        {
            if (double.IsNaN(internalAngle))
                internalAngle = DefaultThetaIn;

            var externalAngle = double.NaN;

            SPRiOptimizer.ComputeSPRiPrismAngles(
                ref internalAngle, ref externalAngle,
                SlideMaterial, PrismMaterial, Wavelength_Meters);
            return externalAngle;
        }

        public double GetInternalSPRiAngle(double externalAngle)
        {
            var internalAngle = double.NaN;

            SPRiOptimizer.ComputeSPRiPrismAngles(
                ref internalAngle, ref externalAngle,
                SlideMaterial, PrismMaterial, Wavelength_Meters);
            return internalAngle;
        }

        public double ComputeReflection()
        {
            Equations.ComputeReflectionTransmission(
                out _,
                out double reflection,
                out _,
                Wavelength_Meters,
                SlideMaterial,
                SPRiLayers,
                AnalyteMaterial, DefaultThetaIn, false);

            return reflection;
        }

        public double ComputeSensitivity(bool isAbsoluteSensitivity = true)
        {
            return SPRiOptimizer.ComputeSPRiSensitivity(
                SlideMaterial,
                SPRiLayers,
                AnalyteMaterial,
                Wavelength_Meters,
                DefaultThetaIn,
                PenetrationDepth,
                isAbsoluteSensitivity);
        }

        public void CheckAndCorrectCurrentSetup()
        {
            //检查当前模型无效的参数并修正

            //检查层厚度
            //要求1：厚度大等于0，如果小于0则赋值0
            //要求2：如果LayerOptimizationRange进行了范围约束，
            //      超范围的厚度值被限制回边界值。
            //      如果LayerOptimizationRange长度不等于层数且不等于零，
            //      说明设置存在错误，应当抛出异常

            if (LayerOptimizationRanges.Count != 0 && 
                LayerOptimizationRanges.Count != SPRiLayers.Count)
                throw new ArgumentException("LayerOptimizationRanges count must be zero or match layer count.");

            for (int i = 0; i < SPRiLayers.Count; i++)
            {
                var layer = SPRiLayers[i];
                var thickness = layer.thickness_Meters;

                if (thickness < 0)
                    thickness = 0;

                if (LayerOptimizationRanges.Count == SPRiLayers.Count)
                {
                    var (lowerLimit, upperLimit) = LayerOptimizationRanges[i];
                    if (thickness < lowerLimit)
                        thickness = lowerLimit;
                    else if (thickness > upperLimit)
                        thickness = upperLimit;
                }

                if (!Equals(thickness, layer.thickness_Meters))
                    SPRiLayers[i] = (layer.material, thickness);
            }
        }

        public SPRiSetup Clone() => new SPRiSetup(this);

        public void WriteInfo()
        {
            var wavelengthNm = this.Wavelength_Meters * 1e9;

            var prismNk = this.PrismMaterial[this.Wavelength_Meters];
            var slideNk = this.SlideMaterial[this.Wavelength_Meters];
            var analyteNk = this.AnalyteMaterial[this.Wavelength_Meters];

            Console.WriteLine($"PrismMaterial: {this.PrismMaterial.Name}, n: {prismNk.Real:F3}, k: {prismNk.Imaginary:F3}");
            Console.WriteLine($"SlideMaterial: {this.SlideMaterial.Name}, n: {slideNk.Real:F3}, k: {slideNk.Imaginary:F3}");
            Console.WriteLine($"AnalyteMaterial: {this.AnalyteMaterial.Name}, n: {analyteNk.Real:F3}, k: {analyteNk.Imaginary:F3}");
            Console.WriteLine($"Wavelength_nm: {wavelengthNm:F3}");
            Console.WriteLine($"DefaultThetaIn: {this.DefaultThetaIn}");

            for (int i = 0; i < this.SPRiLayers.Count; i++)
            {
                var layer = this.SPRiLayers[i];
                var layerNk = layer.material[this.Wavelength_Meters];
                var thicknessNm = layer.thickness_Meters * 1e9;
                Console.WriteLine($"Layer {i}: {layer.material.Name:F3}, Thickness_nm: {thicknessNm:F3}, n: {layerNk.Real:F3}, k: {layerNk.Imaginary:F3}");
            }

            Console.WriteLine();
            Console.WriteLine($"Optimized Reflection = {this.ComputeReflection():F3}");
            Console.WriteLine($"Absolute Sensitivity = {this.ComputeSensitivity():F3}");
            Console.WriteLine($"Relative Sensitivity = {this.ComputeSensitivity(false):F3}");
        }

        #endregion

        #region 方法

        public double OptimizeThetaIn(
            double thetaIn = double.NaN,
            bool isAbsoluteSensitivity = true)
        {
            if (double.IsNaN(thetaIn))
                thetaIn = DefaultThetaIn;

            return SPRiOptimizer.OptimizeSPRiAngleIn(
                SlideMaterial,
                SPRiLayers,
                AnalyteMaterial,
                Wavelength_Meters,
                thetaIn,
                depth_Meters: PenetrationDepth,
                absoluteSensitivity: isAbsoluteSensitivity);
        }

        public double[] OptimizeLayerThickness(
            (double init, double lowerbound, double upperbound)[]?
                optimizationRange_Meters = null,
            bool isAbsoluteSensitivity = true)
        {
            //如果不传入，默认在0-2倍范围内优化
            if (optimizationRange_Meters is null)
            {
                if (LayerOptimizationRanges.Count == SPRiLayers.Count)
                {
                    //如果模型已经预置了限制范围
                    optimizationRange_Meters = SPRiLayers.Select((s, id) =>
                    {
                        if (s.thickness_Meters < LayerOptimizationRanges[id].lowerLimit ||
                            s.thickness_Meters > LayerOptimizationRanges[id].upperLimit)
                            throw new ArgumentException("Incompatible Data");

                        return (s.thickness_Meters,
                            LayerOptimizationRanges[id].lowerLimit,
                            LayerOptimizationRanges[id].upperLimit);
                    }).ToArray();
                }
                else
                {
                    optimizationRange_Meters = SPRiLayers.Select(s =>
                        (s.thickness_Meters, 0.0, s.thickness_Meters * 2)).ToArray();
                }
            }
            else
            {
                //如果函数直接传入了参数优化范围，则以函数传入的为准。
            }


            return SPRiOptimizer.OptimizeSPRiLayerThicknessesAndAngleIn(
                SlideMaterial,
                SPRiLayers.Select(s => s.material),
                optimizationRange_Meters,
                AnalyteMaterial,
                Wavelength_Meters,
                DefaultThetaIn,
                depth_Meters: PenetrationDepth,
                absoluteSensitivity: isAbsoluteSensitivity).ToArray();
        }

        public double OptimizeWavelength(
            double wavelengthInit = double.NaN,
            double wavelengthLowerBound = double.NaN,
            double wavelengthUpperBound = double.NaN,
            (double init, double lowerbound, double upperbound)[]?
                optimizationRange_Meters = null,
            bool isAbsoluteSensitivity = true)
        {
            //如果不传入，默认在0-2倍范围内优化
            if (optimizationRange_Meters is null)
                optimizationRange_Meters = SPRiLayers.Select(s =>
                    (s.thickness_Meters, 0.0, s.thickness_Meters * 2)).ToArray();

            if (double.IsNaN(wavelengthInit))
                wavelengthInit = Wavelength_Meters;
            if (double.IsNaN(wavelengthLowerBound))
                wavelengthLowerBound = wavelengthInit - 250e-9;
            if (double.IsNaN(wavelengthUpperBound))
                wavelengthUpperBound = wavelengthInit + 250e-9;

            return SPRiOptimizer.OptimizeSPRiWaveLength(
                SlideMaterial,
                SPRiLayers.Select(s => s.material),
                optimizationRange_Meters,
                AnalyteMaterial,
                wavelengthInit,
                DefaultThetaIn,
                wavelength_Min_Meters: wavelengthLowerBound,
                wavelength_Max_Meters: wavelengthUpperBound,
                depth_Meters: PenetrationDepth,
                absoluteSensitivity: isAbsoluteSensitivity);
        }


        public SPRiSetup GetOptiInstance_ThetaIn(
            double thetaIn = double.NaN,
            bool isAbsoluteSensitivity = true)
        {
            if (double.IsNaN(thetaIn))
                thetaIn = DefaultThetaIn;

            var optimizedThetaIn = SPRiOptimizer.OptimizeSPRiAngleIn(
                SlideMaterial,
                SPRiLayers,
                AnalyteMaterial,
                Wavelength_Meters,
                thetaIn,
                depth_Meters: PenetrationDepth,
                absoluteSensitivity: isAbsoluteSensitivity);

            var toRet = Clone();
            toRet.DefaultThetaIn = optimizedThetaIn;
            return toRet;
        }

        public SPRiSetup GetOptiInstance_LayerThickness(
            (double init, double lowerbound, double upperbound)[]?
                optimizationRange_Meters = null,
            bool isAbsoluteSensitivity = true)
        {
            if (optimizationRange_Meters is null)
                optimizationRange_Meters = SPRiLayers.Select(s =>
                    (s.thickness_Meters, 0.0, s.thickness_Meters * 2)).ToArray();

            var optimizedThicknesses = SPRiOptimizer.OptimizeSPRiLayerThicknessesAndAngleIn(
                SlideMaterial,
                SPRiLayers.Select(s => s.material),
                optimizationRange_Meters,
                AnalyteMaterial,
                Wavelength_Meters,
                DefaultThetaIn,
                absoluteSensitivity: isAbsoluteSensitivity);

            var updatedLayers = SPRiLayers
                .Select((layer, index) => (layer.material, optimizedThicknesses[index]))
                .ToList();

            var toRet = Clone();
            toRet.SPRiLayers.Clear();
            foreach (var layer in updatedLayers)
                toRet.SPRiLayers.Add(layer);
            toRet.CheckAndCorrectCurrentSetup();
            return toRet.GetOptiInstance_ThetaIn(isAbsoluteSensitivity: true);

        }

        public SPRiSetup GetOptiInstanceByEnum_Angle()
        {
            var optiAngle = SPRiOptimizer.OptimizeSPRiAngleInByEnumeration(
                SlideMaterial,
                SPRiLayers,
                AnalyteMaterial,
                Wavelength_Meters,
                ThetaOptimizationRangeMin,
                ThetaOptimizationRangeMax,
                depth_Meters: PenetrationDepth);

            var toRet = Clone();
            toRet.DefaultThetaIn = optiAngle;
            return toRet;
        }


        public SPRiSetup GetOptiInstanceByEnum_LayerThickness()
        {
            var optiLayers = SPRiOptimizer
                .OptimizeSPRiLayerThicknessesAndAngleInByEnumeration(
                    SlideMaterial,
                    SPRiLayers.Select(s => s.material),
                    LayerOptimizationRanges.Select(s =>
                    {
                        if (s.upperLimit - s.lowerLimit > 5e-8)
                            return (50, s.lowerLimit, s.upperLimit);
                        else return (1 + (int)((s.upperLimit - s.lowerLimit) / 1e-9),
                            s.lowerLimit, s.upperLimit);
                    }),
                    AnalyteMaterial,
                    Wavelength_Meters,
                    depth_Meters: PenetrationDepth
                );

            var toRet = Clone();
            toRet.SPRiLayers.Clear();
            foreach (var layer in SPRiLayers.Select((s, id) => (s.material, optiLayers[id])))
                toRet.SPRiLayers.Add(layer);

            return toRet.GetOptiInstanceByEnum_Angle();
        }

        public SPRiSetup GetOptiInstance_Wavelength(
            double wavelengthInit = double.NaN,
            double wavelengthLowerBound = double.NaN,
            double wavelengthUpperBound = double.NaN,
            (double init, double lowerbound, double upperbound)[]?
                optimizationRange_Meters = null,
            bool isAbsoluteSensitivity = true)
        {
            if (optimizationRange_Meters is null)
                optimizationRange_Meters = SPRiLayers.Select(s =>
                    (s.thickness_Meters, 0.0, s.thickness_Meters * 2)).ToArray();

            if (double.IsNaN(wavelengthInit))
                wavelengthInit = Wavelength_Meters;
            if (double.IsNaN(wavelengthLowerBound))
                wavelengthLowerBound = wavelengthInit - 250e-9;
            if (double.IsNaN(wavelengthUpperBound))
                wavelengthUpperBound = wavelengthInit + 250e-9;

            var optimizedWavelength = SPRiOptimizer.OptimizeSPRiWaveLength(
                SlideMaterial,
                SPRiLayers.Select(s => s.material),
                optimizationRange_Meters,
                AnalyteMaterial,
                wavelengthInit,
                DefaultThetaIn,
                wavelength_Min_Meters: wavelengthLowerBound,
                wavelength_Max_Meters: wavelengthUpperBound,
                absoluteSensitivity: isAbsoluteSensitivity);

            var toRet = Clone();
            toRet.Wavelength_Meters = optimizedWavelength;
            return toRet.GetOptiInstance_LayerThickness();
        }

        #endregion

        #region 构造函数

        static SPRiSetup()
        {
            LoadBuiltInSetups();
        }


        [Obsolete("此类禁止使用空构造函数创建")]
        private SPRiSetup()
        {
            //空构造函数是禁用的
            throw new NotImplementedException();
        }

        public SPRiSetup(SPRiSetup cloneFrom)
        {
            PrismMaterial = cloneFrom.PrismMaterial;
            SlideMaterial = cloneFrom.SlideMaterial;
            SPRiLayers = cloneFrom.SPRiLayers.ToList();
            AnalyteMaterial = cloneFrom.AnalyteMaterial;
            Wavelength_Meters = cloneFrom.Wavelength_Meters;
            DefaultThetaIn = cloneFrom.DefaultThetaIn;
            PenetrationDepth = cloneFrom.PenetrationDepth;
            ThetaOptimizationRangeMin = cloneFrom.ThetaOptimizationRangeMin;
            ThetaOptimizationRangeMax = cloneFrom.ThetaOptimizationRangeMax;
            LayerOptimizationRanges = cloneFrom.LayerOptimizationRanges.ToList();
        }

        public SPRiSetup(
            OpticalMaterial prismMaterial,
            OpticalMaterial slideMaterial,
            IEnumerable<(OpticalMaterial, double thickness_Meters)> layers,
            OpticalMaterial analyteMaterial,
            double wavelength_Meters,
            double thetaInDefault)
        {
            if (prismMaterial == null) throw new ArgumentNullException(nameof(prismMaterial));
            if (slideMaterial == null) throw new ArgumentNullException(nameof(slideMaterial));
            if (layers == null) throw new ArgumentNullException(nameof(layers));
            if (analyteMaterial == null) throw new ArgumentNullException(nameof(analyteMaterial));

            PrismMaterial = prismMaterial;
            SlideMaterial = slideMaterial;
            AnalyteMaterial = analyteMaterial;
            Wavelength_Meters = wavelength_Meters;
            DefaultThetaIn = thetaInDefault;
            SPRiLayers = layers.ToList();
        }


        #endregion

        #region 内置SPRi模型

        private static void LoadBuiltInSetups()
        {
            var currentGlassSetup = new SPRiSetup(
                Materials["ZF4"], Materials["H-K9L"], [], Materials["PBS"], 660e-9, 70);

            var idealSAM = new OpticalMaterial("SAM", s => 1.465);

            BuiltInSetups.Add("ZF4-K9-Au", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)> 
                    {(Materials["Au"], 47.5e-9)},
                LayerOptimizationRanges = new List<(double, double)> 
                    {(20e-9, 70e-9)}
            });

            BuiltInSetups.Add("ZF4-K9-CrAu", new(currentGlassSetup)
            {
                SPRiLayers =  new List<(OpticalMaterial,double)>
                    {(Materials["Cr"], 5e-9),(Materials["Au"], 47.5e-9)},
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(2e-9, 5e-9),(20e-9, 70e-9)}
            });

            BuiltInSetups.Add("ZF4-K9-TiAu", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)>
                    {(Materials["Ti"], 5e-9),(Materials["Au"], 47.5e-9) },
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(2e-9, 5e-9),(20e-9, 70e-9)}
            });

            BuiltInSetups.Add("ZF4-K9-TiCuAg", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)>
                    {(Materials["Ti"], 5e-9),(Materials["Cu"], 5e-9),(Materials["Ag"], 40e-9) },
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(5e-9, 5e-9),(5e-9, 5e-9),(10e-9, 80e-9)}
            });

            BuiltInSetups.Add("ZF4-K9-TiAuAg", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)>
                    {(Materials["Ti"], 5e-9),(Materials["Au"], 5e-9),(Materials["Ag"], 40e-9) },
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(5e-9, 5e-9),(5e-9, 5e-9),(10e-9, 80e-9)}
            });

            BuiltInSetups.Add("ZF4-K9-CrNiAg", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)>
                    {(Materials["Cr"], 5e-9),(Materials["Ni"], 5e-9),(Materials["Ag"], 40e-9) },
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(5e-9, 5e-9),(5e-9, 5e-9),(10e-9, 80e-9)}
            });


            BuiltInSetups.Add("ZF4-K9-TiCuAgAu", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)>
                    {(Materials["Ti"], 5e-9),(Materials["Cu"], 5e-9),(Materials["Ag"], 40e-9), (Materials["Au"], 5e-9) },
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(5e-9, 5e-9),(5e-9, 5e-9),(10e-9, 80e-9),(5e-9, 5e-9)}
            });

            BuiltInSetups.Add("ZF4-K9-TiAuAgAu", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)>
                    {(Materials["Ti"], 5e-9),(Materials["Au"], 5e-9),(Materials["Ag"], 40e-9), (Materials["Au"], 5e-9) },
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(5e-9, 5e-9),(5e-9, 5e-9),(10e-9, 80e-9),(5e-9, 5e-9)}
            });

            BuiltInSetups.Add("ZF4-K9-CrNiAgAu", new(currentGlassSetup)
            {
                SPRiLayers = new List<(OpticalMaterial, double)>
                    {(Materials["Cr"], 5e-9),(Materials["Ni"], 5e-9),(Materials["Ag"], 40e-9), (Materials["Au"], 5e-9)},
                LayerOptimizationRanges = new List<(double ll, double ul)>
                        {(5e-9, 5e-9),(5e-9, 5e-9),(10e-9, 80e-9),(5e-9, 5e-9)}
            });


            foreach (var setup in BuiltInSetups.ToList())
            {
                BuiltInSetups.Add(setup.Key + "-SAM", new SPRiSetup(setup.Value)
                {
                    SPRiLayers = setup.Value.SPRiLayers
                        .Append((idealSAM,2e-9)).ToList(),
                    LayerOptimizationRanges = setup.Value
                        .LayerOptimizationRanges
                        .Append((2e-9,2e-9)).ToList()
                });
            }

        }

        public static Dictionary<string, SPRiSetup> BuiltInSetups
        {
            get;
            set;
        } = new();

        #endregion
    }
}
