using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KM = MaterialsAndEquations.KnownMaterials.KnownMaterials;
using ScottPlot;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics;
using System.Collections;
using static MaterialsAndEquations.Tests.Tools;
using System.Runtime.InteropServices;

namespace MaterialsAndEquations.Tests
{
    [TestClass]
    public class Sprimodeling
    {
        public static IEnumerable<object[]> SPRiSetupsData
        {
            get
            {
                foreach (var setupkvp in SPRiSetup.BuiltInSetups)
                {
                    yield return new object[] { setupkvp.Key, setupkvp.Value };
                }
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(SPRiSetupsData))]
        public void PlotOptimizedSPRiSetup(string setupName, SPRiSetup setup)
        {
            var opti = setup.GetOptiInstanceByEnum_LayerThickness();
            opti.WriteInfo();

            var xs = new List<double>();
            var reflections = new List<double>();
            var optimRefl = new List<double>();
            var sensis = new List<double>();

            for (var intAngle = 5.0; intAngle < 85.0; intAngle += 0.2)
            {
                setup.DefaultThetaIn = intAngle;
                opti.DefaultThetaIn = setup.DefaultThetaIn = intAngle;

                xs.Add(intAngle);
                reflections.Add(setup.ComputeReflection());
                optimRefl.Add(opti.ComputeReflection());
                sensis.Add(opti.ComputeSensitivity());
            }

            var extMin = setup.GetInternalSPRiAngle(40);
            var extMax = setup.GetInternalSPRiAngle(60);

            var plot = new Plot();
            plot.Add.SignalXY(xs.ToArray(), reflections.ToArray());
            plot.Add.SignalXY(xs.ToArray(), optimRefl.ToArray());

            var sig = plot.Add.SignalXY(xs.ToArray(), sensis.ToArray());
            sig.Axes.YAxis = plot.Axes.Right;

            plot.Add.HorizontalSpan(extMin, extMax);
            plot.Title(setupName);
            SavePlot(plot, setupName);
        }


        [TestMethod]
        public void ClassicalTiAuPenentrationAndSensitivity()
        {
            //基于正无穷厚度计算最优灵敏度
            var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-TiAu"]
                .GetOptiInstanceByEnum_LayerThickness();

            var plot = new Plot();

            var penentrationDepthes = new double[] { 
                50e-9, 100e-9, 200e-9, 500e-9, 1e-6, 2e-6, 5e-6 };

            foreach (var pen in penentrationDepthes) {

                spriSetup.PenetrationDepth = pen;

                var thetaInMin = 45.0;
                var thetaInMax = 85.0;
                var steps = 1001;
                var thetaInEpsilon = (thetaInMax - thetaInMin) / steps;
                var xs = new double[steps];
                var reflections = new double[steps];
                var sensitivitiesAbsolute = new double[steps];
                var sensitivitiesRelative = new double[steps];
                for (int i = 0; i < steps; i++)
                {
                    var thetaIn = thetaInMin + i * thetaInEpsilon;
                    spriSetup.DefaultThetaIn = thetaIn;
                    xs[i] = thetaIn;
                    sensitivitiesAbsolute[i] = spriSetup.ComputeSensitivity(true);
                }
                var sensitivityLine = plot.Add.SignalXY(xs, sensitivitiesAbsolute);
                sensitivityLine.LegendText = $"{pen * 1e+6:F2}um";
            }

            plot.ShowLegend(Alignment.UpperLeft);
            plot.Title("TiAu SPRi Sensitivity at Various Penetration Depths No Optimization");
            SavePlot(plot);
        }

        [TestMethod]
        public void ClassicalTiAuPenentrationAndSensitivityReOpti()
        {
            //基于正无穷厚度计算最优灵敏度
            var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-TiAu"]
                .GetOptiInstanceByEnum_LayerThickness();

            var plot = new Plot();

            var penentrationDepthes = new double[] {
                20e-9, 50e-9, 100e-9, 200e-9, 500e-9, 1e-6};

            foreach (var pen in penentrationDepthes)
            {

                spriSetup.PenetrationDepth = pen;
                var reoptimized = spriSetup.GetOptiInstanceByEnum_LayerThickness();

                var thetaInMin = 45.0;
                var thetaInMax = 85.0;
                var steps = 1001;
                var thetaInEpsilon = (thetaInMax - thetaInMin) / steps;
                var xs = new double[steps];
                var reflections = new double[steps];

                var sensitivitiesAbsolute = new double[steps];
                var sensitivitiesAbsoluteReoptimized = new double[steps];

                for (int i = 0; i < steps; i++)
                {
                    var thetaIn = thetaInMin + i * thetaInEpsilon;
                    spriSetup.DefaultThetaIn = thetaIn;
                    reoptimized.DefaultThetaIn = thetaIn;

                    xs[i] = thetaIn;
                    sensitivitiesAbsolute[i] = spriSetup.ComputeSensitivity(true);
                    sensitivitiesAbsoluteReoptimized[i] = reoptimized.ComputeSensitivity(true);
                }

                var sensitivityLine = plot.Add.SignalXY(xs, sensitivitiesAbsolute);
                var reoptiLine = plot.Add.SignalXY(xs, sensitivitiesAbsoluteReoptimized, sensitivityLine.Color);
                reoptiLine.LinePattern = LinePattern.Dotted;

                sensitivityLine.LegendText = $"{pen * 1e+6:F2}um";
            }

            plot.ShowLegend(Alignment.UpperLeft);
            plot.Title("TiAu SPRi Sensitivity at Various Penetration Depths No Optimization vs Reoptimization For low penentration");
            SavePlot(plot);
        }

        [TestMethod]
        public void PenentrationAndSensitivityFixedAngle()
        {
            var materials = new List<string>() { "ZF4-K9-TiAu", "ZF4-K9-TiCuAg" };

            var plot = new Plot();

            foreach (var matName in materials){
                //基于正无穷厚度计算最优灵敏度
                var spriSetup = SPRiSetup.BuiltInSetups[matName]
                    .GetOptiInstanceByEnum_LayerThickness();

                var penMin = 10e-9;
                var penMax = 1e-6;
                var steps = 1001;
                var penEpsilon = (penMax - penMin) / steps;

                var xs = new double[steps];
                var sensitivitiesAbsolute = new double[steps];
            
                for (int i = 0; i < steps; i++)
                {
                    var pen = penMin + i * penEpsilon;
                    xs[i] = pen;

                    spriSetup.PenetrationDepth = pen;
                    sensitivitiesAbsolute[i] = spriSetup.ComputeSensitivity(true);
                }
                var sensitivityLine = plot.Add.SignalXY(xs, sensitivitiesAbsolute);
                sensitivityLine.LegendText = $"{matName}";
            }

            plot.ShowLegend(Alignment.UpperLeft);
            plot.Title("Penentration Depth vs Sensitivity");
            SavePlot(plot);
        }

        [TestMethod]
        public void OptimizeCrAuSpriAngleIn()
        {
            var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-CrAu"].Clone();

            #region 灵敏度绘图

            var thetaInMin = 5.0;
            var thetaInMax = 85.0;
            var steps = 10001;
            var thetaInEpsilon = (thetaInMax - thetaInMin) / steps;

            var xs = new double[steps];
            var reflections = new double[steps];
            var sensitivitiesAbsolute = new double[steps];
            var sensitivitiesRelative = new double[steps];

            for (int i = 0; i < steps; i++)
            {
                var thetaIn = thetaInMin + i * thetaInEpsilon;
                spriSetup.DefaultThetaIn = thetaIn;
                xs[i] = thetaIn;
                reflections[i] = spriSetup.ComputeReflection();
                sensitivitiesAbsolute[i] = spriSetup.ComputeSensitivity(true);
                sensitivitiesRelative[i] = spriSetup.ComputeSensitivity(false);
            }

            var plot = new Plot();
            var reflectionLine = plot.Add.SignalXY(xs, reflections);

            var sensitivityLine = plot.Add.SignalXY(xs, sensitivitiesAbsolute);
            sensitivityLine.Axes.YAxis = plot.Axes.Right;
            plot.Axes.Right.IsVisible = true;

            var sensitivityLine2 = plot.Add.SignalXY(xs, sensitivitiesRelative);
            var leftAxis = plot.Axes.AddLeftAxis();
            sensitivityLine2.Axes.YAxis = leftAxis;

            #endregion

            spriSetup.DefaultThetaIn = 70.5;
            var optimizedThetaIn =
                spriSetup.OptimizeThetaIn();
            spriSetup.DefaultThetaIn = optimizedThetaIn;
            var sensitivityAtOptimizeAngle =
                spriSetup.ComputeSensitivity(true);

            var pt = plot.Add.ScatterPoints(
                new double[] { optimizedThetaIn }, 
                new double[] { sensitivityAtOptimizeAngle });
            pt.Axes.YAxis = plot.Axes.Right;

            SavePlot(plot);
        }


        [TestMethod]
        public void OptimizeCrAuSpriGoldThickness()
        {
            var plot = new Plot();

            double[] crThicknesses = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0];

            var auThicknessMin = 30.0;
            var auThicknessMax = 60.0;
            var auThicknessSteps = 3001;
            var auThicknessEpsilon = 
                (auThicknessMax - auThicknessMin) / auThicknessSteps;

            double[] auThicknesses = 
                Enumerable.Range(0, auThicknessSteps)
                .Select(i => auThicknessMin + i * auThicknessEpsilon)
                .ToArray();


            foreach (var crThickness in crThicknesses)
            {
                var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-CrAu"].Clone();
                var xs = new double[auThicknessSteps];
                var ys = new double[auThicknessSteps];

                for (int i = 0; i < auThicknessSteps; i++)
                {
                    var auThickness = auThicknesses[i];
                    spriSetup.SPRiLayers[0] = (spriSetup.SPRiLayers[0].material, crThickness * 1e-9);
                    spriSetup.SPRiLayers[1] = (spriSetup.SPRiLayers[1].material, auThickness * 1e-9);
                    xs[i] = auThickness;

                    var optimizedThetaIn = spriSetup.OptimizeThetaIn();
                    spriSetup.DefaultThetaIn = optimizedThetaIn;
                    ys[i] = spriSetup.ComputeSensitivity(true);
                }

                var signalLine = plot.Add.SignalXY(xs, ys);
                signalLine.LegendText = $"Cr Thickness: {crThickness} nm";
            }

            plot.ShowLegend();
            plot.XLabel("GoldThickness");
            SavePlot(plot);
        }

        [TestMethod]
        public void OptimizeTiAuSpriGoldThickness()
        {
            var plot = new Plot();

            double[] tiThicknesses = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0];

            var auThicknessMin = 30.0;
            var auThicknessMax = 60.0;
            var auThicknessSteps = 3001;
            var auThicknessEpsilon =
                (auThicknessMax - auThicknessMin) / auThicknessSteps;

            double[] auThicknesses =
                Enumerable.Range(0, auThicknessSteps)
                .Select(i => auThicknessMin + i * auThicknessEpsilon)
                .ToArray();


            foreach (var crThickness in tiThicknesses)
            {
                var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-TiAu"].Clone();
                var xs = new double[auThicknessSteps];
                var ys = new double[auThicknessSteps];

                for (int i = 0; i < auThicknessSteps; i++)
                {
                    var auThickness = auThicknesses[i];
                    spriSetup.SPRiLayers[0] = (spriSetup.SPRiLayers[0].material, crThickness * 1e-9);
                    spriSetup.SPRiLayers[1] = (spriSetup.SPRiLayers[1].material, auThickness * 1e-9);
                    xs[i] = auThickness;

                    var optimizedThetaIn = spriSetup.OptimizeThetaIn();
                    spriSetup.DefaultThetaIn = optimizedThetaIn;
                    ys[i] = spriSetup.ComputeSensitivity(true);
                }

                var signalLine = plot.Add.SignalXY(xs, ys);
                signalLine.LegendText = $"Ti Thickness: {crThickness} nm";
            }

            plot.ShowLegend();
            plot.XLabel("GoldThickness");
            SavePlot(plot);
        }

        [TestMethod]
        public void OptimizeCrAuSpriWaveLength()
        {
            var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-CrAu"].Clone();
            var xs = new List<double>();
            var absoluteSensitivities = new List<double>();
            var relativeReflection = new List<double>();
            var internalAngle = new List<double>();

            for (int i = 400;i < 1600; i += 1)
            {
                spriSetup.Wavelength_Meters = (i) * 1e-9;
                var optimized = spriSetup.GetOptiInstance_LayerThickness();
                spriSetup = optimized;
                xs.Add(i);
                absoluteSensitivities.Add(optimized.ComputeSensitivity());
                relativeReflection.Add(optimized.ComputeReflection());
                internalAngle.Add(optimized.DefaultThetaIn);
            }

            var p = new Plot();
            p.Add.SignalXY(xs.ToArray(), absoluteSensitivities.ToArray());

            SavePlot(p);
        }

        [TestMethod]
        public void OptimizeCrAuSpriWaveLengthDifferentPenentrations()
        {
            var p = new Plot();
            var penentrations = new List<double>() { 50e-9, 200e-9, 1e-6 };

            foreach(var pen in penentrations)
            {
                var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-CrAu"].Clone();
                spriSetup.PenetrationDepth = pen;
                spriSetup.Wavelength_Meters = 500e-9;
                spriSetup = spriSetup.GetOptiInstanceByEnum_LayerThickness();

                var xs = new List<double>();
                var absoluteSensitivities = new List<double>();
                //var relativeReflection = new List<double>();
                var internalAngle = new List<double>();

                for (int i = 500; i < 1600; i += 1)
                {
                    spriSetup.Wavelength_Meters = (i) * 1e-9;
                    var optimized = spriSetup.GetOptiInstance_LayerThickness();
                    spriSetup = optimized;
                    xs.Add(i);
                    absoluteSensitivities.Add(optimized.ComputeSensitivity());

                    //relativeReflection.Add(optimized.ComputeReflection());
                    //internalAngle.Add(optimized.DefaultThetaIn);
                }

                var sig = p.Add.SignalXY(xs.ToArray(), absoluteSensitivities.ToArray());
                sig.LegendText = $"Pen: {pen * 1e+9:F0} nm";
            }

            SavePlot(p);
        }


        [TestMethod]
        public void OptimizeCrAuSpriSlideRI()
        {
            var slideRIs = new List<double>() { 
                1.50, 
                1.55, 
                1.6, 
                1.65, 
                1.7
            };

            var p = new Plot();

            foreach (var idealSlideRI in slideRIs)
            {
                var spriSetup = SPRiSetup.BuiltInSetups["ZF4-K9-CrAu"].Clone();
                spriSetup.Wavelength_Meters = 500e-9;
                spriSetup.SlideMaterial = new OpticalMaterial("IdealRI",s => idealSlideRI);
                spriSetup = spriSetup.GetOptiInstanceByEnum_LayerThickness();

                var localSensi = spriSetup.ComputeSensitivity();

                var xs = new List<double>();
                var absoluteSensitivities = new List<double>();

                for (int i = 500; i < 2400; i += 1)
                {
                    spriSetup.Wavelength_Meters = (i) * 1e-9;
                    var optimized = spriSetup.GetOptiInstance_LayerThickness();
                    spriSetup = optimized;
                    xs.Add(i);
                    absoluteSensitivities.Add(optimized.ComputeSensitivity());
                }

                var sig = p.Add.SignalXY(xs, absoluteSensitivities);
                sig.LegendText = $"{idealSlideRI}";
            }

            SavePlot(p);
        }


        [TestMethod]
        public void CompareDifferentSPRiSetups()
        {
            var p = new Plot();

            foreach (var setupKVP in SPRiSetup.BuiltInSetups)
            {
                var spriSetup = setupKVP.Value.Clone();
                spriSetup.Wavelength_Meters = 500e-9;

                spriSetup = spriSetup.GetOptiInstanceByEnum_LayerThickness();
                var localSensi = spriSetup.ComputeSensitivity();

                var xs = new List<double>();
                var absoluteSensitivities = new List<double>();

                for (int i = 500; i < 2400; i += 1)
                {
                    spriSetup.Wavelength_Meters = (i) * 1e-9;
                    var optimized = spriSetup.GetOptiInstance_LayerThickness();
                    spriSetup = optimized;
                    xs.Add(i);
                    absoluteSensitivities.Add(optimized.ComputeSensitivity());
                }

                var sig = p.Add.SignalXY(xs, absoluteSensitivities);
                sig.LegendText = $"{setupKVP.Key}";
            }

            SavePlot(p);
        }

    }
}
