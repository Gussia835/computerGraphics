using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;

namespace lab6.Core
{
    public static class ObjIO
    {
        public static Polyhedron LoadFromOBJ(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден по пути: {filePath}");

            var polyhedron = new Polyhedron();
            var culture = CultureInfo.InvariantCulture;

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                string[] tokens = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                string command = tokens[0];

                if (command == "v")
                {
                    if (tokens.Length >= 4)
                    {
                        double x = double.Parse(tokens[1], culture);
                        double y = double.Parse(tokens[2], culture);
                        double z = double.Parse(tokens[3], culture);

                        polyhedron.Vertices.Add(new Point(x, y, z));
                    }
                }
                else if (command == "f")
                {
                    if (tokens.Length >= 4)
                    {
                        var vertexIndices = new List<int>();

                        for (int i = 1; i < tokens.Length; i++)
                        {
                            string[] vertexPart = tokens[i].Split('/');
                            int index = int.Parse(vertexPart[0]);

                            if (index > 0)
                            {
                                vertexIndices.Add(index - 1);
                            }
                            else if (index < 0)
                            {
                                vertexIndices.Add(polyhedron.Vertices.Count + index);
                            }
                        }

                        polyhedron.Faces.Add(new Polygon(vertexIndices.ToArray()));
                    }
                }
            }

            return polyhedron;
        }

        public static void SaveToOBJ(string filePath, Polyhedron polyhedron)
        {
            if (polyhedron == null)
                throw new ArgumentNullException(nameof(polyhedron));

            var sb = new StringBuilder();
            var culture = CultureInfo.InvariantCulture;

            sb.AppendLine("# Wavefront OBJ Export - Лабораторная работа 7 (Часть А)");
            sb.AppendLine($"# Vertices: {polyhedron.Vertices.Count}");
            sb.AppendLine($"# Faces: {polyhedron.Faces.Count}");
            sb.AppendLine();

            foreach (var vertex in polyhedron.Vertices)
            {
                string xStr = vertex.X.ToString("F6", culture);
                string yStr = vertex.Y.ToString("F6", culture);
                string zStr = vertex.Z.ToString("F6", culture);
                sb.AppendLine($"v {xStr} {yStr} {zStr}");
            }

            sb.AppendLine();

            foreach (var face in polyhedron.Faces)
            {
                sb.Append("f");
                foreach (int index in face.VertexIndices)
                {
                    sb.Append($" {index + 1}");
                }
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
