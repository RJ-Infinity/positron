using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PGL
{
    //https://stackoverflow.com/a/65056572/15755351
    public class Layer
    {
        // The Draw event that the background rendering thread will use to draw on the SKPicture Canvas.  
        public event EventHandler<EventArgs_Draw> Draw;

        // The finished recording - Used to play back the Draw commands to the SKGLControl from the GUI thread
        private SKPicture m_Picture = null;

        // A flag that indicates if the Layer is valid, or needs to be redrawn.
        private bool m_IsValid = false;


        // ---------------------------
        // --- Layer - Constructor ---
        // ---------------------------

        public Layer(string title)
        {
            this.Title = title;
        }


        // -------------
        // --- Title ---
        // -------------

        public string Title { get; set; }


        // --------------
        // --- Render ---
        // --------------


        // Raises the Draw event and records any drawing commands to an SKPicture for later playback.  
        // This can be called from any thread.


        public void Render(SKRect clippingBounds)
        {
            // Only redraw the Layer if it has been invalidated
            if (!m_IsValid)
            {
                // Create an SKPictureRecorder to record the Canvas Draw commands to an SKPicture
                using (var recorder = new SKPictureRecorder())
                {
                    // Start recording 
                    recorder.BeginRecording(clippingBounds);

                    // Raise the Draw event.  The subscriber can then draw on the Canvas provided in the event
                    // and the commands will be recorded for later playback.
                    Draw?.Invoke(this, new EventArgs_Draw(recorder.RecordingCanvas, clippingBounds));

                    // Dispose of any previous Pictures
                    m_Picture?.Dispose();

                    // Create a new SKPicture with recorded Draw commands 
                    m_Picture = recorder.EndRecording();

                    this.RenderCount++;

                    m_IsValid = true;
                }
            }
        }


        // --------------------
        // --- Render Count ---
        // --------------------

        // Gets the number of times that this Layer has been rendered

        public int RenderCount { get; private set; }


        // -------------
        // --- Paint ---
        // -------------

        // Paints the previously recorded SKPicture to the provided skglControlCanvas.  This basically plays 
        // back the draw commands from the last Render.  This should be called from the SKGLControl.PaintSurface
        // event using the GUI thread.

        public void Paint(SKCanvas skglControlCanvas)
        {
            if (m_Picture != null)
            {
                // Play back the previously recorded Draw commands to the skglControlCanvas using the GUI thread
                skglControlCanvas.DrawPicture(m_Picture);

                this.PaintCount++;
            }
        }


        // --------------------
        // --- Render Count ---
        // --------------------

        // Gets the number of times that this Layer has been painted

        public int PaintCount { get; private set; }


        // ------------------
        // --- Invalidate ---
        // ------------------

        // Forces the Layer to be redrawn with the next rendering cycle

        public void Invalidate()
        {
            m_IsValid = false;
        }
    }
}
