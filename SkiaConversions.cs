using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace positron
{
    public static class SkiaConversions
    {
        public static SKPoint ToSkiaPoint(this PointF p)=>new SKPoint(p.X,p.Y);
        public static PointF ToSDPoint(this SKPoint p)=>new PointF(p.X,p.Y);
    }
}
