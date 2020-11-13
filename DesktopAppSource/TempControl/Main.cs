using System;
using System.Drawing;
using System.Windows.Forms;

namespace TempControl
{
    public partial class Main : Form
    {
        private readonly Form1 form1;
        public Main()
        {
            InitializeComponent();
            form1 = new Form1
            {
                main = this
            };
        }


        private void Main_Load(object sender, EventArgs e)
        {
            DesktopLocation = new Point(Screen.PrimaryScreen.Bounds.Width - 240, Screen.PrimaryScreen.Bounds.Height - 145);
            /*
            BeginInvoke(new MethodInvoker(delegate
            {
                Hide();
            }));
             */
        }

        private void label7_Click(object sender, EventArgs e)
        {
            form1.Show();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (Visible)
            {
                Visible = false;
            }
            else
            {
                Visible = true; ;
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            form1.timer1.Stop();
            form1.cpu.Close();
            form1.gpu.Close();
            if (form1.sp.IsOpen)
            {
                form1.sp.Close();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult cikis = new DialogResult();
            cikis = MessageBox.Show("Programı kapatmak, Fanların yüksek devirde çalışmasına sebep olacaktır. Gerçekten kapatmak istiyormusunuz!", "Uyarı", MessageBoxButtons.YesNo);
            if (cikis == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            form1.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void DrawGroupBox(GroupBox box, Graphics g, Color textColor, Color borderColor)
        {
            if (box != null)
            {
                Brush textBrush = new SolidBrush(textColor);
                Brush borderBrush = new SolidBrush(borderColor);
                Pen borderPen = new Pen(borderBrush);
                SizeF strSize = g.MeasureString(box.Text, box.Font);
                Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                               box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                               box.ClientRectangle.Width - 1,
                                               box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);
                g.Clear(this.BackColor);
                g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);
                g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
                g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
            }
        }


        private void groupBox1_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.Red, Color.Red);
        }

        private void groupBox2_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.Red, Color.Red);
        }

        private void groupBox3_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.Red, Color.Red);
        }

        private void groupBox6_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.Red, Color.Red);
        }

        private void groupBox5_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.Red, Color.Red);
        }

        private void groupBox4_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.Red, Color.Red);
        }
    }
}