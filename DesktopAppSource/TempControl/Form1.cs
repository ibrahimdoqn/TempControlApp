using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;


namespace TempControl
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            cpu.Open();
            gpu.Open();
            timer1.Start();
            timer2.Start();
            timer3.Start();
            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            serialCOM();
        }

        private void boardConfig()
        {
            if (sp.IsOpen)
            {
                byte[] dataArray = new byte[] { 253 };
                sp.Write(dataArray, 0, 1);
            }

        }

        //Tanımlamalar
        public Computer cpu = new Computer() { CPUEnabled = true };
        public Computer gpu = new Computer() { GPUEnabled = true };
        public SerialPort sp;
        public Main main;
        private readonly PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        //Portlar
        private string[] portlar;

        //Değişkenler
        private readonly List<int> tempcpu = new List<int>();
        private readonly List<int> ClockCpu = new List<int>();
        private int usageGpu;
        private int maxUsageGpu;
        private int ClockGpu;
        private int cpuPower;
        private int maxCpuPower;
        private int gpuMemoryClock;
        private int gpuMemoryUsed;
        private byte tempgpu = 0;
        private byte maxTempCpu = 0;
        private byte maxTempGpu = 0;
        private byte minTempCpu = 110;
        private byte minTempGpu = 110;
        private float maxCpuUsage = 0;
        private byte CpuUsage = 0;
        private int dakika = 0;
        private int saat = 0;
        bool spOpen = false;


        //
        // Bu Alanda Form fonksiyoları tanımlanır
        //
        private void timer1_Tick(object sender, EventArgs e)
        {
            HardwareMonitor();//Sıcaklık hesaplama
            MinMaxTemp();//Label'a sıcaklıkları yazdır
            TempControl();//Sıcaklık Kontrolü / Pc kapatma
            if (spOpen) fanControl();//Seriport ile kullanım değerini denetleyici kart'a gönderir
        }

        private void SerialButton(bool Switch)
        {
            if (Switch)
            {
                sp.Close();
                label28.Text = "No Connection";
                label12.Text = "Count : -";
                spOpen = false;
                button7.Text = "Connect";
                button7.ForeColor = Color.Red;
            }
            else
            {
                timer2.Start();
                button7.Text = "Disconnect";
                button7.ForeColor = Color.GreenYellow;
            }
        }

        private bool SpDataBegin = false;
        private bool SpRoleCount = false;
        private bool tempData = false;
        private bool confData = false;
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!sp.IsOpen) return;
            while (sp.BytesToRead > 1)
            {
                if (!SpDataBegin)
                {
                    int okunan = sp.ReadByte();
                    switch (okunan)
                    {
                        case 208: { SpDataBegin = true; tempData = true; } break;
                        case 202: label1.Text = "12 V"; break;
                        case 203: label1.Text = "5 V"; break;
                        case 201: { SpDataBegin = true; SpRoleCount = true; } break;
                        case 205: turboMode(true); break;
                        case 206: turboMode(false); break;
                        case 200: boardConfig(); break;
                        case 207: { SpDataBegin = true; confData = true; } break;
                        default: break;
                    }
                }
                else
                {

                    if (tempData)
                    {
                        int okunan = sp.ReadByte();
                        label2.Text = okunan.ToString() + " °C";
                        SpDataBegin = false;
                        tempData = false;
                    }
                    else if (SpRoleCount)
                    {
                        string RoleCount = sp.ReadLine();
                        if (RoleCount != "")
                        {
                            label12.Text = "Count : " + RoleCount;
                            SpDataBegin = false;
                            SpRoleCount = false;
                        }
                    }
                    else if (confData)
                    {
                        byte[] SerialDataBuffer = new byte[6];
                        sp.Read(SerialDataBuffer, 0, 6);
                        label24.Text = SerialDataBuffer[0].ToString();
                        label25.Text = SerialDataBuffer[1].ToString();
                        label20.Text = SerialDataBuffer[2].ToString();
                        label21.Text = SerialDataBuffer[3].ToString();
                        label11.Text = SerialDataBuffer[4].ToString();
                        label10.Text = SerialDataBuffer[5].ToString();
                        SpDataBegin = false;
                        confData = false;
                        byte[] dataArray = new byte[] { 248 };
                        sp.Write(dataArray, 0, 1);
                    }
                }
            }

        }

        private void turboMode(bool Switch)
        {
            if (sp.IsOpen)
            {
                if (Switch)
                {
                    byte[] dataArray = new byte[] { 251 };
                    sp.Write(dataArray, 0, 1);
                    button4.ForeColor = Color.GreenYellow;
                    button5.ForeColor = Color.Red;
                }
                else
                {
                    byte[] dataArray = new byte[] { 250 };
                    sp.Write(dataArray, 0, 1);
                    button5.ForeColor = Color.GreenYellow;
                    button4.ForeColor = Color.Red;
                }
            }
        }


        private void timer3_Tick(object sender, EventArgs e)
        {
            //Hizmet süresini hesaplar
            dakika++;
            if (dakika == 60)
            {
                saat++;
                dakika = 0;
            }
            label17.Text = saat.ToString() + " Saat " + dakika.ToString() + " Dakika";
            label17.Location.Offset(groupBox11.Location.X / 2, groupBox11.Location.Y / 2);
        }

        //
        // Bu Alanda Form fonksiyoları tanımlanır
        //

        //Fonksiyonlar

        //Fan Control
        private void fanControl()
        {
            if (sp.IsOpen)
            {
                byte[] dataArray = new byte[] { 255, Convert.ToByte(tempcpu.Max()), tempgpu, 254 };
                sp.Write(dataArray, 0, 4);
            }
        }

        //Uyku modu tespiti
        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    {
                        timer2.Start();
                        timer3.Start();
                    }
                    break;
                case PowerModes.Suspend:
                    {
                        timer3.Stop();
                        spOpen = false;
                        sp.Close();
                    }
                    break;
            }
        }

        //Seriport bağlantısını açar
        public void serialCOM()
        {
            portlar = SerialPort.GetPortNames();
            foreach (string portAdi in portlar)
            {
                try
                {
                    sp = new SerialPort(portAdi, 19200, Parity.Odd, 8, StopBits.One)
                    {
                        ReadTimeout = 1000,
                        WriteTimeout = 1000

                    };
                    sp.Open();
                    byte[] SerialDataBuffer = new byte[] { 252 };
                    sp.Write(SerialDataBuffer, 0, 1);
                    SerialDataBuffer = new byte[1];
                    sp.Read(SerialDataBuffer, 0, 1);
                    if (SerialDataBuffer[0] == 252)
                    {
                        label28.Text = portAdi + " Bağlandı";
                        sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                        boardConfig();
                        button7.Text = "Disconnect";
                        button7.ForeColor = Color.GreenYellow;
                        spOpen = true;
                        timer2.Stop();
                        break;
                    }
                }
                catch
                {
                    if (sp.IsOpen)
                    {
                        spOpen = false;
                        sp.Close();
                    }
                }
            }
            if (!spOpen)
            {
                timer2.Start();
            }
        }
        //OpenHardware Monitor Sıcaklık hesaplama
        private void HardwareMonitor()//Sıcaklık Hesaplama Bölümü
        {

            tempcpu.Clear();
            ClockCpu.Clear();
            //CPU Sıcaklık
            foreach (IHardware hardware in cpu.Hardware)
            {
                hardware.Update();
                foreach (ISensor sensor in hardware.Sensors)
                {

                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        tempcpu.Add(Convert.ToInt32(sensor.Value));
                    }

                    else if (sensor.SensorType == SensorType.Clock)
                    {
                        if (sensor.Name.Substring(0, 3) == "CPU")
                        {
                            ClockCpu.Add(Convert.ToInt32(sensor.Value));
                        }
                    }

                    else if (sensor.SensorType == SensorType.Power)
                    {
                        if (sensor.Name == "CPU Package")
                        {
                            cpuPower = Convert.ToInt32(sensor.Value);
                        }
                    }

                    else if (sensor.SensorType == SensorType.Load)
                    {
                        if (sensor.Name == "CPU Total")
                        {
                            CpuUsage = Convert.ToByte(sensor.Value);
                        }
                    }
                }
            }
            //Grafik kartı Sıcaklık
            foreach (IHardware hardware in gpu.Hardware)
            {
                hardware.Update();

                foreach (ISensor sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        tempgpu = Convert.ToByte(sensor.Value);
                    }
                    else if (sensor.SensorType == SensorType.Clock)
                    {
                        if (sensor.Name == "GPU Core")
                        {
                            ClockGpu = Convert.ToInt32(sensor.Value);
                        }
                        else if (sensor.Name == "GPU Memory")
                        {
                            gpuMemoryClock = Convert.ToInt32(sensor.Value);
                        }

                    }
                    else if (sensor.SensorType == SensorType.Load)
                    {
                        if (sensor.Name == "GPU Core")
                        {
                            usageGpu = Convert.ToInt32(sensor.Value);
                        }
                        else if (sensor.Name == "GPU Memory")
                        {
                            gpuMemoryUsed = Convert.ToInt32(sensor.Value);
                        }
                    }

                }
            }

        }
        //Fan kontrol ve kritik sıcaklık bölümü
        private byte tempTimerGpu = 0;
        private byte tempTimerCpu = 0;
        private void TempControl()
        {

            if (usageGpu >= 60 && tempgpu > Convert.ToInt32(label10.Text))
            {
                //Ekran kartı kritik sıcaklık
                writeLog("Ekran kartı Sistem Yükte Max sıcaklık aşımı");
                sleepMode();
            }

            else if (usageGpu < 60 && tempgpu > Convert.ToInt32(label21.Text))
            {
                tempTimerGpu++;
                if (tempTimerGpu > 5)
                {
                    //Ekran kartı kritik sıcaklık
                    writeLog("Ekran kartı Sistem Bosta Max sıcaklık aşımı");
                    tempTimerGpu = 0;
                    sleepMode();
                }
            }
            else tempTimerGpu = 0;

            if (cpuPower > 80 && tempcpu.Max() > Convert.ToInt32(label11.Text))
            {
                //CPU kritik sıcaklık
                writeLog("İşlemci Sistem Yükte Max Sıcaklık Aşımı");
                sleepMode();
            }
            else if (cpuPower < 80 && tempcpu.Max() > Convert.ToInt32(label20.Text))
            {
                tempTimerCpu++;
                if (tempTimerCpu > 5)
                {
                    //CPU kritik sıcaklık
                    writeLog("İşlemci Sistem Bosta Max Sıcaklık Aşımı");
                    tempTimerCpu = 0;
                    sleepMode();
                }
            }
            else tempTimerCpu = 0;

        }
        //Uyku modu Fonksiyonu
        private void sleepMode()
        {
            Application.SetSuspendState(PowerState.Suspend, false, false);
            Show();
            MessageBox.Show("Bilgisayarınız hedeflenen sıcaklığı geçtiği için uyku moduna alındı...");
        }
        //Formdaki sıcaklıkları gerekli alanlara ekler
        private void MinMaxTemp()
        {
            //Sıcaklıklar
            label35.Text = tempcpu.Max().ToString() + " °C";
            label48.Text = tempgpu.ToString() + " °C";

            //Clock
            label9.Text = Convert.ToInt32(ClockCpu.Max()).ToString() + " MHz";
            label49.Text = ClockGpu.ToString() + " MHz";

            //Load
            label36.Text = "% " + CpuUsage.ToString();
            label47.Text = "% " + usageGpu.ToString();

            //Power
            label40.Text = cpuPower.ToString() + " Watt";
            if (cpuPower > maxCpuPower)
            {
                maxCpuPower = cpuPower;
                label41.Text = maxCpuPower.ToString() + " Watt";
            }

            //Max Load
            if (CpuUsage > maxCpuUsage)
            {
                maxCpuUsage = CpuUsage;
                label37.Text = "% " + CpuUsage.ToString();
            }
            if (usageGpu > maxUsageGpu)
            {
                maxUsageGpu = usageGpu;
                label46.Text = "% " + maxUsageGpu.ToString();
            }

            //Max sıcaklıklar
            if (maxTempCpu < tempcpu.Max() && tempcpu.Max() < 500)
            {
                maxTempCpu = Convert.ToByte(tempcpu.Max());
                label39.Text = maxTempCpu.ToString() + " °C";
            }
            if (maxTempGpu < tempgpu)
            {
                maxTempGpu = tempgpu;
                label44.Text = maxTempGpu.ToString() + " °C";
            }
            //Min Sıcaklıklar
            if (minTempCpu > tempcpu.Min())
            {
                minTempCpu = Convert.ToByte(tempcpu.Min());
                label38.Text = minTempCpu.ToString() + " °C";
            }
            if (minTempGpu > tempgpu)
            {
                minTempGpu = tempgpu;
                label45.Text = minTempGpu.ToString() + " °C";
            }
            //GPU MEMORY
            label42.Text = "% " + gpuMemoryUsed.ToString();
            label43.Text = gpuMemoryClock.ToString() + " MHz";

            // Sıcaklık Bildirim
            main.label1.Text = "% " + CpuUsage.ToString();
            main.label2.Text = tempcpu.Max().ToString() + " °C";
            main.label3.Text = (ClockCpu.Max()).ToString() + " MHz";
            main.label5.Text = tempgpu.ToString() + " °C";
            main.label6.Text = "% " + usageGpu.ToString();
            main.label7.Text = ClockGpu.ToString() + " MHz";
        }

        //Formdaki veriler değiştirildiğinde Board hafızasına kayıt eder
        private void yaz()
        {
            byte[] dataArray = new byte[] { 255, Convert.ToByte(label24.Text), Convert.ToByte(label25.Text), Convert.ToByte(label20.Text), Convert.ToByte(label21.Text), Convert.ToByte(label11.Text), Convert.ToByte(label10.Text), 249 };
            sp.Write(dataArray, 0, 8);
        }

        //Log bölümü
        private void writeLog(string logType)
        {
            string fileName = @"C:\TempControl\Logs.txt";
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Close();
            File.AppendAllText(fileName, Environment.NewLine + "*********LOG BEGIN*********");
            File.AppendAllText(fileName, Environment.NewLine + "Log tipi...: " + logType);
            File.AppendAllText(fileName, Environment.NewLine + "Tarih Saat...: " + DateTime.Now.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Cpu Kullanımı...: %" + CpuUsage.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Cpu Saat Hızı...: " + ClockCpu.Max().ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Max Cpu Kullanımı...: %" + maxCpuUsage.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Cpu Sıcaklığı...: " + tempcpu.Max().ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Cpu Power...: " + cpuPower.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Max Cpu Power...: " + maxCpuPower.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Min Cpu Sıcaklığı...: " + minTempCpu.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Max Cpu Sıcaklığı...: " + maxTempCpu.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Gpu Sıcaklığı...: " + tempgpu.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Min Gpu Sıcaklığı...: " + minTempGpu.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Max Gpu Sıcaklığı...: " + maxTempGpu.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Gpu Saat Hızı...: " + ClockGpu.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Gpu Kullanımı...: %" + usageGpu.ToString());
            File.AppendAllText(fileName, Environment.NewLine + "Sistem saati...: " + saat + " Saat " + dakika + " Dakika");
            File.AppendAllText(fileName, Environment.NewLine + "**********LOG END**********");

        }

        private string buttonDownUp(string labelDest, string DownUp, int min, int max)
        {
            int Number = Convert.ToInt32(labelDest);
            if (DownUp == "+")
            {
                if (Number < max)
                {
                    Number++;
                }
            }
            else if (DownUp == "-")
            {
                if (Number > min)
                {
                    Number--;
                }
            }
            return Number.ToString();

        }

        //
        //Bu alanda Form araçlarının görevleri tanımlanır.
        //

        private void button1_Click(object sender, EventArgs e)
        {
            Hide();
        }

        public static void CopyAll(DirectoryInfo kaynak, DirectoryInfo hedef)
        {
            Directory.CreateDirectory(hedef.FullName);
            //Dosyaları Yeni Dizine Kopyalıyoruz.
            foreach (FileInfo fi in kaynak.GetFiles())
            {
                Console.WriteLine(@"Kopyalanan : {0} \ {1}", hedef.FullName, fi.Name);
                fi.CopyTo(Path.Combine(hedef.FullName, fi.Name), true);
            }
            //Özyineli (recursive) Fonksiyon Kullanarak Alt Dizinleri Kopyalıyoruz.
            foreach (DirectoryInfo diKaynakAltDizin in kaynak.GetDirectories())
            {
                DirectoryInfo sonrakiHedefAltDizin =
                    hedef.CreateSubdirectory(diKaynakAltDizin.Name);
                CopyAll(diKaynakAltDizin, sonrakiHedefAltDizin);
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            string ExeDosyaYolu = @Application.StartupPath.ToString();
            string DestFolder = @"C:\TempControl";
            DirectoryInfo diKaynak = new DirectoryInfo(ExeDosyaYolu);
            DirectoryInfo diHedef = new DirectoryInfo(DestFolder);
            CopyAll(diKaynak, diHedef);
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Sıcaklık ve Devir Kontrol";
                td.Settings.Priority = System.Diagnostics.ProcessPriorityClass.High;
                td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                td.Principal.RunLevel = TaskRunLevel.Highest;
                LogonTrigger lt = new LogonTrigger();
                td.Triggers.Add(lt);
                td.Actions.Add(new ExecAction("C:\\TempControl\\TempControl.exe"));
                ts.RootFolder.RegisterTaskDefinition("TempControl", td);
            }
            MessageBox.Show("Uygulama oturum açıldığında otomatik çalıştırılacak şekilde ayarlandı.");
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

        private void button3_Click(object sender, EventArgs e)
        {
            writeLog("Kullanıcı isteği ile alınan kayıt");
        }

        private bool formTasiniyor = false;
        private Point baslangicNoktasi = new Point(0, 0);

        private void label6_MouseDown(object sender, MouseEventArgs e)
        {
            formTasiniyor = true;
            baslangicNoktasi = new Point(e.X, e.Y);
        }

        private void label6_MouseUp(object sender, MouseEventArgs e)
        {
            formTasiniyor = false;
        }

        private void label6_MouseMove(object sender, MouseEventArgs e)
        {
            if (formTasiniyor)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - baslangicNoktasi.X, p.Y - baslangicNoktasi.Y);
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            formTasiniyor = true;
            baslangicNoktasi = new Point(e.X, e.Y);
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            formTasiniyor = false;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (formTasiniyor)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - baslangicNoktasi.X, p.Y - baslangicNoktasi.Y);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            label20.Text = buttonDownUp(label20.Text, "+", 60, 78);
            yaz();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            label20.Text = buttonDownUp(label20.Text, "-", 60, 78);
            yaz();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            label21.Text = buttonDownUp(label21.Text, "-", 60, 78);
            yaz();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            label21.Text = buttonDownUp(label21.Text, "+", 60, 78);
            yaz();
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            label11.Text = buttonDownUp(label11.Text, "-", 78, 90);
            yaz();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            label11.Text = buttonDownUp(label11.Text, "+", 78, 90);
            yaz();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            label10.Text = buttonDownUp(label10.Text, "-", 78, 90);
            yaz();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            label10.Text = buttonDownUp(label10.Text, "+", 78, 90);
            yaz();
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            label25.Text = buttonDownUp(label25.Text, "+", 30, 62);
            yaz();
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            label25.Text = buttonDownUp(label25.Text, "-", 30, 62);
            yaz();
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            label24.Text = buttonDownUp(label24.Text, "-", 0, 255);
            yaz();
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            label24.Text = buttonDownUp(label24.Text, "+", 0, 255);
            yaz();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            turboMode(true);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            turboMode(false);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (sp.IsOpen)
            {
                SerialButton(true);

            }
            else
            {
                SerialButton(false);
            }
        }

        private void groupBoxMain_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.White, Color.YellowGreen);
        }

        private void groupBoxControl_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.White, Color.Red);
        }

    }
}
