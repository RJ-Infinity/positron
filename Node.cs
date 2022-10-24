using SkiaSharp;

namespace positron
{
    public class Node
    {
        public SKPoint Position;
        public SKColor Colour;
        public Node(SKPoint position, SKColor colour)
        {
            Position = position;
            Colour = colour;
        }
    }
}