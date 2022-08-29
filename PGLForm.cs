using System;
using System.ComponentModel;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace PGL
{
    public class PGLForm : Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.SkiaSurface = new SkiaSharp.Views.Desktop.SKGLControl();
            this.SuspendLayout();
            // 
            // skglControl1
            // 
            SkiaSurface.BackColor = Color.Black;
            SkiaSurface.Dock = DockStyle.Fill;
            SkiaSurface.Location = new Point(0, 0);
            SkiaSurface.Margin = new Padding(4, 3, 4, 3);
            SkiaSurface.Name = "skglControl1";
            SkiaSurface.Size = new Size(800, 450);
            SkiaSurface.TabIndex = 0;
            SkiaSurface.VSync = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(SkiaSurface);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }
        public SKGLControl SkiaSurface { get; set; }

        public List<Layer> Layers = new List<Layer>();

        private Thread m_RenderThread;
        private AutoResetEvent m_ThreadGate;
        private bool alive = true;

        public PGLForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Subscribe to the SKGLControl events
            SkiaSurface.PaintSurface += SkglControl1_PaintSurface;
            SkiaSurface.Resize += SkglControl1_Resize;

            // Create a background rendering thread
            m_RenderThread = new Thread(RenderLoopMethod);
            m_ThreadGate = new AutoResetEvent(false);

            // Start the rendering thread
            m_RenderThread.Start();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            // Let the rendering thread terminate
            alive = false;
            m_ThreadGate.Set();

            base.OnClosing(e);
        }
        private void SkglControl1_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            // Clear the Canvas
            e.Surface.Canvas.Clear(SKColors.Black);

            // Paint each pre-rendered layer onto the Canvas using this GUI thread
            foreach (Layer layer in Layers)
            {
                layer.Paint(e.Surface.Canvas);
            }


            //using (SKPaint paint = new SKPaint())
            //{
            //    paint.Color = SKColors.LimeGreen;

                //for (int i = 0; i < Layers.Count; i++)
                //{
                //    Layer layer = Layers[i];
                //    string text = $"{layer.Title} - Renders = {layer.RenderCount}, Paints = {layer.PaintCount}";
                //    SKPoint textLoc = new SKPoint(10, 10 + (i * 15));

                //    e.Surface.Canvas.DrawText(text, textLoc, paint);
                //}

            //    paint.Color = SKColors.Cyan;
            //}
        }
        private void SkglControl1_Resize(object sender, EventArgs e)
        {
            // Invalidate all of the Layers
            foreach (Layer layer in Layers)
            {
                layer.Invalidate();
            }

            // Start a new rendering cycle to redraw all of the layers.
            UpdateDrawing();
        }
        public virtual void UpdateDrawing()
        {
            // Unblock the rendering thread to begin a render cycle.  Only the invalidated
            // Layers will be re-rendered, but all will be repainted onto the SKGLControl.
            m_ThreadGate.Set();
        }
        private void RenderLoopMethod()
        {
            while (alive)
            {
                // Draw any invalidated layers using this Render thread
                DrawLayers();

                // Invalidate the SKGLControl to run the PaintSurface event on the GUI thread
                // The PaintSurface event will Paint the layer stack to the SKGLControl
                SkiaSurface.Invalidate();

                // DoEvents to ensure that the GUI has time to process
                Application.DoEvents();

                // Block and wait for the next rendering cycle
                m_ThreadGate.WaitOne();
            }
        }
        private void DrawLayers()
        {
            // Iterate through the collection of layers and raise the Draw event for each layer that is
            // invalidated.  Each event handler will receive a Canvas to draw on along with the Bounds for 
            // the Canvas, and can then draw the contents of that layer. The Draw commands are recorded and  
            // stored in an SKPicture for later playback to the SKGLControl.  This method can be called from
            // any thread.

            SKRect clippingBounds = SkiaSurface.ClientRectangle.ToSKRect();

            foreach (Layer layer in Layers)
            {
                layer.Render(clippingBounds);
            }
        }
    }
}
