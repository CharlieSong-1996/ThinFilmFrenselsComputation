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


        //Matlab code for reference:
        //lambda=660e-9;
        //n0=1.721;
        //n1=2.25432+3.63712i;
        //n2=0.20026+3.62238i;
        //n3=0.065+3.7793i;
        //n4=n2;
        //n5=1.333;
        //d1=5e-9;d2=2e-9;d3=42e-9;d4=13e-9;theta=linspace(51.35,57.6,10000);
        //        k0=2*pi* n0/lambda* cos(theta* pi/180);
        //        k1=2*pi* sqrt((n1/lambda )^2-(n0/lambda* sin(theta* pi/180 ) ).^2 );
        //k2=2*pi* sqrt((n2/lambda )^2-(n0/lambda* sin(theta* pi/180 ) ).^2 );
        //k3=2*pi* sqrt((n3/lambda )^2-(n0/lambda* sin(theta* pi/180 ) ).^2 );
        //k4=2*pi* sqrt((n4/lambda )^2-(n0/lambda* sin(theta* pi/180 ) ).^2 );
        //k5=2*pi* sqrt((n5/lambda )^2-(n0/lambda* sin(theta* pi/180 ) ).^2 );
        //p=2;
        //s=0;
        //polar=p;
        //rou01=(n1^polar* k0-n0^polar* k1)./(n1^polar* k0+n0^polar* k1);
        //        rou12=(n2^polar* k1-n1^polar* k2)./(n2^polar* k1+n1^polar* k2);
        //        rou23=(n3^polar* k2-n2^polar* k3)./(n3^polar* k2+n2^polar* k3);
        //        rou34=(n4^polar* k3-n3^polar* k4)./(n4^polar* k3+n3^polar* k4);
        //        rou45=(n5^polar* k4-n4^polar* k5)./(n5^polar* k4+n4^polar* k5);
        //        rou345=(rou34+rou45.* exp(2*i* k4*d4))./(1+rou34.* rou45.*exp(2*i* k4*d4));
        //rou2345=(rou23+rou345.* exp(2*i* k3*d3))./(1+rou23.* rou345.*exp(2*i* k3*d3));
        //rou12345=(rou12+rou2345.* exp(2*i* k2*d2))./(1+rou12.* rou2345.*exp(2*i* k2*d2));
        //rou012345=(rou01+rou12345.* exp(2*i* k1*d1))./(1+rou01.* rou12345.*exp(2*i* k1*d1))
        //R=abs(rou012345).^2;R=R/max(R);

    }
}
