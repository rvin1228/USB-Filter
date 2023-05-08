using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Renci.SshNet;
using IronPython.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Data;

namespace GUIResearch
{
    public partial class detailedView : Form
    {
        private SshClient sshClient;
        public detailedView()
        {
            InitializeComponent();

            comboBox1.Items.Add("Filename");
            comboBox1.Items.Add("MD5 Hash Value");
            comboBox1.Items.Add("Date Detected");
            comboBox1.Items.Add("Status");

            LoadData();

        }

        public void LoadData()
        {
            sshClient = new SshClient("raspberry", "admin", "raspberry");
            sshClient.Connect();

            var command = sshClient.CreateCommand("cat /home/admin/scripts/data.txt");
            var result = command.Execute();

            var dataTable = new DataTable();
            dataTable.Columns.Add("Filename");
            dataTable.Columns.Add("File Path");
            dataTable.Columns.Add("File Size");
            dataTable.Columns.Add("MD5 Hash Value");
            dataTable.Columns.Add("Date Detected");
            dataTable.Columns.Add("Status");

            var lines = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("Filename:"))
                {
                    // Create a new DataRow and add it to the DataTable
                    var dataRow = dataTable.NewRow();
                    dataTable.Rows.Add(dataRow);

                    // Extract the filename and add it to the DataRow
                    dataRow["Filename"] = line.Split(':')[1].Trim();
                }
                else if (line.StartsWith("File Path:"))
                {
                    // Get the last added DataRow and extract the file path
                    var dataRow = dataTable.Rows[dataTable.Rows.Count - 1];
                    dataRow["File Path"] = line.Split(':')[1].Trim();
                }
                else if (line.StartsWith("File Size:"))
                {
                    // Get the last added DataRow and extract the file size
                    var dataRow = dataTable.Rows[dataTable.Rows.Count - 1];
                    dataRow["File Size"] = line.Split(':')[1].Trim();
                }
                else if (line.StartsWith("MD5 Hash Value:"))
                {
                    // Get the last added DataRow and extract the MD5 hash value
                    var dataRow = dataTable.Rows[dataTable.Rows.Count - 1];
                    dataRow["MD5 Hash Value"] = line.Split(':')[1].Trim();
                }
                else if (line.StartsWith("Date Detected:"))
                {
                    // Get the last added DataRow and extract the date detected
                    var dataRow = dataTable.Rows[dataTable.Rows.Count - 1];
                    dataRow["Date Detected"] = line.Split(new[] { ':' }, 2)[1].TrimStart();
                }
                else if (line.StartsWith("Status:"))
                {
                    // Get the last added DataRow and extract the status
                    var dataRow = dataTable.Rows[dataTable.Rows.Count - 1];
                    dataRow["Status"] = line.Split(':')[1].Trim();
                }
            }

            // Bind the DataTable to the DataGridView
            dataGridView1.DataSource = dataTable;


            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col.HeaderText == "File Path" || col.HeaderText == "MD5 Hash Value")
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    col.MinimumWidth = 300; // set a minimum width for the column
                }
                else if (col.HeaderText == "Date Detected")
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    col.MinimumWidth = 200; // set a minimum width for the column
                }
                else
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
        }

        private void backButton_Click(object sender, EventArgs e)
        {
            historicalData hd = new historicalData();
            this.Close();
            hd.Show();
        }

        private void detailedView_FormClosing(object sender, FormClosingEventArgs e)
        {
            var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
            var lines = commandResult.Result.Split('\n');
            string deviceName = System.Environment.MachineName;
            lines[0] = $"Device Name: {deviceName}";
            var updatedText = string.Join("\n", lines);
            sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");

            sshClient.Disconnect();
            sshClient.Dispose();
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            LoadData();

            if (comboBox1.SelectedItem == "Filename" || comboBox1.SelectedItem == "MD5 Hash Value" || comboBox1.SelectedItem == "Status")
            {
                string selectedColumn = null;
                if (comboBox1.SelectedItem != null)
                {
                    selectedColumn = comboBox1.SelectedItem.ToString();
                }

                string searchString = txtSearch.Text;

                DataView dv = new DataView(dataGridView1.DataSource as DataTable);
                dv.RowFilter = string.Format("[{0}] LIKE '%{1}%'", selectedColumn.Replace("'", "''"), searchString.Replace("'", "''"));
                dataGridView1.DataSource = dv;

                //lblNum.Text = $"{dataGridView1.Rows.Count} item(s)";
                //lblNum.Visible = true;
            }
            else if (comboBox1.SelectedItem == "Date Detected")
            {
                DateTime selectedDate = dateTimePicker1.Value.Date;

                DataView dv = new DataView(dataGridView1.DataSource as DataTable);
                dv.RowFilter = string.Format("CONVERT([Date Detected], 'System.String') LIKE '{0}%'", selectedDate.ToString("yyyy-MM-dd"));

                dataGridView1.DataSource = dv;

                //lblNum.Text = $"{dataGridView1.Rows.Count} item(s)";
                //lblNum.Visible = true;
            }

            txtSearch.Text = "";
        }

        private void detailedView_Load(object sender, EventArgs e)
        {

        }
    }
}
