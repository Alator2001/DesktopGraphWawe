using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Net.Configuration;
using System.Diagnostics.Contracts;
using System.Data.SqlTypes;
using System.Reflection;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Windows.Markup;
using System.Globalization;
using Krypton.Toolkit;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Security.Cryptography;
using System.Net;


namespace WindowsFormsAppSH2
{

    public partial class Form1 : Form
    {
        MqttClient mqttClient;

        readonly string defpath = "D:\\old\\source\\diplom\\FormAppSH2\\data.txt";
        string path = "D:\\old\\source\\diplom\\FormAppSH2\\data.txt";
        string addPath = "D:\\old\\source\\diplom\\FormAppSH2\\data.txt";

        const int sizeBufferRecord = 256;

        bool filterFlag = false;
        bool isConnected = false;
        bool isConnectedMqtt = false;
        bool readingFlag = false;
        bool plotFlag = false;
        bool errorFlag = false;
        bool mouseMoveFlag = false;
        bool newMsgMqtt = false;

        string[] bufferLive = new string[sizeBufferRecord];
        string msgMqtt;

        private int _countBuffer = 0;
        private int _countSeconds = 0;

        double min1 = double.MaxValue;
        double max1 = double.MinValue;
        double min2 = double.MaxValue;
        double max2 = double.MinValue;


        public Form1()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void timer2_Tick(object sender, EventArgs e) // Список COM портов
        {
            if (isConnected && !serialPort1.IsOpen)
            {
                disconnectFromBoard();
                errorFlag = true;
                Invoke(new Action(() => callError()));
            }
            comboBox1.Items.Clear();
            // Получаем список COM портов доступных в системе
            string[] portnames = SerialPort.GetPortNames();
            foreach (string portName in portnames)
            {
                //добавляем доступные COM порты в список           
                comboBox1.Items.Add(portName);
                //Console.WriteLine(portnames.Length);
                if (portnames[0] != null)
                {
                    comboBox1.SelectedItem = portnames[0];
                }
            }
        }

        private void button2_Click(object sender, EventArgs e) // Статус подключения
        {
            if (isConnectedMqtt)
            {
                MessageBox.Show("Server connected");
            }
            else
            {
                if (!isConnected)
                {
                    сonnectToBoard();
                }
                else
                {
                    readingFlag = false;
                    disconnectFromBoard();
                }
            }
        }

        private void сonnectToBoard()
        {
            string selectedPort = comboBox1.GetItemText(comboBox1.SelectedItem);
            if (string.IsNullOrEmpty(selectedPort))
            {
                MessageBox.Show("Please select a COM port");
            }
            else
            {
                try
                {
                    isConnected = true;
                    serialPort1.PortName = selectedPort;
                    serialPort1.BaudRate = Convert.ToInt32(textBox1.Text);
                    serialPort1.StopBits = StopBits.One;
                    serialPort1.Open();
                    button2.Text = "Disconnect";
                    panel1.BackColor = Color.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error connecting to port: " + ex.Message);
                    isConnected = false;
                }
            }
        }

        private void disconnectFromBoard()
        {
            isConnected = false;
            serialPort1.Close();
            button2.Text = "Connect";
            panel1.BackColor = Color.Red;
        }

        private void button3_Click(object sender, EventArgs e) // Запись данных в файл
        {
            if (!isConnected && !isConnectedMqtt)
            {
                MessageBox.Show("Device not connected");
            }
            else if (isConnectedMqtt)
            {
                Thread toFileMqtt = new Thread(subClick);
                if (!readingFlag)
                {
                    toFileMqtt.Start();
                    button3.Text = "Stop writing to file...";
                }
                else
                {
                    readingFlag = false;
                    MessageBox.Show("File writing stopped");
                    button3.Text = "Write to file";
                }
            }
            else
            {
                Thread toFile = new Thread(readingClick);
                if (!readingFlag)
                {
                    toFile.Start();
                    button3.Text = "Stop writing to file...";
                }
                else
                {
                    readingFlag = false;
                    MessageBox.Show("File writing stopped");
                    button3.Text = "Write to file";
                }
            }
        }

        

        private void readingClick()
        {
            string msg;
            long fileSize = new FileInfo(defpath).Length;
            if (fileSize == 0)
            {
                msg = "Время[c]\tОборт/мин\tdelta[V2-V1]\tКрутящий момент[Нм]\tМощность\tЛ.С.\n";
                File.AppendAllText(defpath, msg);
            }
            readingFlag = true;
            msg = "|";
            File.AppendAllText(defpath, Environment.NewLine + msg + Environment.NewLine);
            _countBuffer = 0;
            while (readingFlag)
            {
                try
                {
                    msg = serialPort1.ReadLine();
                    int tTime = Convert.ToInt32(msg); 
                    if (tTime != 0)
                    {
                        if (filterFlag)
                        {
                            if (Convert.ToInt32(60 / (tTime * 0.001)) >= Convert.ToInt32(textBox16.Text) &&
                                Convert.ToInt32(60 / (tTime * 0.001)) <= Convert.ToInt32(textBox17.Text))
                            {
                                calculat(tTime);
                            }
                        }
                        else
                        {
                            calculat(tTime);
                        }
                    }
                    
                }
                catch
                {
                    continue;
                }
            }
        }

        private void subClick ()
        {
            string msg;
            long fileSize = new FileInfo(defpath).Length;
            if (fileSize == 0)
            {
                msg = "t[c]\tОб/мин\t[V2-V1]\tКр.м.\tP\tЛ.С.\n";
                File.AppendAllText(defpath, msg);
            }
            readingFlag = true;
            msg = "|";
            File.AppendAllText(defpath, Environment.NewLine + msg + Environment.NewLine);
            _countBuffer = 0;
            while (readingFlag)
            {
                if (newMsgMqtt)
                {

                    int tTime = Convert.ToInt32(msgMqtt);
                    if (tTime != 0)
                    {
                        newMsgMqtt = false;
                        if (filterFlag)
                        {
                            if (Convert.ToInt32(60 / (tTime * 0.001)) >= Convert.ToInt32(textBox16.Text) &&
                                Convert.ToInt32(60 / (tTime * 0.001)) <= Convert.ToInt32(textBox17.Text))
                            {
                                calculat(tTime);
                            }
                        }
                        else
                        {
                            calculat(tTime);
                        }
                    }
                }
                else
                { 
                    continue;
                }
            }
        }

        int V;
        double V_prev = 0;
        double deltaV;
        double M;
        double P;
        double LS;

        private void calculat(int tTime) 
        {
            string msg;
            V = Convert.ToInt32 (60 / (tTime * 0.001));
            deltaV = V - V_prev;
            V_prev = V;
            M = (deltaV * 0.065) / (tTime * 0.001);
            P = M * V * 0.105;
            LS = P / 735.49875;
            msg =   Convert.ToString(tTime) + "\t" +
                    Convert.ToString(V) + "\t" +
                    Convert.ToString(deltaV) + "\t" +
                    Convert.ToString(M) + "\t" +
                    Convert.ToString(P) + "\t" +
                    Convert.ToString(LS) + "\n";
            bufferLive[_countBuffer] = msg;
            _countBuffer++;
            if (_countBuffer == sizeBufferRecord)
            {
                _countBuffer = 0;
            }
            File.AppendAllText(defpath, msg);
        }


        private void callError()
        {
            readingFlag = false;
            MessageBox.Show("Device disconnect, file writing stopped");
            button3.Text = "Write to file";
        }


        private void button4_Click(object sender, EventArgs e) //Plot
        {
            this.chart1.Series[0].Points.Clear();
            this.chart1.Series[1].Points.Clear();
            chart1.MouseWheel += chart1_MouseWheel;
            chart1.ChartAreas[0].AxisX.Title = "Частота вращения коленвала, об/мин";
            chart1.ChartAreas[0].AxisY.Title = "Крутящий момент, Нм";
            chart1.ChartAreas[0].AxisY2.Title = "Мощность, Вт";
            chart1.ChartAreas[0].AxisY2.Enabled = AxisEnabled.True;
            //chart1.Series[2].YAxisType = AxisType.Secondary;
            checkBox1.Checked = true;
            checkBox2.Checked = true;
            string[] msg = File.ReadAllLines(path);
            int j = 0;
            for (int i = 1; i < msg.Length; i++)
            {
                if (msg[i] == "" || msg[i] == "|")
                {
                    continue;
                }
                else
                {
                    string[] values = msg[i].Split('\t');
                    if (values.Length > 2)
                    {
                        if (Convert.ToDouble(values[3]) > 0)
                        {
                            СomparePlot1(values[4]); // по мощности
                            this.chart1.Series[0].Points.AddXY(double.Parse(values[1]), double.Parse(values[3], CultureInfo.GetCultureInfo("fr-FR")));
                            this.chart1.Series[1].Points.AddXY(double.Parse(values[1]), double.Parse(values[4], CultureInfo.GetCultureInfo("fr-FR")));
                        }
                        j++;
                    }
                    j++;
                }
            }
            if (min1 != double.MaxValue && max1 != double.MinValue)
            {
                textBox33.Text ="min:" + min1 + " " + "max:" + max1;
            }
            //avr();
            mouseMoveFlag = true;

        }

        private void button5_Click(object sender, EventArgs e) // Live Plot
        {
            chart2.ChartAreas[0].AxisY.Title = "Частота вращения коленвала, об/мин";
            chart2.ChartAreas[0].AxisX.Title = "Время";
            chart2.ChartAreas[0].AxisX.LabelStyle.Format = "H:mm:ss";
            chart2.Series[0].XValueType = ChartValueType.DateTime;
            chart2.ChartAreas[0].AxisX.Minimum = DateTime.Now.ToOADate();
            chart2.ChartAreas[0].AxisX.Maximum = DateTime.Now.AddMinutes(1).ToOADate();
            chart2.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Seconds;
            chart2.ChartAreas[0].AxisX.Interval = 5;
            if (!plotFlag)
            {
                plotFlag = true;
                timer1.Enabled = true;
                button5.Text = "Stop";
            }
            else
            {
                plotFlag = false;
                timer1.Enabled = false;
                button5.Text = "Live Plot";
            }
            
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (errorFlag)
            {
                errorFlag = false;
                plotFlag = false;
                timer1.Enabled = false;
                button5.Text = "Live Plot";
                return;
            }
            DateTime timeNow = DateTime.Now;
            if (bufferLive[0] == null)
            {
                return;
            }
            if (_countBuffer == 0)
            {
                return;
            }
            else
            {
                string[] values = bufferLive[_countBuffer - 1].Split('\t');
                chart2.Series[0].Points.AddXY(timeNow, values[1]);
            }
            _countSeconds++;
            if (_countSeconds == 600)
            {
                _countSeconds = 0;
                chart2.ChartAreas[0].AxisX.Minimum = DateTime.Now.ToOADate();
                chart2.ChartAreas[0].AxisX.Maximum = DateTime.Now.AddMinutes(1).ToOADate();
                chart2.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Seconds;
                chart2.ChartAreas[0].AxisX.Interval = 5;
            }
        }



        private void button6_Click(object sender, EventArgs e) //Open File
        {
            openFileDialog1.Filter = "All files (*.*)|*.*|txt files (*.txt)|*.txt";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog()==DialogResult.OK)
            {
                path = openFileDialog1.FileName;
                textBox8.Text = "1: " + path;
            }
        }


        private void button7_Click(object sender, EventArgs e) // Save as
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                path = saveFileDialog1.FileName;
                string[] msg = File.ReadAllLines(defpath);
                int stopSession = msg.Length - 1;
                for (int i = msg.Length - 1; i >= 0; i--)
                {
                    if (msg[i]=="|")
                    {
                        stopSession = i;
                        break;
                    }
                }
                for (int i = stopSession + 1; i < msg.Length; i++)
                {
                    if (i == stopSession + 1)
                    {
                        string legend = "Время[c]\tОборт/мин\tdelta[V2-V1]\tКрутящий момент[Нм]\tМощность\tЛ.С.\n";
                        File.AppendAllText(path, legend);
                    }
                    File.AppendAllText(path, msg[i] + Environment.NewLine);
                }
            }
        }

        //private void avr() //Avarage
        //{
        //    try 
        //    { 
        //        Convert.ToUInt32(textBox3.Text); 
        //    }
        //    catch 
        //    { 
        //        MessageBox.Show("Please enter a valid value Size Window AVR");
        //        return;
        //    }
        //    uint sizeWindow = Convert.ToUInt32(textBox3.Text);
        //    this.chart1.Series[1].Points.Clear();
        //    double[] arrWindow = new double[sizeWindow];
        //    string[] msg = File.ReadAllLines(path);
        //    double[] msgs = new double[msg.Length];
        //    int k = 0;
        //    for (int i = 0; i < msg.Length; i++)
        //    {
        //        try
        //        {
        //            if (msg[i] == "|" || msg[i] == "")
        //            {
        //                continue;
        //            }
        //            string[] values = msg[i].Split('\t');
        //            msgs[k] = Convert.ToDouble(values[1]);
        //            k++;
        //        }
        //        catch
        //        {
        //            continue;
        //        }
        //    }
        //    for (int i = 0; i < k; i++)
        //    {
        //        for (int j = 0; j < sizeWindow; j++)
        //        {
        //            if (j == sizeWindow - 1)
        //            {
        //                arrWindow[j] = msgs[i];
        //            }
        //            else
        //            {
        //                arrWindow[j] = arrWindow[j + 1];
        //            }
        //        }
        //        double sum = 0;
        //        for (int j = 0; j < sizeWindow; j++)
        //        {
        //            sum += arrWindow[j] * arrWindow[j];
        //        }
        //        double avr = Math.Sqrt(sum / sizeWindow);
        //        this.chart1.Series[1].Points.AddXY(i, avr);
        //    }

        //}


        private void СomparePlot1(string msg) //Сompare first
        {
            double msgs = double.Parse(msg, CultureInfo.GetCultureInfo("fr-FR"));
            if (msgs>max1)
            {
                max1 = msgs;
            }
            if (msgs<min1)
            {
                min1 = msgs;
            }
        }

        private void СomparePlot2(string msg) //Сompare second
        {
            double msgs = double.Parse(msg, CultureInfo.GetCultureInfo("fr-FR"));
            if (msgs > max2)
            {
                max2 = msgs;
            }
            if (msgs < min2)
            {
                min2 = msgs;
            }
        }

        private void button1_Click(object sender, EventArgs e) //Clear all data
        {
            File.WriteAllText(defpath, string.Empty);
            min1 = double.MaxValue;
            max1 = double.MinValue;
            mouseMoveFlag = false;
        }

        private void button8_Click(object sender, EventArgs e) //Full Screen
        {
            Form2 formF = new Form2();
            formF.WindowState = FormWindowState.Normal;
            //formF.chart1.Dock = DockStyle.Fill;
            formF.Show();  
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)  //Сoordinates
        {
            if (mouseMoveFlag)
            {
                chart1.Invalidate();
                ChartArea chartArea = chart1.ChartAreas[0];
                double xValue = chartArea.AxisX.PixelPositionToValue(e.Location.X);
                double yValue = chartArea.AxisY.PixelPositionToValue(e.Location.Y);
                double y2Value = chartArea.AxisY2.PixelPositionToValue(e.Location.Y);
                textBox7.Text = $"X:{xValue.ToString("0")} Y:{yValue.ToString("0")} Z:{y2Value.ToString("0")}";
            }

        }

        private void chart1_MouseWheel(object sender, MouseEventArgs e) //Zoom scrol mouse
        {
            try
            {
                double zoomFactor = 2; // Задайте желаемый множитель зума
                if (e.Delta < 0) // Если колесико мыши прокручено вниз
                {
                    chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                    chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset();
                    chart1.ChartAreas[0].AxisY2.ScaleView.ZoomReset();
                }
                else if (e.Delta > 0) // Если колесико мыши прокручено вверх
                {
                    double xMin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                    double xMax = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum;
                    double yMin = chart1.ChartAreas[0].AxisY.ScaleView.ViewMinimum;
                    double yMax = chart1.ChartAreas[0].AxisY.ScaleView.ViewMaximum;
                    double y2Min = chart1.ChartAreas[0].AxisY2.ScaleView.ViewMinimum;
                    double y2Max = chart1.ChartAreas[0].AxisY2.ScaleView.ViewMaximum;
                    double posXStart = chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) - (xMax - xMin) / (2 * zoomFactor);
                    double posXFinish = chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) + (xMax - xMin) / (2 * zoomFactor);
                    double posYStart = chart1.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y) - (yMax - yMin) / (2 * zoomFactor);
                    double posYFinish = chart1.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y) + (yMax - yMin) / (2 * zoomFactor);
                    double posY2Start = chart1.ChartAreas[0].AxisY2.PixelPositionToValue(e.Location.Y) - (y2Max - y2Min) / (2 * zoomFactor);
                    double posY2Finish = chart1.ChartAreas[0].AxisY2.PixelPositionToValue(e.Location.Y) + (y2Max - y2Min) / (2 * zoomFactor);
                    chart1.ChartAreas[0].AxisX.ScaleView.Zoom(posXStart, posXFinish);
                    chart1.ChartAreas[0].AxisY.ScaleView.Zoom(posYStart, posYFinish);
                    chart1.ChartAreas[0].AxisY2.ScaleView.Zoom(posYStart, posYFinish);
                }
            }
            catch { }
        }

        private void button11_Click(object sender, EventArgs e) //Filter
        {
            if (filterFlag)
            {
                filterFlag = false;
                button11.Text = "OFF";
            }
            else
            {
                filterFlag = true;
                button11.Text = "ON";
            }
        }

        private void button10_Click(object sender, EventArgs e) //Open second file
        {
            openFileDialog1.Filter = "All files (*.*)|*.*|txt files (*.txt)|*.txt";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                addPath = openFileDialog1.FileName;
                textBox9.Text = "2: " + addPath;
            }
            else return;
        }

        private void button14_Click(object sender, EventArgs e) //Plot second
        {
            checkBox3.Checked = true;
            checkBox4.Checked = true;
            this.chart1.Series[2].Points.Clear();
            this.chart1.Series[3].Points.Clear();
            string[] msg = File.ReadAllLines(addPath);
            for (int i = 1; i < msg.Length; i++)
            {
                if (msg[i] == "" || msg[i] == "|")
                {
                    continue;
                }
                else
                {
                    string[] values = msg[i].Split('\t');
                    if (values.Length > 2)
                    {
                        if (Convert.ToDouble(values[3]) > 0)
                        {
                            СomparePlot2(values[4]);
                            this.chart1.Series[2].Points.AddXY(double.Parse(values[1]), double.Parse(values[3], CultureInfo.GetCultureInfo("fr-FR")));
                            this.chart1.Series[3].Points.AddXY(double.Parse(values[1]), double.Parse(values[4], CultureInfo.GetCultureInfo("fr-FR")));
                        }
                    }
                }
            }
            if (min2 != double.MaxValue && max2 != double.MinValue)
            {
                textBox34.Text = "min:" + min2 + " " + "max:" + max2;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                this.chart1.Series[0].Enabled = true;
            }
            else
            {
                this.chart1.Series[0].Enabled = false;

            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
                if (checkBox2.Checked)
                {
                    this.chart1.Series[1].Enabled = true;
                }
                else
                {
                    this.chart1.Series[1].Enabled = false;

                }
            }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            
                if (checkBox3.Checked)
                {
                    this.chart1.Series[2].Enabled = true;
                }
                else
                {
                    this.chart1.Series[2].Enabled = false;

                }
            }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                this.chart1.Series[3].Enabled = true;
            }
            else
            {
                this.chart1.Series[3].Enabled = false;

            }
        }

        private void button12_Click(object sender, EventArgs e) // Connect MQTT
        {
            if (isConnected)
            {
                MessageBox.Show("COM connected");
            }
            if (!isConnectedMqtt)
            {
                сonnectToMqtt();
            }
            else
            {
                readingFlag = false;
                disconnectFromMqtt();
            }
        }

        private void сonnectToMqtt ()
        {
            string port = textBox13.Text;
            string ip_address = textBox15.Text;
            string user_id = textBox21.Text;
            string login = textBox25.Text;
            string password = textBox26.Text;
            string helloMsg = "Connected " + user_id;
            try
            {
                mqttClient = new MqttClient(ip_address, Convert.ToInt32(port), false, null, null, MqttSslProtocols.None);
                mqttClient.Connect(user_id, login, password);
                //mqttClient.IsConnected;
                mqttClient.Publish("mbed-sample", Encoding.UTF8.GetBytes(helloMsg));
                mqttClient.MqttMsgPublishReceived += OnMqttMessageReceived;
                mqttClient.Subscribe(new string[] { "mbed-sample" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

                isConnectedMqtt = true;
                panel2.BackColor = Color.Green;
                button12.Text = "Disconnect";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to port: " + ex.Message);
                isConnectedMqtt = false;
            }
        }

        private void disconnectFromMqtt()
        {
            string user_id = textBox21.Text;
            string byeMsg = "Disconnect " + user_id;
            mqttClient.Publish("mbed-sample", Encoding.UTF8.GetBytes(byeMsg));
            mqttClient.Disconnect();
            isConnectedMqtt = false;
            panel2.BackColor = Color.Red;
            button12.Text = "Connect";
        }

        void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            msgMqtt = Encoding.UTF8.GetString(e.Message);
            newMsgMqtt = true;
        }

        private void button9_Click(object sender, EventArgs e) //Start engine
        {
            if (isConnectedMqtt)
            {
                string startEngineMsg = "Start engine";
                mqttClient.Publish("mbed-sample", Encoding.UTF8.GetBytes(startEngineMsg));
            }
            else
            {
                MessageBox.Show("Server not connected");
            }
        }
    }
}
 






