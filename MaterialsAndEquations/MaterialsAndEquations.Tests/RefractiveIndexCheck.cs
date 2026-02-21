using MathNet.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialsAndEquations.Tests
{
    /// <summary>
    /// 这个类用于验证已知模型计算折射率的正确性
    /// </summary>
    [TestClass]
    public class RefractiveIndexCheck
    {
        [TestMethod]
        public void CheckRefractiveIndexGold()
        {
            var gold = KnownMaterials.KnownMaterials.Materials["Au"];

            double[] wavelengthes = [400,500,600,700,800];
            double[] risReals = [1.409370, 0.786398, 0.361880, 0.256954, 0.229613];
            double[] riImaginaries = [1.745705,1.896413,2.849393,3.717565,4.508930];

            for(int i = 0;i < wavelengthes.Length; i++)
            {
                var ri = gold[wavelengthes[i] * 1e-9];
                Assert.AreEqual(ri.Real, risReals[i], 1e-6);
                Assert.AreEqual(ri.Imaginary, riImaginaries[i], 1e-6);
            }
        }

        [TestMethod]
        public void CheckRefractiveIndexWater()
        {
            //0 1.0745e-05 3.1155e-03 1.6985e-04 1.1795e-02 1.7504e+02
            //0 0.013691 0.069113 0.21523 0.40743 15.1390
            //0 0.0046865 0.059371 0.0040546 0.037650 7.66167

            var waterOssilators = 
                new List<(double Strength, double Omega0, double Gamma)>
                {
                    (1.0745e-05,0.013691,0.0046865),
                    (3.1155e-03,0.069113,0.059371),
                    (1.6985e-04,0.21523 ,0.0040546 ),
                    (1.1795e-02,0.40743 ,0.037650 ),
                    (1.7504e+02,15.1390,7.66167)
                };

            var ldWater = LorentzDrudeMetal.CreateFromEvParameters
                ("H2O",1.0,1.0,0,waterOssilators,0);

            var xs = new List<double>();
            var ns = new List<double>();
            var ks = new List<double>();

            for(int i = 200;i < 6000; i++)
            {
                xs.Add(i);
                var refInd = ldWater[i * 1e-9];
                ns.Add(refInd.Real);
                ks.Add(refInd.Imaginary);
            }

            var p = new Plot();
            p.Add.SignalXY(xs,ns);
            p.Add.SignalXY(xs, ks);

            Tools.SavePlot(p);
        }


        [TestMethod]
        public void CheckRefractiveIndexWaterDebye()
        {
            var ldWater = new DebyeLorentzWater();

            //TODO
            //需要加入对水特定折射率的Assert
        }
    }
}
