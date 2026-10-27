using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab6.Core
{
    public class Polygon
    {
        public int[] VertexIndices { get; set; }

        public Polygon(params int[] indices)
        {
            VertexIndices = indices;
        }
    }
}
