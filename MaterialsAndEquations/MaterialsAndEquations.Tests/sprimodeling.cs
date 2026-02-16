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

namespace MaterialsAndEquations.Tests
{
    [TestClass]
    public class Sprimodeling
    {
        // Returns a directory path where test plots can be stored. Creates the directory if necessary.
        public string GetPlotsDirectory(string folderName = "TestPlots")
        {
            if(!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
            return folderName;
        }

        // Kept for compatibility; saving is handled by user code. Use GetPlotsDirectory to obtain where to write files.
        public void SavePlot(Plot p,[CallerMemberName]string testName = "") 
        {
            var saveDir = Path.Combine(GetPlotsDirectory(), $"{testName}.png");
            Console.WriteLine(saveDir);
            p.SavePng(saveDir, 600, 400);
        }

        [TestMethod]
        public void ClassicalCrAuSPRi()
        {
            var spriSetup = SPRiSteup.GetClassicalSPRiCrAu();

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
            var spriSetup = SPRiSteup.GetClassicalSPRiCrAu();

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
                var spriSetup = SPRiSteup.GetClassicalSPRiCrAu();
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
                var spriSetup = SPRiSteup.GetClassicalSPRiTiAu();
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
        public void OptimizeCrAuSpriLayerThickness()
        {
            // 获取必要的材料
            var chromium = KM.Materials["Cr"];
            var gold = KM.Materials["Au"];
            var water = KM.Materials["H2O"];

            // 创建理想化的材料：折射率为1.521，无色散
            var idealSlideMaterial = new OpticalMaterial(
                "IdealSlide_n1.521",
                wavelength => new System.Numerics.Complex(1.521, 0.0));

            // 定义波长：660nm
            double wavelength = 660e-9;

            // 定义要优化的膜层：Cr 和 Au
            var layersToOptimize = new[] {
                chromium,
                gold
            };

            // 定义各层的优化范围
            var optimizations = new[] {
                (initialGuess: 2.0, lowerBound: 2.0, upperBound: 2.0),    // Cr: 0-20nm，初始值5nm
                (initialGuess: 47.5, lowerBound: 0.0, upperBound: 100.0)   // Au: 0-100nm，初始值47.5nm
            };

            // 使用 OptimizeSPRiLayerThicknessesAndAngleIn 函数优化膜层厚度和入射角
            Vector<double> result = SPRiOptimizer.OptimizeSPRiLayerThicknessesAndAngleIn(
                idealSlideMaterial,
                layersToOptimize,
                optimizations,
                water,
                wavelength,
                70);

            // 提取优化结果
            double optimalCrThickness = result[0] * 1e-9;      // 转换为米
            double optimalAuThickness = result[1] * 1e-9;      // 转换为米
            double optimalAngle = result[2];                   // 角度

            // 计算最优参数下的灵敏度
            var optimalLayers = new[] {
                (chromium, optimalCrThickness),
                (gold, optimalAuThickness)
            };

            double optimalSensitivity = SPRiOptimizer.ComputeSPRiSensitivity(
                idealSlideMaterial,
                optimalLayers,
                water,
                wavelength,
                optimalAngle,
                absoluteSensitivity: true);

            // 输出结果
            Console.WriteLine("=== OptimizeCrAuSpriLayerThickness 测试结果 ===");
            Console.WriteLine($"优化后的参数：");
            Console.WriteLine($"  Cr厚度：{result[0]:F2} nm");
            Console.WriteLine($"  Au厚度：{result[1]:F2} nm");
            Console.WriteLine($"  最优入射角度：{optimalAngle:F2}°");
            Console.WriteLine($"  最优灵敏度：{optimalSensitivity:F6}");
            Console.WriteLine();

            // 绘制灵敏度曲线以展示优化结果
            var angleMin = 30.0;
            var angleMax = 85.0;
            var angleSteps = 1000;
            var angleEpsilon = (angleMax - angleMin) / angleSteps;
            var angles = Enumerable.Range(0, angleSteps)
                .Select(i => angleMin + i * angleEpsilon)
                .ToArray();

            // 计算经典参数（5nm Cr + 47.5nm Au）下的灵敏度
            var classicalLayers = new[] {
                (chromium, 5e-9),
                (gold, 47.5e-9)
            };

            var classicalSensitivities = angles.Select(angle => {
                try
                {
                    return SPRiOptimizer.ComputeSPRiSensitivity(
                        idealSlideMaterial,
                        classicalLayers,
                        water,
                        wavelength,
                        angle,
                        absoluteSensitivity: true);
                }
                catch
                {
                    return double.NaN;
                }
            }).ToArray();

            // 计算优化后参数下的灵敏度
            var optimalSensitivities = angles.Select(angle => {
                try
                {
                    return SPRiOptimizer.ComputeSPRiSensitivity(
                        idealSlideMaterial,
                        optimalLayers,
                        water,
                        wavelength,
                        angle,
                        absoluteSensitivity: true);
                }
                catch
                {
                    return double.NaN;
                }
            }).ToArray();

            // 绘制对比曲线
            var plot = new Plot();
            var sig1 = plot.Add.SignalXY(angles, classicalSensitivities);
            sig1.LegendText = "Classical";
            var sig2 = plot.Add.SignalXY(angles, optimalSensitivities);
            sig2.LegendText = "Optimized";
            var scatter = plot.Add.Scatter(new double[] { optimalAngle }, new double[] { optimalSensitivity });
            scatter.LegendText = "Optimal Point";
            scatter.MarkerSize = 10;
            scatter.Color = ScottPlot.Color.FromHex("#FF0000");
            plot.Legend.IsVisible = true;
            plot.Title("SPRi Sensitivity Comparison: Classical vs Optimized");
            plot.XLabel("Incident Angle (degrees)");
            plot.YLabel("Absolute Sensitivity");

            SavePlot(plot);
        }


        [TestMethod]
        public void OptimizeCrAuSpri()
        {
            //这个测试用于寻找最佳的SPRi参数组合
            //需要优化的组合参数包括：
            //1. 入射角度(spriInternalAngle)
            //2. 最优的Cr镀层厚度(单位nm)
            //3. 最优的Au镀层厚度(单位nm)
            //4. 最优的工作波长(单位nm)

            Vector<double> initialGuess = Vector<double>.Build.DenseOfArray(new double[] {
                70,   // 初始入射角度
                5.0,    // 初始Cr厚度
                47.5,   // 初始Au厚度
                660.0   // 初始波长
            });
            Vector<double> lowerBounds = Vector<double>.Build.DenseOfArray(new double[] {
                30,   // 最小入射角度
                0.0,    // 最小Cr厚度
                0.0,    // 最小Au厚度
                400.0   // 最小波长
            });
            Vector<double> upperBounds = Vector<double>.Build.DenseOfArray(new double[] {
                85,   // 最大入射角度
                20.0,   // 最大Cr厚度
                100.0,  // 最大Au厚度
                1000.0  // 最大波长
            });


            // 获取必要的相关参数，材料信息等
            var chromium = KM.Materials["Cr"];
            var gold = KM.Materials["Au"];
            var water = KM.Materials["H2O"];

            // 创建理想化的材料：折射率为1.521，无色散（即在所有波长下折射率相同）
            var idealSlideMaterial = new OpticalMaterial(
                "IdealSlide_n1.521",
                wavelength => new System.Numerics.Complex(1.521, 0.0));

            double GetNegativeAbsoluteSensitivity(Vector<double> parameters)
            {
                // 从参数向量中提取各个参数
                double spriInternalAngle = parameters[0];  // 角度
                double crThickness = parameters[1] * 1e-9;  // 转换为米
                double auThickness = parameters[2] * 1e-9;  // 转换为米
                double wavelength = parameters[3] * 1e-9;   // 转换为米

                try
                {
                    // 构建薄膜堆叠：Cr + Au
                    var thinLayers = new[] {
                        (chromium, crThickness),
                        (gold, auThickness)
                    };

                    // 计算SPRi灵敏度
                    double sensitivity = SPRiOptimizer.ComputeSPRiSensitivity(
                        idealSlideMaterial,// 使用理想化的slide材料
                        thinLayers,
                        water,  
                        wavelength,
                        spriInternalAngle,
                        absoluteSensitivity: true);

                    // 如果灵敏度计算失败（返回NaN），返回一个大的正值
                    if (double.IsNaN(sensitivity) || double.IsInfinity(sensitivity))
                        return 1e5;

                    // 返回负灵敏度（因为要找最小值，优化器会最小化这个值）
                    // 如果灵敏度为负数，直接返回；否则返回一个惩罚值
                    if (sensitivity >= 0)
                        return -sensitivity;
                    else
                        return 1e5;  // 负数灵敏度不合理，返回惩罚
                }
                catch
                {
                    return 1e5;
                }
            }

            // 使用约束优化找到最优参数
            Vector<double> resultPoint;
            try
            {
                resultPoint = FindMinimum.OfFunctionConstrained(
                    GetNegativeAbsoluteSensitivity,
                    lowerBounds,
                    upperBounds,
                    initialGuess,
                    gradientTolerance: 0.1,
                    parameterTolerance: 0.01,
                    functionProgressTolerance: 0.01,
                    maxIterations: 100);
            }
            catch (MathNet.Numerics.Optimization.MaximumIterationsException)
            {
                // 如果优化器达到最大迭代次数，使用初始猜测作为结果
                Console.WriteLine("优化器达到最大迭代次数，使用当前最佳点作为结果。");
                resultPoint = initialGuess;
            }

            // 输出结果
            Console.WriteLine("=== SPRi 参数优化结果 ===");
            Console.WriteLine($"优化后的参数：");
            Console.WriteLine($"  入射角度：{resultPoint[0]:F2}°");
            Console.WriteLine($"  Cr厚度：{resultPoint[1]:F2} nm");
            Console.WriteLine($"  Au厚度：{resultPoint[2]:F2} nm");
            Console.WriteLine($"  工作波长：{resultPoint[3]:F2} nm");
            double negSensitivity = GetNegativeAbsoluteSensitivity(resultPoint);
            Console.WriteLine($"  最大灵敏度：{-negSensitivity:F6}");
            Console.WriteLine();

            // 根据最优的参数，绘制SPRi灵敏度曲线图，并与经典SPRi参数对比

            // 经典SPRi参数（参考第一个测试）
            var classicalSlide = KM.Materials["H-K9L"];
            var classicalCrThickness = 5e-9;
            var classicalAuThickness = 47.5e-9;
            var classicalWavelength = 660e-9;

            // 优化后的参数
            var optimalAngle = resultPoint[0];
            var optimalCrThickness = resultPoint[1] * 1e-9;
            var optimalAuThickness = resultPoint[2] * 1e-9;
            var optimalWavelength = resultPoint[3] * 1e-9;

            // 生成入射角度范围
            var angleMin = 30.0;
            var angleMax = 85.0;
            var angleSteps = 1000;
            var angleEpsilon = (angleMax - angleMin) / angleSteps;
            var angles = Enumerable.Range(0, angleSteps)
                .Select(i => angleMin + i * angleEpsilon)
                .ToArray();

            // 计算经典SPRi参数下的灵敏度
            var classicalLayers = new[] {
                (chromium, classicalCrThickness),
                (gold, classicalAuThickness)
            };

            var classicalSensitivities = angles.Select(angle => {
                try
                {
                    return SPRiOptimizer.ComputeSPRiSensitivity(
                        water,
                        classicalLayers,
                        idealSlideMaterial,
                        classicalWavelength,
                        angle,
                        absoluteSensitivity: true);
                }
                catch
                {
                    return double.NaN;
                }
            }).ToArray();

            // 计算优化后参数下的灵敏度
            var optimalLayers = new[] {
                (chromium, optimalCrThickness),
                (gold, optimalAuThickness)
            };

            var optimalSensitivities = angles.Select(angle => {
                try
                {
                    return SPRiOptimizer.ComputeSPRiSensitivity(
                        idealSlideMaterial,
                        optimalLayers,
                        water,
                        optimalWavelength,
                        angle,
                        absoluteSensitivity: true);
                }
                catch
                {
                    return double.NaN;
                }
            }).ToArray();

            // 计算最优角度处的灵敏度
            double optimalSensitivity = SPRiOptimizer.ComputeSPRiSensitivity(
                idealSlideMaterial,
                optimalLayers,
                water,
                optimalWavelength,
                optimalAngle,
                absoluteSensitivity: true);

            // 绘制对比曲线
            var plot = new Plot();
            var sig1 = plot.Add.SignalXY(angles, classicalSensitivities);
            sig1.LegendText = "Classical";
            var sig2 = plot.Add.SignalXY(angles, optimalSensitivities);
            sig2.LegendText = "Optimized";
            var scatter = plot.Add.Scatter(new double[] { optimalAngle }, new double[] { optimalSensitivity });
            scatter.LegendText = "Optimal Point";
            scatter.MarkerSize = 10;
            scatter.Color = ScottPlot.Color.FromHex("#FF0000");
            plot.Legend.IsVisible = true;
            plot.Title("SPRi Sensitivity Comparison: Classical vs Optimized");
            plot.XLabel("Incident Angle (degrees)");
            plot.YLabel("Absolute Sensitivity");

            SavePlot(plot);
        }
    }
}
