using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MaterialsAndEquations
{
    public static class Equations
    {
        /// <summary>
        /// 计算入射光线与出射光线的夹角，以及反射率和透射率。
        /// </summary>
        /// <param name="nIn">入射材料折射率</param>
        /// <param name="nOut">出射材料折射率</param>
        /// <param name="thetaIn">弧度制</param>
        /// <param name="polarizationS">偏振类型</param>
        /// <param name="thetaOut">弧度制</param>
        /// <param name="reflection">反射率，1.0代表全反射，0.0代表全透射</param>
        /// <param name="transmission">透射率，0.0代表全反射，1.0代表全透射</param>
        /// <exception cref="NotImplementedException"></exception>
        public static void ComputeReflectionTransmission(
            out double thetaOut,
            out double reflection,
            out double transmission,
            Complex nIn, 
            Complex nOut, 
            double thetaIn, 
            bool polarizationS)
        {
            throw new NotImplementedException();
        }





    }
}
