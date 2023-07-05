using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WindowsFormsAppSH2
{
    public partial class Form2 : Form
    {

        readonly string defpath = "D:\\олд\\source\\diplom\\FormAppSH2\\data.txt";
        string path = "D:\\олд\\source\\diplom\\FormAppSH2\\data.txt";
        string addPath = "D:\\олд\\source\\diplom\\FormAppSH2\\data.txt";

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

        public Form2()
        {
            InitializeComponent();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "All files (*.*)|*.*|txt files (*.txt)|*.txt";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                path = openFileDialog1.FileName;
                textBox8.Text = "1: " + path;
            }
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.chart1.Series[0].Points.Clear();
            this.chart1.Series[1].Points.Clear();
            chart1.MouseWheel += chart1_MouseWheel;
            chart1.ChartAreas[0].AxisX.Title = "Частота вращения коленвала, об/мин";
            chart1.ChartAreas[0].AxisY.Title = "Крутящий момент, Нм";
            chart1.ChartAreas[0].AxisY2.Title = "Мощность";
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
                textBox33.Text = "min:" + min1 + " " + "max:" + max1;
            }
            //avr();
            mouseMoveFlag = true;

        }

        private void СomparePlot1(string msg) //Сompare first
        {
            double msgs = double.Parse(msg, CultureInfo.GetCultureInfo("fr-FR"));
            if (msgs > max1)
            {
                max1 = msgs;
            }
            if (msgs < min1)
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

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "All files (*.*)|*.*|txt files (*.txt)|*.txt";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                addPath = openFileDialog1.FileName;
                textBox5.Text = "2: " + addPath;
            }
            else return;
        }

        private void button2_Click(object sender, EventArgs e)
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
                textBox1.Text = "min:" + min2 + " " + "max:" + max2;
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
    }
}
