using PGL;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace positron
{
    public abstract class EComponent
    {
        public abstract int IOs { get; }
        public Node[] IONodes;
        public abstract SKRect BoundingBox { get; }
        public abstract void Render(object sender, EventArgs_Draw e);
        public EComponent(SKPoint position)
        {
            IONodes = new Node[IOs];
        }
    }
}
