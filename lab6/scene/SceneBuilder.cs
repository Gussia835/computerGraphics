using lab6.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab6.scene
{
    public static class SceneBuilder
    {
        public static Polyhedron Combine(params Polyhedron[] objects)
        {
            var scene = new Polyhedron();
            int vertexOffset = 0;

            foreach (var obj in objects)
            {
                // Добавляем вершины
                foreach (var v in obj.Vertices)
                {
                    scene.Vertices.Add(v);
                }

                // Добавляем грани со смещенными индексами
                foreach (var face in obj.Faces)
                {
                    var shiftedIndices = face.VertexIndices
                        .Select(idx => idx + vertexOffset)
                        .ToArray();
                    scene.Faces.Add(new Polygon(shiftedIndices));
                }

                vertexOffset += obj.Vertices.Count;
            }

            return scene;
        }
    }
}
