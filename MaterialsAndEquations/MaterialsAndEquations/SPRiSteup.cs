using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MaterialsAndEquations.KnownMaterials.KnownMaterials;

namespace MaterialsAndEquations
{
    public class SPRiSteup
    {
        #region 属性

        public OpticalMaterial PrismMaterial { get; set; }
            = new("Dummy Material", s => 1.715);

        public OpticalMaterial SlideMaterial { get; set; }
            = new("Dummy Material", s => 1.521);

        public OpticalMaterial AnalyteMaterial { get; set; } 
            = new("Dummy Material",s => 1.330);

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

        #region 方法

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
                isAbsoluteSensitivity);
        }

        public double OptimizeThetaIn(
            double thetaIn = double.NaN,
            bool isAbsoluteSensitivity = true)
        {
            if(double.IsNaN(thetaIn))
                thetaIn = DefaultThetaIn;

            return SPRiOptimizer.OptimizeSPRiAngleIn(
                SlideMaterial,
                SPRiLayers,
                AnalyteMaterial,
                Wavelength_Meters,
                thetaIn,
                absoluteSensitivity: isAbsoluteSensitivity);
        }

        public double[] OptimizeLayerThickness(
            (double init, double lowerbound, double upperbound)[]?
                optimizationRange_Meters = null,
            bool isAbsoluteSensitivity = true)
        {
            //如果不传入，默认在0-2倍范围内优化
            if(optimizationRange_Meters is null)
            {
                if(LayerOptimizationRanges.Count == SPRiLayers.Count)
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

            if(double.IsNaN(wavelengthInit))
                wavelengthInit = Wavelength_Meters;
            if(double.IsNaN(wavelengthLowerBound))
                wavelengthLowerBound = wavelengthInit - 250e-9;
            if(double.IsNaN(wavelengthUpperBound))
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
                absoluteSensitivity: isAbsoluteSensitivity);
        }


        public SPRiSteup GetOptiInstance_ThetaIn(
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
                absoluteSensitivity: isAbsoluteSensitivity);

            return new SPRiSteup
            {
                PrismMaterial = PrismMaterial,
                SlideMaterial = SlideMaterial,
                AnalyteMaterial = AnalyteMaterial,
                Wavelength_Meters = Wavelength_Meters,
                SPRiLayers = SPRiLayers.ToList(),
                DefaultThetaIn = optimizedThetaIn
            };
        }

        public SPRiSteup GetOptiInstance_LayerThickness(
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

            var toRet = new SPRiSteup
            {
                PrismMaterial = PrismMaterial,
                SlideMaterial = SlideMaterial,
                AnalyteMaterial = AnalyteMaterial,
                Wavelength_Meters = Wavelength_Meters,
                SPRiLayers = updatedLayers,
                DefaultThetaIn = DefaultThetaIn
            };
            return toRet.GetOptiInstance_ThetaIn(isAbsoluteSensitivity: true);

        }

        public SPRiSteup GetOptiInstance_Wavelength(
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

            var toRet = new SPRiSteup
            {
                PrismMaterial = PrismMaterial,
                SlideMaterial = SlideMaterial,
                AnalyteMaterial = AnalyteMaterial,
                Wavelength_Meters = optimizedWavelength,
                SPRiLayers = SPRiLayers.ToList(),
                DefaultThetaIn = DefaultThetaIn
            };
            return toRet.GetOptiInstance_LayerThickness();
        }

        #endregion

        #region 其他函数

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

        #region 构造函数


        #endregion

        #region 内置SPRi模型

        public static SPRiSteup GetClassicalSPRiCrAu()
        {
            return new SPRiSteup
            {
                PrismMaterial = Materials["ZF4"],
                SlideMaterial = Materials["H-K9L"],
                AnalyteMaterial = Materials["H2O"],
                Wavelength_Meters = 660e-9,
                SPRiLayers = new List<(OpticalMaterial material, double thickness_Meters)>
                {
                    (Materials["Cr"], 5e-9),
                    (Materials["Au"], 47.5e-9) // 50 nm gold layer
                },
                DefaultThetaIn = 70.5
            };
        }

        public static SPRiSteup GetClassicalSPRiTiAu()
        {
            return new SPRiSteup
            {
                PrismMaterial = Materials["ZF4"],
                SlideMaterial = Materials["H-K9L"],
                AnalyteMaterial = Materials["H2O"],
                Wavelength_Meters = 660e-9,
                SPRiLayers = new List<(OpticalMaterial material, double thickness_Meters)>
                {
                    (Materials["Ti"], 2e-9),
                    (Materials["Au"], 47.5e-9) // 50 nm gold layer
                },
                DefaultThetaIn = 70.5
            };
        }



        #endregion
    }
}
