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
            DesktopLocation = new Point(Screen.PrimaryScreen.Bounds.Width - 260, Screen.PrimaryScreen.Bounds.Height - 160);
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

        private void label6_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            if (form1.sp.IsOpen)
            {
                label2.Text = "No Connection";
                form1.timer1.Stop();
                form1.sp.Close();
            }
            else
            {
                form1.timer2.Start();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
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

    }
}
