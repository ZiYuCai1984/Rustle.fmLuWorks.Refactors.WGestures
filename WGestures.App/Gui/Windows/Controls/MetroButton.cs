using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WGestures.App.Gui.Windows.Controls
{
    internal class MetroButton : LazyPaintButton
    {
        private static readonly Color normalBgColor = Color.FromArgb(240, 240, 240);
        private static readonly Color pressedBgColor = Color.FromArgb(220, 220, 220);
        private Color _bgColor = normalBgColor;


        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _bgColor = pressedBgColor;
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _bgColor = normalBgColor;
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SetClip(pevent.ClipRectangle);


            g.Clear(_bgColor);
            using (var b = new SolidBrush(Color.FromArgb(130, Color.White)))
            {
                g.FillRectangle(b, new Rectangle(0, 0, this.Width, this.Height / 2));
            }

            if (this.Image != null)
            {
                var limitW = this.Width * 0.8f;
                var limitH = this.Height * 0.8f;

                var scale = 1.0f;
                SizeF newSize = this.Image.Size;


                if (newSize.Width >= limitW)
                {
                    scale = limitW / newSize.Width;
                    newSize.Width = limitW;
                    newSize.Height *= scale;
                }

                if (newSize.Height >= limitH)
                {
                    scale = limitH / newSize.Height;
                    newSize.Height = limitH;
                    newSize.Width *= scale;
                }

                if (this.Enabled)
                {
                    g.DrawImage(
                        this.Image,
                        (this.Width - newSize.Width) / 2,
                        (this.Height - newSize.Height) / 2,
                        newSize.Width,
                        newSize.Height);
                }
                else
                {
                    using (var attr = new ImageAttributes())
                    {
                        attr.SetGamma(0.2f);

                        g.DrawImage(
                            this.Image,
                            new Rectangle(
                                new Point(
                                    (int) ((this.Width - newSize.Width) / 2),
                                    (int) ((this.Height - newSize.Height) / 2)),
                                Size.Round(newSize)),
                            0,
                            0,
                            this.Image.Width,
                            this.Image.Height,
                            GraphicsUnit.Pixel,
                            attr);
                    }
                }
            }

            if (this.Text != null && this.Text.Length > 0)
            {
                var txtSize = g.MeasureString(this.Text, this.Font);
                using (var b = new SolidBrush(this.Enabled ? this.ForeColor : Color.DarkGray))
                {
                    g.DrawString(
                        this.Text,
                        this.Font,
                        b,
                        new Point(
                            (int) ((this.Width - txtSize.Width) / 2),
                            (int) ((this.Height - txtSize.Height) / 2)));
                }
            }

            //g.DrawRectangle(Pens.WhiteSmoke,new Rectangle(1,1,Width-2,Height-2));
            if (this.Enabled)
            {
                g.DrawRectangle(Pens.Gray, new Rectangle(0, 0, this.Width - 1, this.Height - 2));
            }
            else
            {
                g.DrawRectangle(
                    Pens.DarkGray,
                    new Rectangle(0, 0, this.Width - 1, this.Height - 2));
            }

            g.DrawLine(Pens.White, 0, this.Height - 1, this.Width, this.Height - 1);
        }
    }
}
