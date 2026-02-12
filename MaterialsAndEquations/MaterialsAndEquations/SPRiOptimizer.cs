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


            if(absoluteSensitivity)
            {
                relChange = dR; // use absolute change in reflectance
            }
            else
            {
                if(reflection0 < 1e-15)
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
        /// <param name="spriAngle"></param>
        /// <param name="prismOutAngle"></param>
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
        public void ComputeSPRiPrismAngles(
            ref double spriAngle,
            ref double prismOutAngle,
            OpticalMaterial slideMaterial,
            OpticalMaterial prismMaterial,
            double wavelength_Meters,
            double prismAngle = 60)
        {
            if (double.IsNaN(spriAngle) == double.IsNaN(prismOutAngle))
                throw new ArgumentException("Dont know which should be computed");

            // helper
            static double Deg2Rad(double d) => d * Math.PI / 180.0;
            static double Rad2Deg(double r) => r * 180.0 / Math.PI;
            static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

            // get real refractive indices (use real part)
            double nSlide = slideMaterial[wavelength_Meters].Real;
            double nPrism = prismMaterial[wavelength_Meters].Real;
            const double nAir = 1.0;

            // If prismAngle is NaN -> branch where spriAngle is known and we compute prismOutAngle
            if (double.IsNaN(prismAngle))
            {
                // spriAngle must be provided (checked earlier)
                if (double.IsNaN(spriAngle))
                {
                    prismOutAngle = double.NaN;
                    return;
                }

                // Snell at slide -> prism (both angles measured from local normal which is vertical here)
                double spriRad = Deg2Rad(spriAngle);
                double sinThetaPrism = (nSlide * Math.Sin(spriRad)) / nPrism;
                if (Math.Abs(sinThetaPrism) > 1.0)
                {
                    // total internal reflection (no transmission into prism)
                    prismOutAngle = double.NaN;
                    return;
                }
                double thetaPrism = Math.Asin(Clamp(sinThetaPrism, -1.0, 1.0)); // radians, measured from vertical (normal)

                // ray direction inside prism (unit), x to right, y up
                double ix = Math.Sin(thetaPrism);
                double iy = Math.Cos(thetaPrism);

                // decide which side (left/right) it will hit based on sign of ix
                int sideSign = ix >= 0 ? 1 : -1; // +1 => right side, -1 => left side
                double beta = Deg2Rad(prismAngle); // side angle w.r.t. horizontal inside triangle

                // side unit vector pointing from base to apex (into triangle)
                double sx = sideSign * Math.Cos(beta);
                double sy = Math.Sin(beta);

                // outward normal for that side (rotate s by -90deg): n_out = (sy, -sx)
                double nx = sy;
                double ny = -sx;

                // incident angle at side: cos(theta1) = -n·i (n points outward; i points toward outside)
                double dot_ni = ix * nx + iy * ny;
                double cosTheta1 = -dot_ni;
                cosTheta1 = Clamp(cosTheta1, -1.0, 1.0);
                double theta1 = Math.Acos(cosTheta1);
                double sinTheta1 = Math.Sin(theta1);

                // Snell at prism -> air
                double sinTheta2 = (nPrism * sinTheta1) / nAir;
                if (Math.Abs(sinTheta2) > 1.0)
                {
                    // TIR at prism side
                    prismOutAngle = double.NaN;
                    return;
                }
                double theta2 = Math.Asin(Clamp(sinTheta2, -1.0, 1.0));

                // tangent unit vector (rotate normal by +90deg)
                double tx = -ny;
                double ty = nx;
                // determine rotation direction sign based on projection of incident on tangent
                double tDot = ix * tx + iy * ty;
                double rotSign = tDot >= 0 ? 1.0 : -1.0;

                // transmitted direction in air: tdir = cos(theta2)*n_out + rotSign*sin(theta2)*t_unit
                double txDir = Math.Cos(theta2) * nx + rotSign * Math.Sin(theta2) * tx;
                double tyDir = Math.Cos(theta2) * ny + rotSign * Math.Sin(theta2) * ty;

                // angle relative to vertical midline:
                // angle = atan2(x, y) (x positive => to right)
                double outAngleRad = Math.Atan2(txDir, tyDir);
                prismOutAngle = Rad2Deg(outAngleRad);
                return;
            }
            else
            {
                // prismAngle known -> compute spriAngle from given prismOutAngle
                if (double.IsNaN(prismOutAngle))
                {
                    spriAngle = double.NaN;
                    return;
                }

                // transmitted ray in air defined by prismOutAngle (deg) relative to vertical
                double outRad = Deg2Rad(prismOutAngle);
                double txDir = Math.Sin(outRad);
                double tyDir = Math.Cos(outRad);

                // choose side by sign of txDir
                int sideSign = txDir >= 0 ? 1 : -1;
                double beta = Deg2Rad(prismAngle);

                // side vector (into triangle)
                double sx = sideSign * Math.Cos(beta);
                double sy = Math.Sin(beta);

                // outward normal for that side
                double nx = sy;
                double ny = -sx;

                // angle between transmitted direction and normal in air: cosTheta2 = dot(tdir, n_out)
                double cosTheta2 = Clamp(txDir * nx + tyDir * ny, -1.0, 1.0);
                double theta2 = Math.Acos(cosTheta2);
                double sinTheta2 = Math.Sin(theta2);

                // Snell at prism->air reversed to get incidence inside prism
                double sinTheta1 = (nAir * sinTheta2) / nPrism;
                if (Math.Abs(sinTheta1) > 1.0)
                {
                    // impossible (would be TIR inside), return NaN
                    spriAngle = double.NaN;
                    return;
                }
                double theta1 = Math.Asin(Clamp(sinTheta1, -1.0, 1.0));
                double cosTheta1 = Math.Cos(theta1);

                // tangent unit vector
                double tx = -ny;
                double ty = nx;
                // determine rotation direction sign: transmitted projection on tangent
                double tDot = txDir * tx + tyDir * ty;
                double rotSign = tDot >= 0 ? 1.0 : -1.0;

                // incident direction inside prism (points from inside toward surface)
                // i = -cos(theta1)*n_out + rotSign*sin(theta1)*t_unit
                double ix = -cosTheta1 * nx + rotSign * Math.Sin(theta1) * tx;
                double iy = -cosTheta1 * ny + rotSign * Math.Sin(theta1) * ty;

                // angle inside prism relative to vertical:
                double thetaPrismRad = Math.Atan2(ix, iy); // same convention: atan2(x,y)
                // Snell at slide->prism: nSlide * sin(spri) = nPrism * sin(thetaPrism)
                double sinSpri = (nPrism * Math.Sin(thetaPrismRad)) / nSlide;
                if (Math.Abs(sinSpri) > 1.0)
                {
                    // no real solution (TIR at slide->prism)
                    spriAngle = double.NaN;
                    return;
                }
                spriAngle = Rad2Deg(Math.Asin(Clamp(sinSpri, -1.0, 1.0)));
                return;
            }
        }
    }
}
