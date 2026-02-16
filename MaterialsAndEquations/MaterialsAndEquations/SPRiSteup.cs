using System;
using System.Collections.Generic;
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
        
        public OpticalMaterial SlideMaterial { get; set; }

        public OpticalMaterial AnalyteMaterial { get; set; }

        public IList<(OpticalMaterial material, double thickness_Meters)> SPRiLayers
        {
            get;
            private init;
        }

        public double Wavelength_Meters { get; set; }

        public double DefaultThetaIn { get; set; }

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
            double thickness_Meters = double.NaN,
            bool isAbsoluteSensitivity = true)
        {
            //如果不传入，默认在0-2倍范围内优化
            if(optimizationRange_Meters is null)
                optimizationRange_Meters = SPRiLayers.Select(s =>
                    (s.thickness_Meters, 0.0, s.thickness_Meters * 2)).ToArray();

            return SPRiOptimizer.OptimizeSPRiLayerThicknessesAndAngleIn(
                SlideMaterial,
                SPRiLayers.Select(s => s.material),
                optimizationRange_Meters,
                AnalyteMaterial,
                Wavelength_Meters,
                DefaultThetaIn,
                absoluteSensitivity: isAbsoluteSensitivity).ToArray();
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
