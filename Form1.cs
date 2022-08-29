#pragma warning disable IDE0044 // Add readonly modifier
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
        private SKPoint mousePos = new();
        private bool ShowGrid = true;
        private bool ShowMouse = false;
        private Point m_PrevMouseLoc = new();
        public List<EComponent> Components = new();
        private int sideBarWidth=100;
        private SKPoint offset;
        private float zoom = 1;
        private MouseState mouseState = MouseState.None;
        private List<Node> nodes = new();
        private int resizingOffset = 0;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Form1()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        //disabled cause they are initialised in the onload method
        {
            InitializeComponent();
            offset = new(sideBarWidth + 1, 0);
        }

        protected override void OnLoad(EventArgs e)
        {
            // Create layers to draw on, each with a dedicated SKPicture
            layer_Background = new("Background Layer");
            layer_Grid = new("Grid Layer");
            layer_Data = new("Data Layer");
            layer_Overlay = new("Overlay Layer");
            layer_NCDisplay = new("Non Content Layer");

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
            SkiaSurface.Resize += SkiaSurface_Resize;

            base.OnLoad(e);

            // Set the title of the Form
            this.Text = "Positron";


            Components.Add(new ECWire());
            Components[0].IONodes[0] = new(new SKPoint(50, 50));
            Components[0].IONodes[1] = new(new SKPoint(150, 150));

        }

        private void SkiaSurface_Resize(object? sender, EventArgs e)
        {
            if (sideBarWidth < 50)
            {
                sideBarWidth = 50;
            }
            if (sideBarWidth > SkiaSurface.Width - 50)
            {
                sideBarWidth = SkiaSurface.Width - 50;
            }
            layer_NCDisplay.Invalidate();
        }

        public override void UpdateDrawing()
        {
            SetCursor();
            base.UpdateDrawing();
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
                    if (e.Button == MouseButtons.Left)
                    {
                        mouseState = MouseState.ResizeSidebar;
                        resizingOffset = sideBarWidth - e.X;
                    }
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
            SetCursor();
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
            using SKPaint paint = new();
            e.Canvas.DrawLine(new(sideBarWidth + 1, 0), new(sideBarWidth + 1, e.Bounds.Height), paint);
            paint.Color = SKColors.White;
            e.Canvas.DrawRect(new(0, 0, sideBarWidth, e.Bounds.Height), paint);
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
            mousePos = e.Location.ToSKPoint();
            SetCursor();
            if (e.Button == MouseButtons.None)
            {
                mouseState = MouseState.None;
            }

            // If Mouse Move, draw new mouse coordinates
            if (e.Location != m_PrevMouseLoc)
            {
                switch (mouseState)
                {
                    case MouseState.None:
                        break;
                    case MouseState.Panning:{
                        offset.X += e.Location.X - m_PrevMouseLoc.X;
                        offset.Y += e.Location.Y - m_PrevMouseLoc.Y;
                        layer_Data.Invalidate();
                        layer_Grid.Invalidate();
                    } break;
                    case MouseState.ResizeSidebar:{
                        sideBarWidth = e.X - resizingOffset;
                        if (sideBarWidth < 50)
                        {
                            sideBarWidth = 50;
                        }
                        if (sideBarWidth > SkiaSurface.Width - 50)
                        {
                            sideBarWidth = SkiaSurface.Width - 50;
                        }
                        layer_NCDisplay.Invalidate();
                    }break;
                    default:
                        break;
                }
                if (mouseState == MouseState.Panning)
                {

                }
                // Remember the previous mouse location
                m_PrevMouseLoc = e.Location;

                // Invalidate the Overlay Layer to show the new mouse coordinates
                layer_Overlay.Invalidate();
            }
            // Start a new rendering cycle to redraw any invalidated layers.
            UpdateDrawing();
        }
        private void SetCursor()
        {
            Position pos = MousePos(mousePos);
            Cursor nCursor = Cursor.Current;
            switch (pos.Section)
            {
                case Section.SidebarSizer:
                    nCursor = Cursors.SizeWE;
                    break;
                case Section.Main:
                    nCursor = Cursors.Cross;
                    break;
                case Section.None:
                case Section.Sidebar:
                default:
                    nCursor = Cursors.Default;
                    break;
            }
            //make sure the cursor is correct when elements might not have updated yet
            switch (mouseState)
            {
                case MouseState.Panning:
                    nCursor = Cursors.NoMove2D;
                    break;
                case MouseState.ResizeSidebar:
                    nCursor = Cursors.VSplit;
                    break;
                case MouseState.None:
                default:
                    break;
            }
            Cursor = nCursor;
        }
        private void Layer_Background_Draw(object? sender, EventArgs_Draw e)
        {
            // Create a diagonal gradient fill from Blue to Black to use as the background
            SKPoint topLeft = new(e.Bounds.Left, e.Bounds.Top);
            SKPoint bottomRight = new(e.Bounds.Right, e.Bounds.Bottom);
            SKColor[] gradColors = new[] { SKColors.LightBlue, SKColors.Lavender};

            using SKPaint paint = new();
            using SKShader shader = SKShader.CreateLinearGradient(topLeft, bottomRight, gradColors, SKShaderTileMode.Clamp);
            paint.Shader = shader;
            paint.Style = SKPaintStyle.Fill;
            e.Canvas.DrawRect(e.Bounds, paint);
        }
        private void Layer_Grid_Draw(object? sender, EventArgs_Draw e)
        {
            using SKPaint paint = new();
            // Draw a 25x25 grid of gray lines

            paint.Color = SKColors.Gray; // Very dark gray
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 1;

            if (ShowGrid)
            {
                // Draw the Horizontal Grid Lines
                int i = ((int)offset.Y % (int)(30 * zoom));
                while (i < e.Bounds.Height)
                {
                    SKPoint leftPoint = new(e.Bounds.Left, i);
                    SKPoint rightPoint = new(e.Bounds.Right, i);

                    e.Canvas.DrawLine(leftPoint, rightPoint, paint);

                    i += (int)(30 * zoom);
                }

                // Draw the Vertical Grid Lines
                i = ((int)offset.X % (int)(30 * zoom));
                while (i < e.Bounds.Width)
                {
                    SKPoint topPoint = new(i, e.Bounds.Top);
                    SKPoint bottomPoint = new(i, e.Bounds.Bottom);

                    e.Canvas.DrawLine(topPoint, bottomPoint, paint);

                    i += (int)(30 * zoom);
                }
            }
            e.Canvas.DrawCircle(offset, 5, paint);
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

            using SKPaint paint = new();
            if (ShowMouse)
            {
                string str =
                    mousePos.ToString() + "\n" +
                    (offset + mousePos).ToString() + "\n" +
                    zoom;
                List<SKRect> lineboundsList = new();

                float height = 0;
                float width = 0;

                foreach (string line in str.Split("\n"))
                {
                    // Measure the bounds of the text
                    SKRect lineBounds = new();
                    paint.MeasureText(line, ref lineBounds);

                    lineBounds = lineBounds.Standardized;
                    // Fix the inverted height value from the MeaureText

                    height += lineBounds.Height;
                    width = width > lineBounds.Width ? width : lineBounds.Width;

                    lineboundsList.Add(lineBounds);

                }

                SKPoint pos = mousePos;

                if (pos.X < sideBarWidth + 1)
                {
                    pos.X = sideBarWidth + 1;
                }
                if (pos.Y - height < 0)
                {
                    pos.Y = height;
                }
                if (pos.X + width > e.Bounds.Width)
                {
                    pos.X = e.Bounds.Width - width;
                }
                if (pos.Y > e.Bounds.Height)
                {
                    pos.Y = e.Bounds.Height;
                }

                // Configure the Paint to draw a black rectangle behind the text
                paint.Color = SKColors.Black;
                paint.Style = SKPaintStyle.Fill;

                e.Canvas.DrawRect(new(pos.X, pos.Y - height, pos.X + width, pos.Y), paint);

                // Change the Paint to yellow
                paint.Color = SKColors.Yellow;
                float linesHeight = 0;
                int i = 0;
                foreach (string line in str.Split("\n"))
                {
                    linesHeight += lineboundsList[i].Height;
                    e.Canvas.DrawText(
                        line,
                        new(pos.X, pos.Y - height + linesHeight),
                        paint
                    );
                    i++;
                }
                //e.Canvas.DrawText(line2, pos+new SKPoint(0, line1Bounds.Height), paint);
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