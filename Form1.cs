using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO.Ports;
using ZedGraph;
using System.Reflection;
using System.IO;

namespace PIDMotorControl
{
    public partial class Form1 : Form
    {

        #region Declare the variables to be used in this program
        List<double> t = new List<double>();
        List<int> pu = new List<int>();
        private System.Windows.Forms.Timer aTimer;
        System.Media.SoundPlayer player = new System.Media.SoundPlayer("C:\\Users\\QUOCVU\\Downloads\\click.wav");

        byte[] temp = new byte[6];
        byte[] value = new byte[4];

        byte[] number = new byte[3];
        int m;
        float angle;
        float k;
        float p, i, d, sp;

        string spulse = String.Empty;
        double realtime = 0;
        double sreal_time = 0;
        int rtime = 0;
        int sample = 0;
        int count = 0;
        int tmp_pulses;
        int pulses;
        bool change = false;
        bool status = false;
        bool check = false;
        
        // Số lượng port COM đang khả dụng 
        int lenCom = 0;
        #endregion

        public Form1()
        {
            InitializeComponent();
            
            //loadGraphPane
            GraphPane graph1 = zedGraphControl1.GraphPane;
            graph1.Title.Text = "Position Realtime (DC Motor Servo)";
            graph1.XAxis.Title.Text = "Time (sec)";
            graph1.YAxis.Title.Text = "Pulse";

            RollingPointPairList list = new RollingPointPairList(60000);
            RollingPointPairList list0 = new RollingPointPairList(60000);
            LineItem curve = new LineItem("Pusle", list, Color.Red, SymbolType.None, 1.5f);
            LineItem curve0 = new LineItem("SetPoint", list0, Color.Blue, SymbolType.None, 1.5f);
            graph1.CurveList.Add(curve);
            graph1.CurveList.Add(curve0);
            graph1.XAxis.Scale.Min = 0;
            graph1.XAxis.Scale.Max = 100;
            graph1.XAxis.Scale.MinorStep = 25;
            graph1.XAxis.Scale.MajorStep = 50;
            graph1.YAxis.Scale.Min = 0;
            graph1.YAxis.Scale.Max = 150;
            graph1.AxisChange();

            GraphPane graph2 = zedGraphControl2.GraphPane;
            graph2.Title.Text = "Position Respone Record (DC Motor Servo)";
            graph2.XAxis.Title.Text = "Time (sec)";
            graph2.YAxis.Title.Text = "Pulse";

            RollingPointPairList list1 = new RollingPointPairList(60000);
            LineItem curve1 = new LineItem("Pusle", list1, Color.Green, SymbolType.None, 1.5f);
            graph2.CurveList.Add(curve1);
            graph2.XAxis.Scale.Min = 0;
            graph2.XAxis.Scale.Max = 0.5;
            graph2.XAxis.Scale.MinorStep = 0.125;
            graph2.XAxis.Scale.MajorStep = 0.25;
            graph2.YAxis.Scale.Min = 0;
            graph2.YAxis.Scale.Max = 150;
            graph2.AxisChange();

            string[] baudRate = { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" };
            comboBoxBaudRate.Items.AddRange(baudRate);
            string[] databits = { "6", "7", "8" };
            comboBoxDataBit.Items.AddRange(databits);
            string[] paritybits = { "None", "Odd", "Even" };
            comboBoxParityBit.Items.AddRange(paritybits);
            string[] stopbits = { "1", "1.5", "2" };
            comboBoxStopBit.Items.AddRange(stopbits);
        }

        void saveFile()
        {
            Assembly Assemb = Assembly.GetExecutingAssembly();
            Stream stream = Assemb.GetManifestResourceStream("click.wav");
            FileStream fs = new FileStream("C:\\Users\\QUOCVU\\Downloads\\click.wav", FileMode.CreateNew);
            BinaryReader br = new BinaryReader(stream);
            byte[] save = new byte[stream.Length];
            br.Read(save, 0, save.Length);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(save, 0, save.Length);
            bw.Flush();
            bw.Close();
        }

        #region Setup UART Box

        // Code chọn Baud Rate từ comboBox
        private void comboBoxBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            Com.BaudRate = Convert.ToInt32(comboBoxBaudRate.Text);
        }

        // Code chọn số bit data từ comboBox
        private void comboBoxDataBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            Com.DataBits = Convert.ToInt32(comboBoxDataBit.Text);
        }

        // Code chọn Parity bit từ comboBox
        private void comboBoxParityBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            switch (comboBoxParityBit.SelectedItem.ToString())
            {
                case "Odd":
                    Com.Parity = Parity.Odd;
                    break;
                case "Even":
                    Com.Parity = Parity.Even;
                    break;
                case "None":
                    Com.Parity = Parity.None;
                    break;
            }
        }

        // Code chọn Stop bit từ comboBox
        private void comboBoxStopBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Com.IsOpen) Com.Close();
            switch (comboBoxStopBit.SelectedItem.ToString())
            {
                case "1":
                    Com.StopBits = StopBits.One;
                    break;
                case "1.5":
                    Com.StopBits = StopBits.OnePointFive;
                    break;
                case "2":
                    Com.StopBits = StopBits.Two;
                    break;
            }
        }

        /*
         Cài đặt giá trị mặc định của các comboBox và textBox khi mở ứng dụng.
          
         baudRate = { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" };
         databits = { "6", "7", "8" };
         paritybits = { "None", "Odd", "Even" };
         stopbits = { "1", "1.5", "2" };
         */

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxBaudRate.SelectedIndex = 7;
            comboBoxDataBit.SelectedIndex = 2;
            comboBoxParityBit.SelectedIndex = 0;
            comboBoxStopBit.SelectedIndex = 0;
            this.AcceptButton = buttonCP;
        }
        #endregion


        #region Button Click Events

        // Nút bấm kết nối UART
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (labelStatus.Text == "Disconnected")
                {
                    Com.PortName = comboBoxSecCom.Text;
                    Com.Open();
                    Com.DiscardInBuffer(); // Xóa dữ liệu trong buffer
                    buttonConnect.ForeColor = Color.Red;
                    labelStatus.Text = "Connected";
                    labelStatus.ForeColor = Color.LimeGreen;
                    buttonConnect.Text = "Disconnect";
                }
                else
                {
                    Com.Close();
                    buttonConnect.ForeColor = Color.Lime;
                    labelStatus.Text = "Disconnected";
                    labelStatus.ForeColor = Color.Red;
                    buttonConnect.Text = "Connect";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonSetPoint_Click(object sender, EventArgs e)
        {
            try
            {
                if (buttonSetPoint.Text == "START")
                {
                    if (m != 0)
                    {
                        buttonSetPoint.Text = "STOP";
                        buttonSetPoint.BackColor = Color.Red;
                        status = true;
                    }
                    else
                    {
                        MessageBox.Show("Mode is not selected!");
                    }
                }
                else
                {
                    buttonSetPoint.Text = "START";
                    buttonSetPoint.BackColor = Color.Blue;
                    Com.DiscardInBuffer();
                    Com.DiscardOutBuffer();
                    ClearZedGraph();
                    resetValue();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonPID_Click(object sender, EventArgs e)
        {
            player.Play();
            float.TryParse(textBoxKP.Text, out p);
            float.TryParse(textBoxKI.Text, out i);
            float.TryParse(textBoxKD.Text, out d);

            string pid = "P " + p.ToString() + "\r\nI " + i.ToString() + "\r\nD " + d.ToString() + "\r\n";

            try
            {
                Com.Write(pid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonCP_Click(object sender, EventArgs e)
        {
            player.Play();
            ClearZedGraph2(); // Làm mới đồ thị graph2
            float.TryParse(textBoxSetPoint.Text, out sp);
            string s = "S " + sp.ToString();
            
            change = true;

            try
            {
                Com.Write(s);
                aTimer = new System.Windows.Forms.Timer();
                aTimer.Tick += new EventHandler(aTimer_Tick);
                aTimer.Interval = 10; // 10 milisecond
                rtime = 0;
                aTimer.Start();               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radio = sender as RadioButton;

            if (radio.Checked)
            {
                try
                {
                    m = 1;
                    string sM = "M " + m.ToString();
                    Com.Write(sM);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radio = sender as RadioButton;

            if (radio.Checked)
            {
                try
                {
                    m = 4;
                    string sM = "M " + m.ToString();
                    Com.Write(sM);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        #endregion

        #region Timer_Tick Event

        private void timer1_Tick(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames(); // Lấy tất cả các COM đang khả dụng trên PC
            if (lenCom != ports.Length)
            {
                lenCom = ports.Length;
                comboBoxSecCom.Items.Clear();
                for (int i = 0; i < lenCom; i++)
                {
                    comboBoxSecCom.Items.Add(ports[i]);
                }
                comboBoxSecCom.Text = ports[0];
            }
        }

        private void aTimer_Tick(object sender, EventArgs e)
        {
            if (check == true)
            {
                try
                {
                    Draw_Record();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        #endregion    

        #region Push data from MCU to PC via UART
        private void OnCom(object sender, SerialDataReceivedEventArgs e)
        {
            //Control.CheckForIllegalCrossThreadCalls = false;
            
            spulse = Com.ReadLine();

            if (status == true)
            {
                int.TryParse(spulse, out pulses);
                sample++;
                rtime++;

                // Khi có tín hiệu gửi setpoint mới:
                if (change == true)
                {
                    if (check == false)
                    {
                        t.Add(rtime);
                        pu.Add(pulses);
                    }
                    // Khi đủ 50 mẫu:
                    if (t.Count > 50)
                    {
                        check = true; // Dừng việc nạp buffer và bắt đầu vẽ đồ thị graph2
                    }
                }
                if (sample == 3)
                {
                    sreal_time++;
                    realtime = sreal_time / 10;
                    Data_ListView();
                    Draw();
                    if (m == 1)
                    {
                        if (pulses >= 0)
                            tmp_pulses = pulses;
                        else
                            tmp_pulses = pulses * (-1);

                        angle = (tmp_pulses * 360) / 32;
                        k = angle / 360;
                        if (k > 1)
                            angle = angle - (int)k * 360;
                    }
                    else if (m == 4)
                    {
                        if (pulses >= 0)
                            tmp_pulses = pulses;
                        else
                            tmp_pulses = pulses * (-1);
                        angle = (tmp_pulses * 360) / 128;
                        k = angle / 360;
                        if (k > 1)
                            angle = angle - (int)k * 360;
                    }

                    if(pulses >= 0)
                        textBoxAngle.Text = angle.ToString() + " Degrees";
                    else
                        textBoxAngle.Text = "-" + angle.ToString() + " Degrees";
                    sample = 0;
                }

            }

        }
        #endregion   

        #region ZedGraph
        private void Data_ListView()
        {
            if (status == false) return;
            else
            {
                ListViewItem item = new ListViewItem((realtime).ToString()); // Gán biến realtime vào cột đầu tiên của ListView
                item.SubItems.Add(spulse);
                listView1.Items.Add(item); //Gán biến pulses vào cột tiếp theo của ListView

                //Hiện thị dòng được gán gần nhất ở ListView (cuộn ListView theo dữ liệu gần nhất):
                listView1.Items[listView1.Items.Count - 1].EnsureVisible();
            }
        }

        // Vẽ đồ thị
        private void Draw()
        {
            if (zedGraphControl1.GraphPane.CurveList.Count <= 0)
                return;

            LineItem curve = zedGraphControl1.GraphPane.CurveList[0] as LineItem;
            LineItem curve0 = zedGraphControl1.GraphPane.CurveList[1] as LineItem;

            if (curve == null)
                return;
            if (curve0 == null)
                return;

            IPointListEdit list = curve.Points as IPointListEdit;
            IPointListEdit list0 = curve0.Points as IPointListEdit;

            if (list == null)
                return;
            if (list0 == null)
                return;

            list.Add(realtime, pulses); // Thêm điểm số xung thực trên đồ thị 
            list0.Add(realtime, (int)sp); // Thêm điểm set point trên đồ thị

            Scale xScale = zedGraphControl1.GraphPane.XAxis.Scale;
            Scale yScale = zedGraphControl1.GraphPane.YAxis.Scale;

            // Tự động Scale theo trục x
            if (realtime > xScale.Max - xScale.MajorStep)
            {
                xScale.Max = realtime + xScale.MajorStep;
                xScale.Min = xScale.Max - 100;
            }

            // Tự động Scale theo trục y
            if (pulses > yScale.Max - yScale.MajorStep)
            {
                yScale.Max = pulses + yScale.MajorStep;
            }
            else if (pulses < yScale.Min + yScale.MajorStep)
            {
                yScale.Min = pulses - yScale.MajorStep;
            }

            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
            zedGraphControl1.Refresh();
        }

        // Vẽ đồ thị record 0.5sec
        private void Draw_Record()
        {
            if (zedGraphControl2.GraphPane.CurveList.Count <= 0)
                return;

            LineItem curve1 = zedGraphControl2.GraphPane.CurveList[0] as LineItem;

            if (curve1 == null)
                return;

            IPointListEdit list1 = curve1.Points as IPointListEdit;

            if (list1 == null)
                return;
            double rt = t[count] / 100;
            list1.Add(rt, pu[count]); // Thêm điểm trên đồ thị
            count++;

            Scale xScale = zedGraphControl2.GraphPane.XAxis.Scale;
            Scale yScale = zedGraphControl2.GraphPane.YAxis.Scale;

            // Tự động Scale theo trục x
            if (rt > xScale.Max)
            {
                xScale.Max = rt + xScale.MajorStep;
                xScale.Min = xScale.Max - 0.5;
            }

            // Tự động Scale theo trục y
            if (pulses > yScale.Max - yScale.MajorStep)
            {
                yScale.Max = pulses + yScale.MajorStep;
            }
            else if (pulses < yScale.Min + yScale.MajorStep)
            {
                yScale.Min = pulses - yScale.MajorStep;
            }

            zedGraphControl2.AxisChange();
            zedGraphControl2.Invalidate();
            zedGraphControl2.Refresh();
            if (count == 50) // Vẽ xong đáp ứng trong 0.5s
            {
                t.Clear(); // Reset buffer thời gian
                pu.Clear(); // Reset buffer xung
                change = false; // Chờ tín hiệu mới từ Setpoint
                count = 0; // Reset index
                check = false; // Bắt đầu nạp lại buffer khi nhận được xung 
            }
        }

        private void textBoxSetPoint_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonCP_Click(sender, e);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }


        // Xóa đồ thị
        private void ClearZedGraph()
        {
            zedGraphControl1.GraphPane.CurveList.Clear(); // Xóa đường graph1
            zedGraphControl1.GraphPane.GraphObjList.Clear(); // Xóa đối tượng graph1

            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();

            zedGraphControl2.GraphPane.CurveList.Clear(); // Xóa đường graph2
            zedGraphControl2.GraphPane.GraphObjList.Clear(); // Xóa đối tượng graph2

            zedGraphControl2.AxisChange();
            zedGraphControl2.Invalidate();

            GraphPane graph1 = zedGraphControl1.GraphPane;
            graph1.Title.Text = "Position Realtime (DC Motor Servo)";
            graph1.XAxis.Title.Text = "Time (sec)";
            graph1.YAxis.Title.Text = "Pulse";

            RollingPointPairList list = new RollingPointPairList(60000);
            RollingPointPairList list0 = new RollingPointPairList(60000);
            LineItem curve = new LineItem("Pusle", list, Color.Red, SymbolType.None, 1.5f);
            LineItem curve0 = new LineItem("SetPoint", list0, Color.Blue, SymbolType.None, 1.5f);
            graph1.CurveList.Add(curve);
            graph1.CurveList.Add(curve0);
            graph1.XAxis.Scale.Min = 0;
            graph1.XAxis.Scale.Max = 100;
            graph1.XAxis.Scale.MinorStep = 25;
            graph1.XAxis.Scale.MajorStep = 50;
            graph1.YAxis.Scale.Min = 0;
            graph1.YAxis.Scale.Max = 150;
            graph1.AxisChange();

            GraphPane graph2 = zedGraphControl2.GraphPane;
            graph2.Title.Text = "Position Respone Record (DC Motor Servo)";
            graph2.XAxis.Title.Text = "Time (sec)";
            graph2.YAxis.Title.Text = "Pulse";

            RollingPointPairList list1 = new RollingPointPairList(60000);
            LineItem curve1 = new LineItem("Pusle", list1, Color.Green, SymbolType.None, 1.5f);
            graph2.CurveList.Add(curve1);
            graph2.XAxis.Scale.Min = 0;
            graph2.XAxis.Scale.Max = 0.5;
            graph2.XAxis.Scale.MinorStep = 0.125;
            graph2.XAxis.Scale.MajorStep = 0.25;
            graph2.YAxis.Scale.Min = 0;
            graph2.YAxis.Scale.Max = 150;
            graph2.AxisChange();
        }

        private void ClearZedGraph2()
        {
            zedGraphControl2.GraphPane.CurveList.Clear(); // Xóa đường graph2
            zedGraphControl2.GraphPane.GraphObjList.Clear(); // Xóa đối tượng graph2

            zedGraphControl2.AxisChange();
            zedGraphControl2.Invalidate();

            GraphPane graph2 = zedGraphControl2.GraphPane;
            graph2.Title.Text = "Position Respone Record (DC Motor Servo)";
            graph2.XAxis.Title.Text = "Time (sec)";
            graph2.YAxis.Title.Text = "Pulse";

            RollingPointPairList list1 = new RollingPointPairList(60000);
            LineItem curve1 = new LineItem("Pusle", list1, Color.Green, SymbolType.None, 1.5f);
            graph2.CurveList.Add(curve1);
            graph2.XAxis.Scale.Min = 0;
            graph2.XAxis.Scale.Max = 0.5;
            graph2.XAxis.Scale.MinorStep = 0.125;
            graph2.XAxis.Scale.MajorStep = 0.25;
            graph2.YAxis.Scale.Min = 0;
            graph2.YAxis.Scale.Max = 150;
            graph2.AxisChange();
        }

        #endregion

        // Hàm xóa dữ liệu (reset)
        private void resetValue()
        {
            realtime = 0;
            sreal_time = 0;
            pulses = 0;
            count = 0;
            sample = 0;
            textBoxKP.Text = textBoxKI.Text = textBoxKD.Text = String.Empty;
            textBoxAngle.Text = String.Empty;          
            textBoxSetPoint.Text = String.Empty;
            spulse = String.Empty;
            status = false;
            change = false;
            check = false;
            listView1.Items.Clear();
            t.DefaultIfEmpty();
            pu.DefaultIfEmpty();
        }
        
        
    }
}
