using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using PGL;

//https://stackoverflow.com/a/65056572/15755351


namespace positron
{
    public partial class Form1 : PGLForm
    {
        private Layer layer_Background;
        private Layer layer_Grid;
        private Layer layer_Data;
        private Layer layer_Overlay;
        private Layer layer_NCDisplay;
        private SKPoint m_MousePos = new SKPoint();
        private bool ShowGrid = true;
        private bool ShowMouse = false;
        private Point m_PrevMouseLoc = new Point();
        public List<EComponent> Components = new List<EComponent>();
        private int sideBarWidth=100;
        private SKPoint offset;
        private float zoom = 1;
        private MouseState mouseState = MouseState.None;
        private List<Node> nodes = new List<Node>();

        public Form1()
        {
            InitializeComponent();
            offset = new SKPoint(sideBarWidth + 1, 0);
        }

        protected override void OnLoad(EventArgs e)
        {
            // Create layers to draw on, each with a dedicated SKPicture
            layer_Background = new Layer("Background Layer");
            layer_Grid = new Layer("Grid Layer");
            layer_Data = new Layer("Data Layer");
            layer_Overlay = new Layer("Overlay Layer");
            layer_NCDisplay = new Layer("Non Content Layer");

            // Add the drawing layers to there collection
            Layers.Add(layer_Background);
            Layers.Add(layer_Grid);
            Layers.Add(layer_Data);
            Layers.Add(layer_Overlay);
            Layers.Add(layer_NCDisplay);

            // Subscribe to the Draw Events for each layer
            layer_Background.Draw += Layer_Background_Draw;
            layer_Grid.Draw += Layer_Grid_Draw;
            layer_Data.Draw += Layer_Data_Draw;
            layer_Overlay.Draw += Layer_Overlay_Draw;
            layer_NCDisplay.Draw += Layer_NCDisplay_Draw;

            // Subscribe to the SKGLControl events
            SkiaSurface.MouseMove += SkglControl1_MouseMove;
            SkiaSurface.MouseDown += SkiaSurface_MouseDown;
            SkiaSurface.KeyDown += SkiaSurface_KeyDown;
            SkiaSurface.MouseWheel += SkiaSurface_MouseWheel;

            base.OnLoad(e);

            // Set the title of the Form
            this.Text = "Positron";


            Components.Add(new ECWire());
            Components[0].IONodes[0] = new Node(new SKPoint(50, 50));
            Components[0].IONodes[1] = new Node(new SKPoint(150, 150));

        }

        private void SkiaSurface_MouseDown(object? sender, MouseEventArgs e)
        {
            Position mousePos = MousePos(e.Location.ToSKPoint());
            switch (mousePos.Section)
            {
                case Section.None:
                    break;
                case Section.Sidebar:
                    break;
                case Section.SidebarSizer:
                    break;
                case Section.Main:
                    if (
                        e.Button == MouseButtons.Middle ||
                        (
                            e.Button == MouseButtons.Left &&
                            ModifierKeys == Keys.Alt
                        )
                    )
                    {
                        mouseState = MouseState.Panning;
                    }
                    break;
                default:
                    break;
            }
        }
        private Position MousePos(SKPoint mouse)
        {
            if (mouse.X > sideBarWidth - 1 && mouse.X < sideBarWidth + 2)
            {
                return new Position
                {
                    Section = Section.SidebarSizer,
                    Node = null
                };
            }
            if (mouse.X < sideBarWidth)
            {
                return new Position
                {
                    Section = Section.Sidebar,
                    Node = null
                };
            }
            if (mouse.X > sideBarWidth)
            {
                foreach (Node n in nodes)
                {
                    SKPoint pos = WorldToScreen(n.Position);
                    if (
                        new SKRect(
                            pos.X - 2,
                            pos.Y - 2,
                            pos.X + 2,
                            pos.Y + 2
                        )
                        .Contains(mouse)
                    )
                    {
                        return new Position
                        {
                            Section = Section.Main,
                            Node = n
                        };
                    }
                }
                return new Position
                {
                    Section = Section.Main,
                    Node = null
                };
            }
            return new Position
            {
                Section = Section.None,
                Node = null
            };
        }
        private void Layer_NCDisplay_Draw(object? sender, EventArgs_Draw e)
        {
            using (SKPaint paint = new SKPaint())
            {
                e.Canvas.DrawLine(new SKPoint(sideBarWidth + 1, 0), new SKPoint(100, e.Bounds.Height), paint);
                paint.Color = SKColors.White;
                e.Canvas.DrawRect(new SKRect(0, 0, sideBarWidth, e.Bounds.Height), paint);
            }
        }

        private void SkiaSurface_MouseWheel(object? sender, MouseEventArgs e)
        {
            SKPoint InitialWorldPos = ScreenToWorld(e.Location.ToSKPoint());
            zoom *= 1 + (float)((e.Delta / SystemInformation.MouseWheelScrollDelta)*0.1);
            if (zoom > 15)
            {
                zoom = 15;
            }
            if (zoom < 0.15)
            {
                zoom = 0.15F;
            }
            offset += e.Location.ToSKPoint() - WorldToScreen(InitialWorldPos);
            layer_Data.Invalidate();
            layer_Grid.Invalidate();
            layer_Overlay.Invalidate();
            Console.WriteLine(zoom);
            UpdateDrawing();
        }

        private void SkiaSurface_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                ShowMouse = !ShowMouse;
                layer_Overlay.Invalidate();
            }
            if (e.KeyCode == Keys.F2)
            {
                ShowGrid = !ShowGrid;
                layer_Grid.Invalidate();
            }
            if (e.KeyCode == Keys.F3)
            {
                offset.X = sideBarWidth+1;
                offset.Y = 0;
                zoom = 1;
                layer_Grid.Invalidate();
                layer_Data.Invalidate();
                layer_Overlay.Invalidate();
            }
            UpdateDrawing();
        }

        private void SkglControl1_MouseMove(object? sender, MouseEventArgs e)
        {
            // Save the mouse position
            m_MousePos = e.Location.ToSKPoint();

            if (e.Button == MouseButtons.None)
            {
                mouseState = MouseState.None;
            }

            // If Mouse Move, draw new mouse coordinates
            if (e.Location != m_PrevMouseLoc)
            {
                if (mouseState == MouseState.Panning)
                {
                    offset.X += e.Location.X - m_PrevMouseLoc.X;
                    offset.Y += e.Location.Y - m_PrevMouseLoc.Y;
                    layer_Data.Invalidate();
                    layer_Grid.Invalidate();
                }
                // Remember the previous mouse location
                m_PrevMouseLoc = e.Location;

                // Invalidate the Overlay Layer to show the new mouse coordinates
                layer_Overlay.Invalidate();
            }
            // Start a new rendering cycle to redraw any invalidated layers.
            UpdateDrawing();
        }
        private void Layer_Background_Draw(object? sender, EventArgs_Draw e)
        {
            // Create a diagonal gradient fill from Blue to Black to use as the background
            SKPoint topLeft = new SKPoint(e.Bounds.Left, e.Bounds.Top);
            SKPoint bottomRight = new SKPoint(e.Bounds.Right, e.Bounds.Bottom);
            SKColor[] gradColors = new SKColor[] { SKColors.LightBlue, SKColors.Lavender};

            using (SKPaint paint = new SKPaint())
            using (SKShader shader = SKShader.CreateLinearGradient(topLeft, bottomRight, gradColors, SKShaderTileMode.Clamp))
            {
                paint.Shader = shader;
                paint.Style = SKPaintStyle.Fill;
                e.Canvas.DrawRect(e.Bounds, paint);
            }
        }
        private void Layer_Grid_Draw(object? sender, EventArgs_Draw e)
        {
            using (SKPaint paint = new SKPaint())
            {
                // Draw a 25x25 grid of gray lines

                paint.Color = SKColors.Gray; // Very dark gray
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1;

                if (ShowGrid)
                {
                    // Draw the Horizontal Grid Lines
                    int i = ((int)offset.Y % (int)(30*zoom));
                    while (i < e.Bounds.Height)
                    {
                        SKPoint leftPoint = new SKPoint(e.Bounds.Left, i);
                        SKPoint rightPoint = new SKPoint(e.Bounds.Right, i);

                        e.Canvas.DrawLine(leftPoint, rightPoint, paint);

                        i += (int)(30 * zoom);
                    }

                    // Draw the Vertical Grid Lines
                    i = ((int)offset.X % (int)(30 * zoom));
                    while (i < e.Bounds.Width)
                    {
                        SKPoint topPoint = new SKPoint(i, e.Bounds.Top);
                        SKPoint bottomPoint = new SKPoint(i, e.Bounds.Bottom);

                        e.Canvas.DrawLine(topPoint, bottomPoint, paint);

                        i += (int)(30 * zoom);
                    }
                }
                e.Canvas.DrawCircle(offset, 5, paint);
            }
        }
        private void Layer_Data_Draw(object? sender, EventArgs_Draw e)
        {
            foreach (EComponent comp in Components)
            {
                comp.Render(this, e);
            }
        }

        private void Layer_Overlay_Draw(object? sender, EventArgs_Draw e)
        {
            // Draw the mouse coordinate text next to the cursor

            using (SKPaint paint = new SKPaint())
            {
                if (ShowMouse)
                {
                    // Configure the Paint to draw a black rectangle behind the text
                    paint.Color = SKColors.Black;
                    paint.Style = SKPaintStyle.Fill;

                    // Measure the bounds of the text
                    string line1 = m_MousePos.ToString();// +"\n"+ (offset+m_MousePos).ToString();
                    SKRect line1Bounds = new SKRect();
                    paint.MeasureText(line1, ref line1Bounds);

                    // Fix the inverted height value from the MeaureText
                    line1Bounds = line1Bounds.Standardized;

                    string line2 = ScreenToWorld(m_MousePos).ToString();
                    SKRect line2Bounds = new SKRect();
                    paint.MeasureText(line2, ref line2Bounds);

                    // Fix the inverted height value from the MeaureText
                    line1Bounds = line1Bounds.Standardized;

                    SKPoint pos = m_MousePos;

                    if (pos.X > SkiaSurface.Width - line1Bounds.Width)
                    {pos.X = SkiaSurface.Width - line1Bounds.Width;}
                    if (pos.X > SkiaSurface.Width - line2Bounds.Width)
                    { pos.X = SkiaSurface.Width - line2Bounds.Width; }
                    if (pos.Y - (line1Bounds.Height + line2Bounds.Height) < 0)
                    {pos.Y = line1Bounds.Height + line2Bounds.Height; }
                    if (pos.X < 0)
                    {pos.X = 0;}
                    if (pos.Y > SkiaSurface.Height)
                    {pos.Y = SkiaSurface.Height;}
                    pos.Y -= line1Bounds.Height + line2Bounds.Height;
                    //textBounds.Location = new SKPoint(m_MousePos.X, m_MousePos.Y - textBounds.Height);
                    line1Bounds.Location = pos;
                    line2Bounds.Location = pos + new SKPoint(0,line1Bounds.Height);
                    // Draw the black filled rectangle where the text will go
                    e.Canvas.DrawRect(line1Bounds, paint);
                    e.Canvas.DrawRect(line2Bounds, paint);
                    // Change the Paint to yellow
                    paint.Color = SKColors.Yellow;
                    pos.Y += line1Bounds.Height;
                    // Draw the mouse coordinates text
                    e.Canvas.DrawText(line1, pos, paint);
                    e.Canvas.DrawText(line2, pos+new SKPoint(0, line1Bounds.Height), paint);
                }
            }
        }
        public float ScreenToWorld(float s) => ScreenToWorld(new SKPoint(s, 0)).X;
        public SKPoint ScreenToWorld(SKPoint s)
        {
            SKPoint rv = (s - offset);
            rv.X /= zoom;
            rv.Y /= zoom;
            return rv;
        }
        public float WorldToScreen(float s) => WorldToScreen(new SKPoint(s, 0)).X;
        public SKPoint WorldToScreen(SKPoint w)
        {
            SKPoint rv = w;
            rv.X *= zoom;
            rv.Y *= zoom;
            rv += offset;
            return rv;
        }
        private enum MouseState
        {
            None,
            Panning,
            ResizeSidebar,
        }
        private enum Section
        {
            None,
            Sidebar,
            SidebarSizer,
            Main,
        }
        private struct Position
        {
            public Section Section;
            public Node? Node;
        }
    }
}