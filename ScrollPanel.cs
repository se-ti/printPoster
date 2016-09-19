using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace printPoster
{
    public partial class ScrollPanel : Panel
    {
        public ScrollPanel()
        {
            InitializeComponent();

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                            ControlStyles.UserPaint |
                            ControlStyles.AllPaintingInWmPaint, true);
        }
        public event ZoomEventHandler ZoomEvent;


        // based on http://stackoverflow.com/questions/7828121/shift-mouse-wheel-horizontal-scrolling/11218920#11218920
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                var scrolledPos = e.Location;
                var pos = AutoScrollPosition;
                scrolledPos.Offset(-pos.X, -pos.Y);
                OnZoomEvent(new ZoomEventArgs(scrolledPos, e.Delta / 120));
                return;
            }
            else if (ModifierKeys == Keys.Shift && HScroll && VScroll)
            {
                VScroll = false;
                base.OnMouseWheel(e);
                VScroll = true;
            }
            else
                base.OnMouseWheel(e);
        }

        protected void OnZoomEvent(ZoomEventArgs e)
        {
            ZoomEvent(this, e);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            var hs = HorizontalScroll;
        }

        public class ZoomEventArgs : EventArgs
        {
            public int Delta { get; private set; }
            public Point Location { get; private set; }
            public ZoomEventArgs(Point location, int delta)
            {
                Delta = delta;
                Location = location;
            }
        }

        public delegate void ZoomEventHandler(object sender, ZoomEventArgs args);
    }
}
