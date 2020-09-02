using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WGestures.Common.OsSpecific.Windows;

namespace WGestures.App.Gui.Windows.Controls
{
    internal class ColorButton : LazyPaintButton
    {
        private readonly float _dpiFactor = Native.GetScreenDpi() / 96.0f;
        private readonly Pen borderPen;

        private readonly Pen mainPen;
        private readonly Pen shadowPen;
        private Color _color = Color.Transparent;

        public ColorButton()
        {
            mainPen = new Pen(this.Color, 2.0f * _dpiFactor);
            borderPen = new Pen(Color.FromArgb(255, 255, 255, 255), 4.5f * _dpiFactor);
            shadowPen = new Pen(Color.FromArgb(80, 0, 0, 0), 5.0f * _dpiFactor);
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                mainPen.Color = _color;
                this.Invalidate();
                if (this.ColorChanged != null)
                {
                    this.ColorChanged(this, new EventArgs());
                }
            }
        }

        public event EventHandler ColorChanged;

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            var g = pevent.Graphics;

            g.SetClip(pevent.ClipRectangle);

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            //g.Clear(BackColor);

            var bias = 5 * _dpiFactor;

            var rect = Rectangle.Inflate(this.Bounds, (int) -bias, (int) -bias);
            rect.X = (int) bias;
            rect.Y = (int) bias;

            /*g.DrawEllipse(shadowPen, rect);
            g.DrawEllipse(borderPen, rect);
            g.DrawEllipse(mainPen, rect);*/

            var p0 = new Point(rect.Left, rect.Bottom);
            var p1 = new Point(rect.Right, rect.Bottom);
            g.DrawLine(shadowPen, p0, p1);
            g.DrawLine(borderPen, p0, p1);
            g.DrawLine(mainPen, p0, p1);
            /*g.DrawRectangle(shadowPen, rect);
            g.DrawRectangle(borderPen, rect);
            g.DrawRectangle(mainPen, rect);*/
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            using (var colorDlg = new ColorDialog())
            {
                colorDlg.AnyColor = true;
                colorDlg.FullOpen = true;

                colorDlg.Color = this.Color;

                var ok = colorDlg.ShowDialog();
                if (ok == DialogResult.OK)
                {
                    this.Color = colorDlg.Color;
                }
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mainPen.Dispose();
                borderPen.Dispose();
                shadowPen.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
