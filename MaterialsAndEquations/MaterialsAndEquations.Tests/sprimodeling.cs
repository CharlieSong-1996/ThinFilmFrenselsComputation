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

namespace MaterialsAndEquations.Tests
{
    [TestClass]
    public class Sprimodeling
    {
        [TestMethod]
        public void ClassicalCrAuSPRi()
        {
            var spriSetup = SPRiSetup.GetClassicalSPRiCrAu();

            var thetaInMin = 5.0;
            var thetaInMax = 85.0;
            var steps = 10001;
            var thetaInEpsilon = (thetaInMax - thetaInMin) / steps;

            var xs = new double[steps];
            var reflections = new double[steps];
            var sensitivitiesAbsolute = new double[steps];
            var sensitivitiesRelative = new double[steps];

            for (int i = 0; i < steps;i++)
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

            SavePlot(plot);
        }

        [TestMethod]
        public void OptimizeCrAuSpriAngleIn()
        {
            var spriSetup = SPRiSetup.GetClassicalSPRiCrAu();

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
                var spriSetup = SPRiSetup.GetClassicalSPRiCrAu();
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
                var spriSetup = SPRiSetup.GetClassicalSPRiTiAu();
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
            var spriSetup = SPRiSetup.GetClassicalSPRiCrAu();
            var xs = new List<double>();
            var absoluteSensitivities = new List<double>();
            var relativeReflection = new List<double>();
            var internalAngle = new List<double>();

            for (int i = 500;i < 2400; i += 1)
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

            var sigRef = p.Add.SignalXY(xs.ToArray(), internalAngle);
            sigRef.Axes.YAxis = p.Axes.Right;

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
                var spriSetup = SPRiSetup.GetClassicalSPRiCrAu();
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

    }
}
