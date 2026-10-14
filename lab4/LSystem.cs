using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace lab4
{
    public class LSystem
    {
        public string Axiom { get; set; } = "F";
        public double Angle { get; set; } = 25.0;
        public double InitialDirection { get; set; } = 90.0;
        public Dictionary<char, string> Rules { get; } = new Dictionary<char, string>();

        public int Iterations { get; set; } = 5;
        public double RandomAngleVariation { get; set; } = 0.0;
        public double StepLength { get; set; } = 5.0;

        public static LSystem LoadFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                throw new InvalidDataException("Файл пуст.");

            var lsystem = new LSystem();

            var first = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (first.Length < 3)
                throw new InvalidDataException("Первая строка должна содержать: атом, угол, начальное направление.");

            lsystem.Axiom = first[0];
            lsystem.Angle = double.Parse(first[1], CultureInfo.InvariantCulture);
            lsystem.InitialDirection = double.Parse(first[2], CultureInfo.InvariantCulture);

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                char symbol = line.Substring(0, eq).Trim()[0];
                string replacement = line.Substring(eq + 1).Trim();
                lsystem.Rules[symbol] = replacement;
            }

            return lsystem;
        }

        public string Generate(int iterations)
        {
            string current = Axiom;
            var sb = new StringBuilder();

            for (int i = 0; i < iterations; i++)
            {
                sb.Clear();
                foreach (char c in current)
                {
                    if (Rules.TryGetValue(c, out string rule))
                        sb.Append(rule);
                    else
                        sb.Append(c);
                }
                current = sb.ToString();
            }

            return current;
        }
    }
}