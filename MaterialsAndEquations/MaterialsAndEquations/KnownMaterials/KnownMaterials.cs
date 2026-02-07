using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialsAndEquations.KnownMaterials
{
    public static class KnownMaterials
    {
        public static Dictionary<string, OpticalMaterial> Materials { get; } = new Dictionary<string, OpticalMaterial>(StringComparer.OrdinalIgnoreCase)
        {

            //K9 热膨胀系数 7.6e-6 m/K
            ["K9"] = new SellmeierMaterial("K9",
                [(6.14555251E-01, 1.45987884E-02), (6.56775017E-01, 2.87769588E-03), (1.02699346E+00, 1.07653051E+02)]),

            ["H-K9L"] = new SellmeierMaterial("H-K9L",
                [(6.14555251E-01, 1.45987884E-02),(6.56775017E-01, 2.87769588E-03),(1.02699346E+00, 1.07653051E+02)]),


            //热膨胀系数 9.6E-6 m/K
            ["ZF4"] = new SchottMaterial("ZF4", 2.8923490E+00, -1.7933942E-02, 2.3009472E-02, 5.6390617E-03, -6.9815495E-04, 5.2528998E-05),
        };
    }
}