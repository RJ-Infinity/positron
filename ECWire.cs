using PGL;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace positron
{
    class ECWire : EComponent
    {
        public override int IOs => 2;

        public override void Render(object sender, EventArgs_Draw e)
        {
            using (SKPaint paint = new()
            {
                Color = SKColors.Black,
                StrokeWidth = 5,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
            })
            {
                e.Canvas.DrawLine(
                    ((Form1)sender).WorldToScreen(IONodes[0].Position),
                    ((Form1)sender).WorldToScreen(IONodes[1].Position),
                    paint
                );
            }
        }
    }
}
