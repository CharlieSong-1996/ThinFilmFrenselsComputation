using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using MaterialsAndEquations;

namespace MaterialsAndEquations.KnownMaterials
{
    public static class KnownMaterials
    {
        public static Dictionary<string, OpticalMaterial> Materials { get; } = new Dictionary<string, OpticalMaterial>(StringComparer.OrdinalIgnoreCase)
        {
        };


        static KnownMaterials()
        {
            LoadCDGMData(); // 读取CDGM玻璃数据库
            LoadLDData(); // 读取 Lorentz-Drude（金属）数据库

            Materials.Add("H2O", new SellmeierMaterial("H2O", [(0.75831,0.01007),(0.08495,8.91377)]));
        }

        private static void LoadCDGMData()
        {
            // 读取项目中的 CDGM.csv（尝试多个候选路径），自动解析每行：
            // - 若存在 K/L 对 => 生成 SellmeierMaterial
            // - 否则若存在 A0..A5 => 生成 SchottMaterial
            // 将生成的材料按名称添加到 Materials 字典（不覆盖已存在项）


            using var stream = new StreamReader(
                Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MaterialsAndEquations.KnownMaterials.CDGM.csv")!, encoding: Encoding.UTF8);

            try
            {
                var lines = stream.ReadToEnd().Split('\n');
                foreach (var rawLine in lines)
                {
                    if (string.IsNullOrWhiteSpace(rawLine)) continue;

                    // 忽略 header 行（以 Glass 开头）
                    if (rawLine.TrimStart().StartsWith("Glass", StringComparison.OrdinalIgnoreCase)) continue;

                    var cols = ParseCsvLine(rawLine);
                    if (cols.Length == 0) continue;

                    var name = cols[0].Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    if (Materials.ContainsKey(name)) continue; // 不覆盖已有项

                    // CSV 列约定（见文件示例）:
                    // 0: Glass (name)
                    // 1: Code
                    // 2: K1, 3: L1, 4: K2, 5: L2, 6: K3, 7: L3
                    // 8..13: A0..A5 (Schott)
                    // 其后还有若干列，但不是我们需要的

                    // 尝试解析 Sellmeier 项 (K/L 对)
                    var sellmeierTerms = new List<(double K, double L)>();
                    for (int i = 0; i < 3; i++)
                    {
                        int kIdx = 2 + i * 2;
                        int lIdx = 3 + i * 2;
                        if (kIdx < cols.Length && lIdx < cols.Length)
                        {
                            if (TryParseDouble(cols[kIdx], out double k) && TryParseDouble(cols[lIdx], out double l))
                            {
                                // 若解析成功且值有效，则认为是一个有效项
                                sellmeierTerms.Add((k, l));
                            }
                        }
                    }

                    if (sellmeierTerms.Count > 0)
                    {
                        try
                        {
                            Materials[name] = new SellmeierMaterial(name, sellmeierTerms);
                            continue;
                        }
                        catch
                        {
                            // 若构造失败，退回尝试 Schott（或跳过）
                        }
                    }

                    // 否则尝试解析 Schott 系数 A0..A5
                    var coeffs = new List<double>();
                    for (int ai = 0; ai < 6; ai++)
                    {
                        int idx = 8 + ai;
                        if (idx < cols.Length && TryParseDouble(cols[idx], out double a))
                        {
                            coeffs.Add(a);
                        }
                        else
                        {
                            // 如果某个系数缺失，用 0 代替以保持长度（Schott 构造会补齐到 6）
                            coeffs.Add(0.0);
                        }
                    }

                    // 如果至少有一个非零系数或列中确实存在 A 列，则创建 SchottMaterial
                    if (coeffs.Any(c => c != 0.0) || cols.Length >= 9)
                    {
                        try
                        {
                            Materials[name] = new SchottMaterial(name, coeffs);
                        }
                        catch
                        {
                            // 忽略创建失败的行
                        }
                    }
                }
            }
            catch
            {
                // 读取过程中出现异常则静默忽略，不阻塞静态初始化
            }

            // 局部辅助函数：尽量稳健解析 double（支持科学计数法），使用 InvariantCulture
            static bool TryParseDouble(string s, out double value)
            {
                value = double.NaN;
                if (string.IsNullOrWhiteSpace(s)) return false;
                s = s.Trim();
                return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
            }

            // 局部 CSV 解析器：支持简单的带引号字段（不做复杂转义处理）
            static string[] ParseCsvLine(string line)
            {
                if (string.IsNullOrEmpty(line)) return Array.Empty<string>();
                var parts = new List<string>();
                var sb = new StringBuilder();
                bool inQuotes = false;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == '"')
                    {
                        // 如果双引号后面仍为双引号，认为是转义的双引号
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++; // skip escaped quote
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        parts.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                parts.Add(sb.ToString());
                return parts.ToArray();
            }


        }

        private static void LoadLDData()
        {
            // 读取谁要使用LD模型计算折射率的材料数据

            // 参考数据已经嵌入资源，读取方法测试正确不要修改。
            using var stream = new StreamReader(
                Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MaterialsAndEquations.KnownMaterials.LD.csv")!, encoding: Encoding.UTF8);

            //Example

            //name,omega_p,f_0,Gamma_0,f_1,Gamma_1,omega_1,f_2,Gamma_2,omega_2,f_3,Gamma_3,omega_3,f_4,Gamma_4,omega_4,f_5,Gamma_5,omega_5,
            //Au,9.03,0.760,0.053,0.024,0.241,0.415,0.010,0.345,0.830,0.071,0.870,2.969,0.601,2.494,4.304,4.384,2.214,13.32


            try
            {
                var lines = stream.ReadToEnd().Split('\n');

                foreach (var rawLine in lines)
                {
                    if (string.IsNullOrWhiteSpace(rawLine)) continue;

                    var cols = ParseCsvLine(rawLine);
                    if (cols.Length == 0) continue;

                    // skip header if present
                    if (cols[0].Trim().Equals("name", StringComparison.OrdinalIgnoreCase)) continue;

                    var name = cols[0].Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (Materials.ContainsKey(name)) continue; // don't overwrite

                    // expect at least: name, omega_p, f_0, Gamma_0
                    if (cols.Length < 4) continue;

                    if (!TryParseDouble(cols[1], out double plasmaEv)) continue; // plasma energy in eV
                    // f0 is at cols[2]
                    double f0 = 1.0;
                    if (2 < cols.Length && TryParseDouble(cols[2], out double parsedF0))
                    {
                        f0 = parsedF0;
                    }

                    // Drude gamma is Gamma_0 at cols[3]
                    if (!TryParseDouble(cols[3], out double drudeGammaEv))
                    {
                        // if missing, default to small value
                        drudeGammaEv = 0.0;
                    }

                    // build oscillators list from columns. CSV layout (after index 3) is groups of (f_j, Gamma_j, omega_j)
                    var oscillators = new List<(double Strength, double ResonanceEnergyEv, double GammaEv)>();
                    int startIdx = 4; // first f_1
                    int maxOsc = (cols.Length - startIdx) / 3;
                    for (int j = 0; j < maxOsc; j++)
                    {
                        int fIdx = startIdx + j * 3;
                        int gammaIdx = fIdx + 1;
                        int omegaIdx = fIdx + 2;

                        if (fIdx >= cols.Length) break;

                        if (TryParseDouble(cols[fIdx], out double f) &&
                            gammaIdx < cols.Length && TryParseDouble(cols[gammaIdx], out double g) &&
                            omegaIdx < cols.Length && TryParseDouble(cols[omegaIdx], out double w))
                        {
                            // add oscillator only if values parsed
                            oscillators.Add((f, w, g));
                        }
                    }

                    try
                    {
                        // epsInf not provided in CSV; use 1.0 as reasonable default for metals
                        var mat = LorentzDrudeMetal.CreateFromEvParameters(name, 1.0, plasmaEv, drudeGammaEv, oscillators, drudeStrength: f0);
                        Materials[name] = mat;
                    }
                    catch
                    {
                        // ignore failures for individual lines
                    }
                }
            }
            catch
            {

            }

            static bool TryParseDouble(string s, out double value)
            {
                value = double.NaN;
                if (string.IsNullOrWhiteSpace(s)) return false;
                s = s.Trim();
                return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
            }

            static string[] ParseCsvLine(string line)
            {
                if (string.IsNullOrEmpty(line)) return Array.Empty<string>();
                var parts = new List<string>();
                var sb = new StringBuilder();
                bool inQuotes = false;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == '"')
                    {
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        parts.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                parts.Add(sb.ToString());
                return parts.ToArray();
            }
        }
    }
}