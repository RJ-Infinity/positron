using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace PGL
{
    //https://stackoverflow.com/a/65056572/15755351
    public class EventArgs_Draw : EventArgs
    {
        public SKRect Bounds { get; set; }
        public SKCanvas Canvas { get; set; }

        public EventArgs_Draw(SKCanvas canvas, SKRect bounds)
        {
            this.Canvas = canvas;
            this.Bounds = bounds;
        }
    }
}
