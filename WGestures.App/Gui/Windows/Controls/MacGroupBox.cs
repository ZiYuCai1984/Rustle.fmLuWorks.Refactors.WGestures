using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WGestures.Common.OsSpecific.Windows;

namespace WGestures.App.Gui.Windows.Controls
{
    internal class MacGroupBox : GroupBox
    {
        private readonly float _dpiFactor = Native.GetScreenDpi() / 96.0f;


        protected override void OnPaint(PaintEventArgs e)
        {
            const int cornerRadius = 6;
            var actualCornerRadius = (int) (cornerRadius * _dpiFactor);
            const int distTxtAndBox = 2;
            var actualDistTxtAndbox = (int) (distTxtAndBox * _dpiFactor);


            var g = e.Graphics;
            g.SetClip(e.ClipRectangle);
            g.Clear(this.Parent.BackColor);

            var strSize = g.MeasureString(this.Text, this.Font);

            g.DrawString(this.Text, this.Font, Brushes.Black, 0, 0);
            var rect = new Rectangle(
                (int) (1 * _dpiFactor),
                (int) (strSize.Height + actualDistTxtAndbox),
                (int) (this.Width - 2 * _dpiFactor),
                (int) (this.Height - strSize.Height - actualDistTxtAndbox));

            using (var pen = new Pen(Color.FromArgb(210, 210, 210), 1 * _dpiFactor))
            {
                this.DrawRoundedRectangle(g, rect, actualCornerRadius, pen, this.BackColor);

                g.SetClip(
                    new Rectangle(
                        (int) (2 * _dpiFactor),
                        (int) (3 * _dpiFactor),
                        (int) (this.Width - 4 * _dpiFactor),
                        this.Height / 2));
                for (var i = 0; i < 4; i++)
                {
                    pen.Color = Color.FromArgb(pen.Color.A - 63, pen.Color);

                    rect.Y += (int) (1 * _dpiFactor);

                    this.DrawRoundedRectangle(g, rect, actualCornerRadius, pen, this.BackColor);
                }

                rect = new Rectangle(
                    (int) (1 * _dpiFactor),
                    (int) (strSize.Height + actualDistTxtAndbox),
                    (int) (this.Width - 2 * _dpiFactor),
                    (int) (this.Height - strSize.Height - actualDistTxtAndbox));

                g.ResetClip();
                pen.Color = Color.FromArgb(210, 210, 210);
                this.DrawRoundedRectangle(g, rect, actualCornerRadius, pen, Color.Transparent);

                rect = Rectangle.Inflate(rect, (int) (-1 * _dpiFactor), (int) (-1 * _dpiFactor));
                pen.Color = Color.FromArgb(80, pen.Color);
                this.DrawRoundedRectangle(g, rect, actualCornerRadius, pen, Color.Transparent);

                g.SetClip(
                    new Rectangle(0, this.Height - actualCornerRadius, this.Width, this.Height));
                pen.Color = Color.FromArgb(80, Color.White);
                this.DrawRoundedRectangle(
                    g,
                    new Rectangle(0, 0, this.Width, this.Height),
                    actualCornerRadius,
                    pen,
                    Color.Transparent);
            }


            //base.OnPaint(e);
        }

        private void DrawRoundedRectangle(
            Graphics gfx, Rectangle Bounds, int CornerRadius, Pen DrawPen, Color FillColor)
        {
            var strokeOffset = Convert.ToInt32(Math.Ceiling(DrawPen.Width));
            Bounds = Rectangle.Inflate(Bounds, -strokeOffset, -strokeOffset);

            //DrawPen.EndCap = DrawPen.StartCap = LineCap.Round;

            using (var gfxPath = new GraphicsPath())
            {
                gfxPath.AddArc(Bounds.X, Bounds.Y, CornerRadius, CornerRadius, 180, 90);
                gfxPath.AddArc(
                    Bounds.X + Bounds.Width - CornerRadius,
                    Bounds.Y,
                    CornerRadius,
                    CornerRadius,
                    270,
                    90);
                gfxPath.AddArc(
                    Bounds.X + Bounds.Width - CornerRadius,
                    Bounds.Y + Bounds.Height - CornerRadius,
                    CornerRadius,
                    CornerRadius,
                    0,
                    90);
                gfxPath.AddArc(
                    Bounds.X,
                    Bounds.Y + Bounds.Height - CornerRadius,
                    CornerRadius,
                    CornerRadius,
                    90,
                    90);
                gfxPath.CloseAllFigures();


                using (var sb = new SolidBrush(FillColor))
                {
                    gfx.FillPath(sb, gfxPath);
                }

                gfx.DrawPath(DrawPen, gfxPath);
            }
        }
    }
}
