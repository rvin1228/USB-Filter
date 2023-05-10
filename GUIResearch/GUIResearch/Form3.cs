using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Renci.SshNet;
using IronPython.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace GUIResearch
{
    public partial class historicalData : Form
    {
        private SshClient sshClient;
        public historicalData()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            sshClient = new SshClient("raspberry", "admin", "raspberry");
            sshClient.Connect();

            var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
            var lines = commandResult.Result.Split('\n');

            string deviceName = "";
            int copyCount = 0;
            int moveCount = 0;
            int malwareCount = 0;
            int safeCount = 0;
            int restoreCount = 0;
            int deleteCount = 0;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("Device Name:"))
                {
                    deviceName = line.Substring("Device Name:".Length).Trim();
                    lblDeviceName.Text = deviceName;
                }
                else if (line.StartsWith("Copy Count"))
                {
                    copyCount = int.Parse(line.Split(':')[1].Trim());
                    lblCopy.Text = copyCount.ToString();
                }
                else if (line.StartsWith("Move Count"))
                {
                    moveCount = int.Parse(line.Split(':')[1].Trim());
                    lblMove.Text = moveCount.ToString();
                }
                else if (line.StartsWith("Malware Count"))
                {
                    malwareCount = int.Parse(line.Split(':')[1].Trim());
                    lblQuarantine.Text = malwareCount.ToString();
                }
                else if (line.StartsWith("Safe Count"))
                {
                    safeCount = int.Parse(line.Split(':')[1].Trim());
                    lblSafe.Text = safeCount.ToString();
                }
                else if (line.StartsWith("Restore Count"))
                {
                    restoreCount = int.Parse(line.Split(':')[1].Trim());
                    lblRestore.Text = restoreCount.ToString();
                }
                else if (line.StartsWith("Delete Count"))
                {
                    deleteCount = int.Parse(line.Split(':')[1].Trim());
                    lblDelete.Text = deleteCount.ToString();
                }
            }

            copyPercentage.Text = (Math.Round((double)copyCount / (copyCount + moveCount) * 100, 2)).ToString() + "%";
            movePercentage.Text = (Math.Round((double)moveCount / (copyCount + moveCount) * 100, 2)).ToString() + "%";
            restorePercentage.Text = (Math.Round((double)restoreCount / (restoreCount + deleteCount) * 100, 2)).ToString() + "%";
            deletePercentage.Text = (Math.Round((double)deleteCount / (restoreCount + deleteCount) * 100, 2)).ToString() + "%";
            malwarePercentage.Text = (Math.Round((double)malwareCount / (malwareCount + safeCount) * 100, 2)).ToString() + "%";
            safePercentage.Text = (Math.Round((double)safeCount / (malwareCount + safeCount) * 100, 2)).ToString() + "%";


            var series = new System.Windows.Forms.DataVisualization.Charting.Series();

            chart1.Series.Add("Data");
            chart1.Series["Data"].ChartType = SeriesChartType.Pie;
            chart2.Series.Add("Data");
            chart2.Series["Data"].ChartType = SeriesChartType.Pie;
            chart3.Series.Add("Data");
            chart3.Series["Data"].ChartType = SeriesChartType.Pie;

            chart1.Series["Data"].Points.AddXY(((Math.Round((double)copyCount / (copyCount + moveCount) * 100, 2)).ToString() + "%"), copyCount);
            chart1.Series["Data"].Points[0].LegendText = "Copy";
            chart1.Series["Data"].Points.AddXY(((Math.Round((double)moveCount / (copyCount + moveCount) * 100, 2)).ToString() + "%"), moveCount);
            chart1.Series["Data"].Points[1].LegendText = "Move";
            chart2.Series["Data"].Points.AddXY(((Math.Round((double)restoreCount / (restoreCount + deleteCount) * 100, 2)).ToString() + "%"), restoreCount);
            chart2.Series["Data"].Points[0].LegendText = "Restore";
            chart2.Series["Data"].Points.AddXY(((Math.Round((double)deleteCount / (restoreCount + deleteCount) * 100, 2)).ToString() + "%"), deleteCount);
            chart2.Series["Data"].Points[1].LegendText = "Delete";
            chart3.Series["Data"].Points.AddXY(((Math.Round((double)malwareCount / (malwareCount + safeCount) * 100, 2)).ToString() + "%"), malwareCount);
            chart3.Series["Data"].Points[0].LegendText = "Malware";
            chart3.Series["Data"].Points.AddXY(((Math.Round((double)safeCount / (malwareCount + safeCount) * 100, 2)).ToString() + "%"), safeCount);
            chart3.Series["Data"].Points[1].LegendText = "Safe";

            series.Points.AddXY("Copy", copyCount);
            series.Points.AddXY("Move", moveCount);
            series.Points.AddXY("Restore", restoreCount);
            series.Points.AddXY("Delete", deleteCount);
            series.Points.AddXY("Quarantine", malwareCount);
            series.Points.AddXY("Restore", restoreCount);

            this.Controls.Add(chart1);
            this.Controls.Add(chart2);
            this.Controls.Add(chart3);
            chart4.Series.Add(series);

        }

        private void viewButton_Click(object sender, EventArgs e)
        {
            detailedView dv = new detailedView();
            this.Close();
            dv.Show();
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            Form1 f1 = new Form1();
            this.Close();
            f1.Show();
        }

        private void historicalData_FormClosing(object sender, FormClosingEventArgs e)
        {
            var sshClient = new SshClient("raspberry", "admin", "raspberry");
            sshClient.Connect();

            var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
            var lines = commandResult.Result.Split('\n');
            string deviceName = System.Environment.MachineName;
            lines[0] = $"Device Name: {deviceName}";
            var updatedText = string.Join("\n", lines);
            sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");

            sshClient.Disconnect();
            sshClient.Dispose();
        }

        private void lblSafe_Click(object sender, EventArgs e)
        {

        }
    }
}
