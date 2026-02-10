using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MaterialsAndEquations
{
    public static class Equations
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thetaOut"></param>
        /// <param name="reflection"></param>
        /// <param name="transmission"></param>
        /// <param name="wavelength_Meters"></param>
        /// <param name="materialIn"></param>
        /// <param name="thinLayers"></param>
        /// <param name="materialOut"></param>
        /// <param name="thetaIn">角度!!</param>
        /// <param name="polarizationS"></param>
        public static void ComputeReflectionTransmission(
            out double thetaOut,
            out double reflection,
            out double transmission,
            double wavelength_Meters,
            OpticalMaterial materialIn,
            IEnumerable<(OpticalMaterial layerMaterial, double thickness_Meters)> thinLayers,
            OpticalMaterial materialOut,
            double thetaIn,
            bool polarizationS)
        {
            // 基础常数与输入转换
            Complex i = Complex.ImaginaryOne;
            double thetaInRad = thetaIn * Math.PI / 180.0;
            double k0 = 2.0 * Math.PI / wavelength_Meters;

            // 获取入/出射材料折射率
            Complex nIn = materialIn[wavelength_Meters];
            Complex nOut = materialOut[wavelength_Meters];

            // 斯涅尔定律不变量 alpha = n * sin(theta)
            Complex alpha = nIn * Math.Sin(thetaInRad);

            // 计算出射角度 (仅返回实部，单位：度)
            Complex sinThetaOut = alpha / nOut;
            thetaOut = Complex.Asin(sinThetaOut).Real * 180.0 / Math.PI;

            // 辅助函数：计算 kz 和 导纳 eta
            (Complex kz, Complex eta) GetParams(Complex n)
            {
                // kz = k0 * sqrt(n^2 - alpha^2)
                Complex term = Complex.Sqrt(n * n - alpha * alpha);
                Complex kzVal = k0 * term;

                Complex etaVal;
                if (polarizationS) // TE (s-polarization)
                {
                    etaVal = kzVal; // 忽略常数因子，因为在比率中消掉
                }
                else // TM (p-polarization)
                {
                    // eta ~ n^2 / kz
                    etaVal = (n * n) / kzVal;
                }
                return (kzVal, etaVal);
            }

            var (kzIn, etaIn) = GetParams(nIn);
            var (kzOut, etaOut) = GetParams(nOut);

            // 传输矩阵法 (Transfer Matrix Method)
            // 初始矩阵为单位阵
            Complex m11 = 1.0;
            Complex m12 = 0.0;
            Complex m21 = 0.0;
            Complex m22 = 1.0;

            if (thinLayers != null)
            {
                foreach (var layer in thinLayers)
                {
                    var nLayer = layer.layerMaterial[wavelength_Meters];
                    var d = layer.thickness_Meters;
                    var (kzLayer, etaLayer) = GetParams(nLayer);

                    // 相位厚度 delta
                    Complex delta = kzLayer * d;
                    Complex sinD = Complex.Sin(delta);
                    Complex cosD = Complex.Cos(delta);

                    // 层特征矩阵元素
                    // M = [ cos(delta)      -i/eta * sin(delta) ]
                    //     [ -i*eta*sin(delta)  cos(delta)       ]
                    Complex a = cosD;
                    // 防止分母为0 (极少数情况)
                    Complex b = Complex.Abs(etaLayer) > 1e-15 
                        ? -(i / etaLayer) * sinD 
                        : 0; 
                    Complex c = -(i * etaLayer) * sinD;
                    Complex dVal = cosD;

                    // 矩阵乘法 M_total = M_total * M_layer
                    Complex t11 = m11 * a + m12 * c;
                    Complex t12 = m11 * b + m12 * dVal;
                    Complex t21 = m21 * a + m22 * c;
                    Complex t22 = m21 * b + m22 * dVal;

                    m11 = t11; m12 = t12;
                    m21 = t21; m22 = t22;
                }
            }

            // 计算反射系数 r 和透射系数 t
            // r = (etaIn*m11 + etaIn*etaOut*m12 - m21 - etaOut*m22) / D
            // t = 2*etaIn / D
            // D = etaIn*m11 + etaIn*etaOut*m12 + m21 + etaOut*m22

            Complex D = etaIn * m11 + etaIn * etaOut * m12 + m21 + etaOut * m22;
            
            // 防止除以零
            if (Complex.Abs(D) < 1e-15)
            {
                reflection = 0;
                transmission = 0;
                return;
            }

            Complex rNumerator = etaIn * m11 + etaIn * etaOut * m12 - m21 - etaOut * m22;
            
            Complex rVal = rNumerator / D;
            Complex tVal = 2.0 * etaIn / D;

            // 计算能量反射率 R 和 透射率 T
            reflection = Complex.Abs(rVal) * Complex.Abs(rVal); // |r|^2

            // T = Re(etaOut) / Re(etaIn) * |t|^2
            double reEtaIn = etaIn.Real;
            double reEtaOut = etaOut.Real;

            if (reEtaIn > 1e-15)
            {
                transmission = (reEtaOut / reEtaIn) * Complex.Abs(tVal) * Complex.Abs(tVal);
            }
            else
            {
                // 全反射或其他情况，入射导纳实部为0，无法定义常规透射率
                transmission = 0;
            }
        }
    }
}
