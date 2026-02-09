using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialsAndEquations
{
    public class SPRiOptimizer
    {
        public void OptimizeWorkingWavelength(double materialInRI, double materialOutRI)
        {
            //基于已知的入射玻片折射率和出射介质折射率，优化SPRi系统的工作波长以实现最佳性能

            // 显示优化参数：
            // 波长
            //
            // 需要隐式优化的参数包括：
            // 工作角度
            // 每一种镀层的厚度
        }

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
        /// <param name="thetaIn"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public double ComputeSPRiSensitivity(
            OpticalMaterial materialIn,
            IEnumerable<(OpticalMaterial layerMaterial, double thickness_Meters)> thinLayers,
            OpticalMaterial materialOut,
            double wavelength_Meters,
            double thetaIn)
        {

            throw new NotImplementedException();
        }
    }
}
