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
        public abstract void Render(object sender, EventArgs_Draw e);
        public EComponent()
        {
            IONodes = new Node[IOs];
        }
    }
}
