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
        private MouseState mouseState = MouseState.None;
        private Point m_PrevMouseLoc = new();
        
        private bool ShowGrid = true;
        private bool ShowMouse = false;

        public List<EComponent> ComponentsList = new();
        private List<Node> nodes = new();
        
        private SKPoint offset;
        private float zoom = 1;

        private int sideBarWidth=100;
        private int resizingOffset = 0;
        private int scrollOffset = 0;
        private int scrollbarOffset = 0;
        private int scrollbarHeight = 0;
        private int textHeight = 20;
        private int textPadding = 2;
        private int scrollBarWidth = 15;

        private Components hoverComponent = Components.None;
        private Components selectComponent = Components.None;

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

            CalculateScrollbar();

            base.OnLoad(e);

            // Set the title of the Form
            this.Text = "Positron";


            ComponentsList.Add(new ECWire());
            ComponentsList[0].IONodes[0] = new(new SKPoint(50, 50));
            ComponentsList[0].IONodes[1] = new(new SKPoint(150, 150));
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
            UpdateScroll();
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
            Console.WriteLine(mousePos.Section);
            switch (mousePos.Section)
            {
                case Section.None:
                    break;
                case Section.Sidebar:
                    if (e.Button == MouseButtons.Left)
                    {
                        selectComponent = mousePos.Component;
                        layer_NCDisplay.Invalidate();
                    }
                    break;
                case Section.SidebarSizer:{
                    if (e.Button == MouseButtons.Left)
                    {
                        mouseState = MouseState.ResizeSidebar;
                        resizingOffset = sideBarWidth - e.X;
                    }
                } break;
                case Section.ScrollBarUp:{
                    scrollOffset -= 15;
                    UpdateScroll();
                }break;
                case Section.ScrollBarDown:{
                    scrollOffset += 15;
                    UpdateScroll();
                }break;
                case Section.ScrollBarThumb:{
                    mouseState = MouseState.MovingScrollThumb;
                }break;
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
            UpdateDrawing();
        }
        private Position MousePos(SKPoint mouse)
        {
            if (mouse.X > sideBarWidth - 1 && mouse.X < sideBarWidth + 2)
            {
                return new Position
                {
                    Section = Section.SidebarSizer,
                    Node = null,
                    Component = Components.None,
                };
            }
            if (mouse.X < sideBarWidth)
            {
                if (mouse.X > sideBarWidth - scrollBarWidth)
                {
                    if (mouse.Y < scrollBarWidth)
                    {
                        return new Position
                        {
                            Section = Section.ScrollBarUp,
                            Node = null,
                            Component = Components.None,
                        };
                    }
                    if (mouse.Y > SkiaSurface.Height - scrollBarWidth)
                    {
                        return new Position
                        {
                            Section = Section.ScrollBarDown,
                            Node = null,
                            Component = Components.None,
                        };
                    }
                    if (
                        mouse.Y > scrollBarWidth + scrollbarOffset &&
                        mouse.Y < scrollBarWidth + scrollbarOffset + scrollbarHeight
                    )
                    {
                        return new Position
                        {
                            Section = Section.ScrollBarThumb,
                            Node = null,
                            Component = Components.None,
                        };
                    }
                        return new Position
                    {
                        Section = Section.ScrollBar,
                        Node = null,
                        Component = Components.None,
                    };
                }
                Components c = (Components)(((mouse.Y + scrollOffset) / textHeight) + 1);
                return new Position
                {
                    Section = Section.Sidebar,
                    Node = null,
                    Component = Enum.IsDefined(typeof(Components), c)?c:Components.None,
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
                            Node = n,
                            Component = Components.None,
                        };
                    }
                }
                return new Position
                {
                    Section = Section.Main,
                    Node = null,
                    Component = Components.None,
                };
            }
            return new Position
            {
                Section = Section.None,
                Node = null,
                Component = Components.None,
            };
        }
        private void Layer_NCDisplay_Draw(object? sender, EventArgs_Draw e)
        {
            using SKPaint paint = new();
            //resiser
            e.Canvas.DrawLine(new(sideBarWidth + 1, 0), new(sideBarWidth + 1, e.Bounds.Height), paint);
            //background
            paint.Color = SKColors.White;
            e.Canvas.DrawRect(new(0, 0, sideBarWidth - scrollBarWidth, e.Bounds.Height), paint);
            //scrollbar
            paint.Color = SKColors.LightGray;
            //scrollbar bg
            e.Canvas.DrawRect(new(
                sideBarWidth - scrollBarWidth,
                0,
                sideBarWidth,
                e.Bounds.Height
            ), paint);
            //scrollbar arrow up
            paint.Color = SKColors.Black;
            e.Canvas.DrawLine(
                new(sideBarWidth - scrollBarWidth + 1, scrollBarWidth - 1),
                new(sideBarWidth - scrollBarWidth + (float)(scrollBarWidth / 2), 1),
                paint
            );
            e.Canvas.DrawLine(
                new(sideBarWidth - scrollBarWidth + (float)(scrollBarWidth / 2), 1),
                new(sideBarWidth - 1, scrollBarWidth - 1),
                paint
            );
            //scrollbar arrow down
            paint.Color = SKColors.Black;
            e.Canvas.DrawLine(
                new(sideBarWidth - scrollBarWidth + 1, e.Bounds.Height - scrollBarWidth + 1),
                new(sideBarWidth - scrollBarWidth + (float)(scrollBarWidth / 2), e.Bounds.Height - 1),
                paint
            );
            e.Canvas.DrawLine(
                new(sideBarWidth - scrollBarWidth + (float)(scrollBarWidth / 2), e.Bounds.Height + 1),
                new(sideBarWidth - 1, e.Bounds.Height - scrollBarWidth + 1),
                paint
            );
            //thumb
            e.Canvas.DrawRect(new(
                sideBarWidth - scrollBarWidth,
                scrollBarWidth + scrollbarOffset,
                sideBarWidth,
                scrollBarWidth + scrollbarOffset + scrollbarHeight
            ), paint);
            //Components
            paint.TextSize = textHeight - (textPadding * 2);
            paint.Color = SKColors.DarkGray;

            int i = 1;
            foreach (Components component in Enum.GetValues(typeof(Components)))
            {
                if (component == Components.None)
                {
                    continue;
                }
                if (component == hoverComponent)
                {
                    paint.Color = SKColors.DarkGray;
                    e.Canvas.DrawRect(new SKRect(
                        0,
                        ((i - 1) * textHeight) - scrollOffset,
                        sideBarWidth - scrollBarWidth,
                        (i * textHeight) - scrollOffset
                    ), paint);
                }
                if (component == selectComponent)
                {
                    paint.Color = SKColors.Black;
                    e.Canvas.DrawRect(new SKRect(
                        0,
                        ((i - 1) * textHeight) - scrollOffset,
                        sideBarWidth - scrollBarWidth,
                        (i * textHeight) - scrollOffset
                    ), paint);
                    paint.Color = SKColors.LightBlue;
                }
                else
                {
                    paint.Color = SKColors.Black;
                }
                e.Canvas.DrawText(component.ToString(), textPadding, (i * textHeight) - textPadding - scrollOffset, paint);
                i++;
            }
        }

        private void SkiaSurface_MouseWheel(object? sender, MouseEventArgs e)
        {
            Position pos = MousePos(e.Location.ToSKPoint());
            switch (pos.Section)
            {
                case Section.Main:{
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
                }break;
                case Section.Sidebar:{
                    scrollOffset -= e.Delta/20;
                    UpdateScroll();
                }break;
                case Section.None:
                case Section.SidebarSizer:
                default:
                    break;
            }

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
        private void UpdateScroll()
        {
            if (scrollOffset > ((Enum.GetNames(typeof(Components)).Length - 1) * textHeight) - SkiaSurface.Height)
            {
                scrollOffset = ((Enum.GetNames(typeof(Components)).Length - 1) * textHeight) - SkiaSurface.Height;
            }
            if (scrollOffset < 0)
            {
                scrollOffset = 0;
            }
            CalculateScrollbar();
            layer_NCDisplay.Invalidate();
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
            Position mp = MousePos(mousePos);
            if (mp.Section != Section.Sidebar && hoverComponent != Components.None)
            {
                hoverComponent = Components.None;
                layer_NCDisplay.Invalidate();
            }

            switch (mp.Section)
            {
                case Section.Sidebar:
                    if (hoverComponent != mp.Component)
                    {
                        hoverComponent = mp.Component;
                        layer_NCDisplay.Invalidate();
                    }
                    break;
                case Section.None:
                case Section.SidebarSizer:
                case Section.Main:
                default:
                    break;
            }

            // If Mouse Move, draw new mouse coordinates
            if (e.Location != m_PrevMouseLoc)
            {
                switch (mouseState)
                {
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
                    case MouseState.MovingScrollThumb:{
                        moveThumb(e.Location.Y - m_PrevMouseLoc.Y);
                    }break;
                    case MouseState.None:
                    default:
                        break;
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
        private void moveThumb(int amount)
        {
            if (scrollbarHeight == SkiaSurface.Height - (scrollBarWidth * 2))
            {
                return;
            }
            //this is a rearanged version of the calculation for the scrollbarOffset in CalculateScrollbar
            scrollbarOffset = scrollbarOffset + amount;
            if (scrollbarOffset > SkiaSurface.Height - (scrollBarWidth * 2) - scrollbarHeight)
            {
                scrollbarOffset = SkiaSurface.Height - (scrollBarWidth * 2) - scrollbarHeight;
            }
            if (scrollbarOffset < 0)
            {
                scrollbarOffset = 0;
            }
            scrollOffset = (int)(
                scrollbarOffset *
                (((Enum.GetNames(typeof(Components)).Length - 1) * textHeight) - SkiaSurface.Height) /
                (float)(SkiaSurface.Height - (scrollBarWidth * 2) - scrollbarHeight)
            );
            layer_NCDisplay.Invalidate();
        }
        private void CalculateScrollbar()
        {
            int contentHeight = (Enum.GetNames(typeof(Components)).Length - 1) * textHeight;
            if (SkiaSurface.Height >= contentHeight)
            {
                scrollbarOffset = 0;
                scrollbarHeight = SkiaSurface.Height - (scrollBarWidth * 2);
            }
            else
            {
                //esentialy just a lerp
                scrollbarHeight = (int)((SkiaSurface.Height / (float)contentHeight) * (SkiaSurface.Height - (scrollBarWidth * 2)));
                scrollbarHeight = Math.Max(
                    Math.Min(20, SkiaSurface.Height - (scrollBarWidth * 2)),
                    scrollbarHeight
                );
                //esentialy just a lerp again
                scrollbarOffset = (int)(scrollOffset / (float)(contentHeight - SkiaSurface.Height) * (SkiaSurface.Height - (scrollBarWidth * 2) - scrollbarHeight));
            }
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
            foreach (EComponent comp in ComponentsList)
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
                int padding = 2;
                string str =
                    "ScreenPos:"+mousePos.ToString() + "\n" +
                    "WorldPos:"+ScreenToWorld(mousePos).ToString() + "\n" +
                    "Zoom:"+(int)(zoom*100)+"%";
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
                    lineBounds.Top -= padding;
                    lineBounds.Left -= padding;
                    lineBounds.Right += padding;
                    lineBounds.Bottom += padding;

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
                        new(pos.X + padding, pos.Y - height + linesHeight - padding),
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
            MovingScrollThumb,
        }
        private enum Section
        {
            None,
            Main,
            Sidebar,
            SidebarSizer,
            ScrollBar,
            ScrollBarUp,
            ScrollBarDown,
            ScrollBarThumb,
        }
        private enum Components
        {
            None,
            Wire,
            Test,
            Test2,
            Test3,
            Test4,
            Test5,
            Test6,
            Test7,
            Test8,
            Test9,
            Test10,
            Test11,
            Test12,
            Test13,
            Test14,
            Test15,
            Test16,
            Test17,
        }
        private struct Position
        {
            public Section Section;
            public Node? Node;
            public Components Component;
        }
    }
}