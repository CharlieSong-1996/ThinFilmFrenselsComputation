using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MaterialsAndEquations
{
    public class SPRiOptimizer
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
        public double ComputeSPRiSensitivity(
            OpticalMaterial materialIn,
            IEnumerable<(OpticalMaterial layerMaterial, double thickness_Meters)> thinLayers,
            OpticalMaterial materialOut,
            double wavelength_Meters,
            double thetaIn)
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
            if (Math.Abs(reflection0) > 1e-15)
            {
                relChange = dR / reflection0;
            }
            else
            {
                relChange = dR; // avoid division by zero
            }

            // change in refractive index (real part)
            Complex n0 = materialOut[wavelength_Meters];
            Complex n1 = material_dRI[wavelength_Meters];
            double dnOut = (n1 - n0).Real;

            if (Math.Abs(dnOut) < 1e-20)
            {
                // can't compute sensitivity for zero index change
                return double.NaN;
            }

            double sensitivity = relChange / dnOut;
            return sensitivity;
        }
    }
}
