using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShortNotes
{
    public class nTabControl : TabControl
    {
        public nTabControl()
        {
            /* Needed for DrawNItem
            DrawMode = TabDrawMode.OwnerDrawFixed;
            this.DrawItem += DrawNItem;
            */
        }


        //Remove Padding by https://stackoverflow.com/questions/10018458/c-sharp-remove-padding-from-tabpages-in-windows-forms
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x1300 + 40)
            {
                RECT rc = (RECT)m.GetLParam(typeof(RECT));
                rc.Left -= 7;
                rc.Right += 7;
                rc.Top -= 1; //2
                rc.Bottom += 7;
                Marshal.StructureToPtr(rc, m.LParam, true);
            }
            base.WndProc(ref m);
        }
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        /* DrawNItem
        private void DrawNItem(object sender, DrawItemEventArgs e)
        {
            //Draw BackgroundColor
            
            using (Graphics g = this.CreateGraphics())
            {
                BufferedGraphicsContext currentContext;
                BufferedGraphics myBuffer;
                currentContext = BufferedGraphicsManager.Current;
                myBuffer = currentContext.Allocate(g,
                    this.ClientRectangle);
                using (Brush br = new SolidBrush(Color.Black))
                {
                    myBuffer.Graphics.FillRectangle(br, ClientRectangle);
                }
                myBuffer.Render();
                myBuffer.Dispose();
            }
            

            //Draw Tabs 
            
            using (Brush br = new SolidBrush(Color.FromArgb(28, 28, 28)))
            {
                e.Graphics.FillRectangle(br, e.Bounds);
                SizeF sz = e.Graphics.MeasureString(this.TabPages[e.Index].Text, e.Font);
                if (Controls.IndexOf(SelectedTab) == e.Index)
                    e.Graphics.DrawString(this.TabPages[e.Index].Text, new Font(e.Font, FontStyle.Bold), Brushes.White, e.Bounds.Left + (e.Bounds.Width - sz.Width) / 2, e.Bounds.Top + (e.Bounds.Height - sz.Height) / 2 + 1);
                else 
                    e.Graphics.DrawString(this.TabPages[e.Index].Text, new Font(e.Font, FontStyle.Regular), Brushes.White, e.Bounds.Left + (e.Bounds.Width - sz.Width) / 2, e.Bounds.Top + (e.Bounds.Height - sz.Height) / 2 + 1);

                Rectangle rect = e.Bounds;
                rect.Offset(0, 1);
                rect.Inflate(0, -1);
                e.Graphics.DrawRectangle(Pens.DarkGray, rect);
                e.DrawFocusRectangle();
            }
        }*/
    }
}
