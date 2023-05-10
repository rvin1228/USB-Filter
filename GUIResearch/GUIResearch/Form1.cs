using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Renci.SshNet;
using IronPython.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Drawing;

namespace GUIResearch
{
    public partial class Form1 : Form
    {

        private Stack<List<ListViewItem>> pcListViewStack = new Stack<List<ListViewItem>>();
        private Stack<List<ListViewItem>> usbListViewStack = new Stack<List<ListViewItem>>();
        private ListView destinationListView = new ListView();
        private System.Threading.Timer pcFilesUpdateTimer;
        private SshClient sshClient;

        string deviceName = "";
        int copyCount = 0;
        int moveCount = 0;
        int malwareCount = 0;
        int safeCount = 0;
        int restoreCount = 0;
        int deleteCount = 0;

        public Form1()
        {
            InitializeComponent();
            pcFilesUpdateTimer = new System.Threading.Timer(new TimerCallback(pcFilesUpdateTimer_Tick), null, 0, 10000);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            sshClient = new SshClient("raspberry", "admin", "raspberry");

            try
            {
                sshClient.Connect();

                //Displaying PC Filenames and Foldernames
                var resultPc = sshClient.RunCommand("ls -1 Desktop");
                var outputPc = resultPc.Result;
                var linesPc = outputPc.Split('\n');

                pcFiles.Items.Clear();
                pcFiles.View = View.List;

                foreach (var line in linesPc)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        pcFiles.Items.Add(line.Trim());
                    }
                }

                var commandResult = sshClient.RunCommand("ls /home/admin/Quarantine");
                var fileList = commandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));

                foreach (var file in fileList)
                {
                    string cleanedFileName = file.Replace(".exe.quarantine", "");
                    quarantinedFiles.Items.Add(cleanedFileName);
                }

                string desktopPath = "/home/admin/Desktop/";
                string infoFileName = "info.txt";
                string infoFilePath = Path.Combine(desktopPath, infoFileName);

                if (!sshClient.RunCommand($"test -e {infoFilePath}").ExitStatus.Equals(0))
                {
                    using (var sftp = new SftpClient(sshClient.ConnectionInfo))
                    {
                        sftp.Connect();
                        using (var fileStream = sftp.Create(infoFilePath))
                        {
                            using (var writer = new StreamWriter(fileStream))
                            {
                                writer.WriteLine($"Device Name: {deviceName}");
                                writer.WriteLine($"Copy Count: {copyCount}");
                                writer.WriteLine($"Move Count: {moveCount}");
                                writer.WriteLine($"Malware Count: {malwareCount}");
                                writer.WriteLine($"Safe Count: {safeCount}");
                                writer.WriteLine($"Restore Count: {restoreCount}");
                                writer.WriteLine($"Delete Count: {deleteCount}");
                            }
                        }
                        sftp.Disconnect();
                    }
                }

                //Displaying USB Filenames and Foldernames
                var command = "lsblk -o LABEL /dev/sd* | grep -v \"^$\" | awk 'NR>1 {print $1}' | sort | uniq";
                var usbDeviceName = sshClient.RunCommand(command);
                var resultUsbDeviceName = usbDeviceName.Result;
                var linesResultUsbDeviceName = resultUsbDeviceName.Split('\n');

                foreach (var deviceName in linesResultUsbDeviceName)
                {
                    var resultUsb = sshClient.RunCommand($"ls -1 /media/admin/{deviceName}");
                    var outputUsb = resultUsb.Result;
                    var linesUsb = outputUsb.Split('\n');

                    usbFiles.Items.Clear();
                    usbFiles.View = View.List;

                    foreach (var line in linesUsb)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            // Split the result into individual USB device names
                            var usbDeviceNames = resultUsbDeviceName.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                            // Populate the ComboBox with USB device names
                            usbComboBox.Items.Add(line.Trim());
                        }
                    }
                }

                if (usbComboBox.Items.Count == 1)
                {
                    usbComboBox.SelectedIndex = 0;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        //Back Button for PC
        private void backButtonPc_Click(object sender, EventArgs e)
        {
            if (pcListViewStack.Count > 0)
            {
                List<ListViewItem> previousListViewItems = pcListViewStack.Pop();
                pcFiles.Items.Clear();
                pcFiles.Items.AddRange(previousListViewItems.ToArray());
            }
            else
            {
                MessageBox.Show("No previous folder found.");
            }
        }


        //Back Button for USB
        private void backButtonUsb_Click(object sender, EventArgs e)
        {
            if (usbListViewStack.Count > 0)
            {
                List<ListViewItem> previousListViewItems = usbListViewStack.Pop();
                usbFiles.Items.Clear();
                usbFiles.Items.AddRange(previousListViewItems.ToArray());
            }
            else
            {
                MessageBox.Show("No previous folder found.");
            }
        }

        //Copy Button to Copy a File or a Folder From PC to USB and Vice-Versa
        private void copyButton_Click(object sender, EventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                //Copy from PC to USB
                if (pcFiles.SelectedItems.Count == 1 && usbFiles.SelectedItems.Count == 0)
                {
                    // Show confirmation dialog
                    DialogResult result = MessageBox.Show("Are you sure you want to copy this file?", "Confirmation", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        var sourceFile = pcFiles.SelectedItems[0].Text;
                        var directoriesToSearch = "/home/admin/Desktop";
                        var findCommand = $"find {string.Join(" ", directoriesToSearch)} -name '{sourceFile}' 2>/dev/null";
                        var sourcePath = sshClient.RunCommand(findCommand);
                        var sourceOutput = sourcePath.Result;

                        var command = "lsblk -o LABEL /dev/sd* | grep -v \"^$\" | awk 'NR>1 {print $1}' | sort | uniq";
                        var usbDeviceName = sshClient.RunCommand(command);
                        var resultUsbDeviceName = usbDeviceName.Result;

                        //sshClient.RunCommand($"cp -r \"{sourceOutput.Trim()}\" \"/media/admin/{resultUsbDeviceName.Trim()}/\"");
                        sshClient.RunCommand($"cp -r \"{sourceOutput.Trim()}\" \"/media/admin/{usbComboBox.SelectedItem.ToString()}/\"");


                        var quarantinePath = sshClient.RunCommand($"find /home/admin/Quarantine -name '{sourceFile}.quarantine' 2>/dev/null");
                        var quarantineOutput = quarantinePath.Result;
                        if (!string.IsNullOrWhiteSpace(quarantineOutput))
                        {
                            // File is in the quarantine folder
                            MessageBox.Show($"The selected file has been added to the quarantine folder.");


                            quarantinedFiles.Items.Clear();
                            var fileListCommandResult = sshClient.RunCommand("ls /home/admin/Quarantine");
                            var fileList = fileListCommandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                            foreach (var file in fileList)
                            {
                                quarantinedFiles.Items.Add(file.Replace(".exe.quarantine", ""));
                            }
                        }
                        else
                        {
                            MessageBox.Show($"{sourceFile} successfully copied");
                        }


                        // Read the existing contents of info.txt
                        var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
                        var lines = commandResult.Result.Split('\n');

                        // Update the copy count
                        int copyCount = int.Parse(lines[1].Split(':')[1].Trim());
                        copyCount++;
                        lines[1] = $"Copy Count: {copyCount}";

                        var updatedText = string.Join("\n", lines);
                        sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");

                        var selectedDevice = usbComboBox.SelectedItem.ToString();

                        RefreshUSBFilesListView(selectedDevice);
                    }
                }

                //Copy a File or a Folder from USB to PC
                else if (usbFiles.SelectedItems.Count == 1 && pcFiles.SelectedItems.Count == 0)
                {
                    // Show confirmation dialog
                    DialogResult result = MessageBox.Show("Are you sure you want to copy this file?", "Confirmation", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        var command = "lsblk -o LABEL /dev/sd* | grep -v \"^$\" | awk 'NR>1 {print $1}' | sort | uniq";
                        var usbDeviceName = sshClient.RunCommand(command);
                        var resultUsbDeviceName = usbDeviceName.Result;

                        var sourceFile = usbFiles.SelectedItems[0].Text;
                        var sourcePath = sshClient.RunCommand($"find /media/admin/{resultUsbDeviceName.Trim()} -name '{sourceFile.Trim()}' 2>/dev/null");
                        var output = sourcePath.Result;

                        sshClient.RunCommand($"cp -r \"{output.Trim()}\" \"/home/admin/Desktop\"");
                        //MessageBox.Show($"{sourceFile} successfully copied to Desktop folder");

                        var quarantinePath = sshClient.RunCommand($"find /home/admin/Quarantine -name '{sourceFile}.quarantine' 2>/dev/null");
                        var quarantineOutput = quarantinePath.Result;
                        if (!string.IsNullOrWhiteSpace(quarantineOutput))
                        {
                            // File is in the quarantine folder
                            MessageBox.Show($"The selected file has been added to the quarantine folder.");


                            quarantinedFiles.Items.Clear();
                            var fileListCommandResult = sshClient.RunCommand("ls /home/admin/Quarantine");
                            var fileList = fileListCommandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                            foreach (var file in fileList)
                            {
                                quarantinedFiles.Items.Add(file.Replace(".exe.quarantine", ""));
                            }
                        }
                        else
                        {
                            MessageBox.Show($"{sourceFile} successfully copied to Desktop folder");
                        }

                        // Read the existing contents of info.txt
                        var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
                        var lines = commandResult.Result.Split('\n');

                        // Update the copy count
                        int copyCount = int.Parse(lines[1].Split(':')[1].Trim());
                        copyCount++;
                        lines[1] = $"Copy Count: {copyCount}";

                        var updatedText = string.Join("\n", lines);
                        sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");

                        var selectedDevice = usbComboBox.SelectedItem.ToString();

                        RefreshUSBFilesListView(selectedDevice);
                    }
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        //Move Button to Move a File or a Folder From PC to USB and Vice-Versa
        private void moveButton_Click(object sender, EventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                //Move from PC to USB
                if (pcFiles.SelectedItems.Count == 1 && usbFiles.SelectedItems.Count == 0)
                {
                    // Show confirmation dialog
                    DialogResult result = MessageBox.Show("Are you sure you want to move this file?", "Confirmation", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        var sourceFile = pcFiles.SelectedItems[0].Text;
                        var directoriesToSearch = "/home/admin/Desktop";
                        var findCommand = $"find {string.Join(" ", directoriesToSearch)} -name '{sourceFile}' 2>/dev/null";
                        var sourcePath = sshClient.RunCommand(findCommand);
                        var sourceOutput = sourcePath.Result;

                        var command = "lsblk -o LABEL /dev/sd* | grep -v \"^$\" | awk 'NR>1 {print $1}' | sort | uniq";
                        var usbDeviceName = sshClient.RunCommand(command);
                        var resultUsbDeviceName = usbDeviceName.Result;

                        sshClient.RunCommand($"mv \"{sourceOutput.Trim()}\" \"/media/admin/{resultUsbDeviceName.Trim()}/\"");
                        //MessageBox.Show($"{sourceFile} successfully moved");

                        var quarantinePath = sshClient.RunCommand($"find /home/admin/Quarantine -name '{sourceFile}.quarantine' 2>/dev/null"); //DITO PROBLEM
                        var quarantineOutput = quarantinePath.Result;
                        if (!string.IsNullOrWhiteSpace(quarantineOutput))
                        {
                            // File is in the quarantine folder
                            MessageBox.Show($"The selected file has been added to the quarantine folder.");


                            quarantinedFiles.Items.Clear();
                            var fileListCommandResult = sshClient.RunCommand("ls /home/admin/Quarantine");
                            var fileList = fileListCommandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                            foreach (var file in fileList)
                            {
                                quarantinedFiles.Items.Add(file.Replace(".exe.quarantine", ""));
                            }
                        }
                        else
                        {
                            MessageBox.Show($"{sourceFile} successfully moved");
                        }

                        var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
                        var lines = commandResult.Result.Split('\n');

                        // Update the copy count
                        int moveCount = int.Parse(lines[2].Split(':')[1].Trim());
                        moveCount++;
                        lines[2] = $"Move Count: {moveCount}";

                        var updatedText = string.Join("\n", lines);
                        sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");


                        var selectedDevice = usbComboBox.SelectedItem.ToString();

                        RefreshUSBFilesListView(selectedDevice);
                    }
                }

                //Move a File or a Folder from USB to PC
                else if (usbFiles.SelectedItems.Count == 1 && pcFiles.SelectedItems.Count == 0)
                {
                    DialogResult result = MessageBox.Show("Are you sure you want to move this file?", "Confirmation", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        var command = "lsblk -o LABEL /dev/sd* | grep -v \"^$\" | awk 'NR>1 {print $1}' | sort | uniq";
                        var usbDeviceName = sshClient.RunCommand(command);
                        var resultUsbDeviceName = usbDeviceName.Result;

                        var sourceFile = usbFiles.SelectedItems[0].Text;
                        var sourcePath = sshClient.RunCommand($"find /media/admin/{resultUsbDeviceName.Trim()} -name '{sourceFile.Trim()}' 2>/dev/null");
                        var output = sourcePath.Result;

                        sshClient.RunCommand($"mv \"{output.Trim()}\" \"/home/admin/Desktop\"");
                        //MessageBox.Show($"{sourceFile} successfully moved to Desktop folder");

                        var quarantinePath = sshClient.RunCommand($"find /home/admin/Quarantine -name '{sourceFile}.quarantine' 2>/dev/null");
                        var quarantineOutput = quarantinePath.Result;
                        if (!string.IsNullOrWhiteSpace(quarantineOutput))
                        {
                            // File is in the quarantine folder
                            MessageBox.Show($"The selected file has been added to the quarantine folder.");


                            quarantinedFiles.Items.Clear();
                            var fileListCommandResult = sshClient.RunCommand("ls /home/admin/Quarantine");
                            var fileList = fileListCommandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                            foreach (var file in fileList)
                            {
                                quarantinedFiles.Items.Add(file.Replace(".exe.quarantine", ""));
                            }
                        }
                        else
                        {
                            MessageBox.Show($"{sourceFile} successfully moved to Desktop folder");
                        }

                        var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
                        var lines = commandResult.Result.Split('\n');

                        // Update the copy count
                        int moveCount = int.Parse(lines[2].Split(':')[1].Trim());
                        moveCount++;
                        lines[2] = $"Move Count: {moveCount}";

                        var updatedText = string.Join("\n", lines);
                        sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");

                        var selectedDevice = usbComboBox.SelectedItem.ToString();

                        RefreshUSBFilesListView(selectedDevice);
                    }
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        //Restore Button to Run the restore.py Script
        private void restoreButton_Click(object sender, EventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                if (quarantinedFiles.SelectedItems.Count > 0)
                {
                    var engine = Python.CreateEngine();
                    var scope = engine.CreateScope();
                    string filename = quarantinedFiles.SelectedItem.ToString();

                    DialogResult result = MessageBox.Show($"Are you sure you want to restore {filename}.exe.quarantine?", "Confirmation", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        var destForm = new restoreDestination();
                        destForm.ShowDialog();

                        if (destForm.SelectedFullPath != null)
                        {
                            string destination = destForm.SelectedFullPath;
                            sshClient.RunCommand($"python /home/admin/scripts/restore.py {destination} {filename}");
                            MessageBox.Show($"{filename} restored successfully");

                            var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
                            var lines = commandResult.Result.Split('\n');

                            int restoreCount = int.Parse(lines[5].Split(':')[1].Trim());
                            restoreCount++;
                            lines[5] = $"Restore Count: {restoreCount}";

                            var updatedText = string.Join("\n", lines);
                            sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");

                            quarantinedFiles.Items.Clear();
                            var fileListCommandResult = sshClient.RunCommand("ls /home/admin/Quarantine");
                            var fileList = fileListCommandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                            foreach (var file in fileList)
                            {
                                quarantinedFiles.Items.Add(file.Replace(".exe.quarantine", ""));
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        //Delete Button to Run the delete.py Script
        //GUMAGANA NA TO!!!!!!
        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                if (quarantinedFiles.SelectedItems.Count > 0)
                {
                    string filename = quarantinedFiles.SelectedItem.ToString();

                    DialogResult result = MessageBox.Show($"Are you sure you want to delete {filename}.exe.quarantine?", "Confirmation", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        sshClient.RunCommand($"python /home/admin/scripts/delete.py {filename}");

                        MessageBox.Show($"{filename} deleted successfully");

                        var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
                        var lines = commandResult.Result.Split('\n');
                        int deleteCount = int.Parse(lines[6].Split(':')[1].Trim());
                        deleteCount++;
                        lines[6] = $"Delete Count: {deleteCount}";
                        var updatedText = string.Join("\n", lines);
                        sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");

                        quarantinedFiles.Items.Clear();
                        var fileListCommandResult = sshClient.RunCommand("ls /home/admin/Quarantine");
                        var fileList = fileListCommandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                        foreach (var file in fileList)
                        {
                            quarantinedFiles.Items.Add(file.Replace(".exe.quarantine", ""));
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        //Display Information Button to Display the Information of a Selected File or Folder Either From PC or USB
        private void displayInfoButton_Click(object sender, EventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                if (pcFiles.SelectedItems.Count > 0)
                {
                    try
                    {

                        if (pcFiles.SelectedItems.Count > 0)
                        {
                            var fileName = pcFiles.SelectedItems[0].Text;
                            var directoriesToSearch = "/home/admin/Desktop";
                            var findCommand = $"find {string.Join(" ", directoriesToSearch)} -name '{fileName}' 2>/dev/null";
                            var filePath = sshClient.RunCommand(findCommand);
                            var fileOutput = filePath.Result;

                            var fileInfo = new FileInfo(fileOutput.Trim());
                            var isDirectory = (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                            var result = sshClient.RunCommand(isDirectory ? $"ls -ld {fileOutput.Trim()}" : $"ls -l {fileOutput.Trim()}");
                            var output = result.Result;

                            var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 0)
                            {
                                var info = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var name = info[info.Length - 1].Substring(info[info.Length - 1].LastIndexOf('/') + 1);
                                var path = Path.GetDirectoryName(info[info.Length - 1]);

                                MessageBox.Show($"File Name: {name}{Environment.NewLine}" +
                                    $"File Path: {path}{Environment.NewLine}File Permission: {info[0]}{Environment.NewLine}" +
                                    $"File Size: {info[4]} bytes");
                            }
                            else
                            {
                                MessageBox.Show("No file information found.");
                            }
                        }
                        else if (usbFiles.SelectedItems.Count > 0)
                        {

                            var command = "lsblk -o LABEL /dev/sd* | grep -v \"^$\" | awk 'NR>1 {print $1}' | sort | uniq";
                            var usbDeviceName = sshClient.RunCommand(command);
                            var resultUsbDeviceName = usbDeviceName.Result;

                            var fileName = usbFiles.SelectedItems[0].Text;
                            var filePath = sshClient.RunCommand($"find /media/admin/{resultUsbDeviceName.Trim()} -name '{fileName.Trim()}' 2>/dev/null");
                            var fileOutput = filePath.Result;

                            var fileInfo = new FileInfo(fileOutput.Trim());
                            var isDirectory = (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                            var result = sshClient.RunCommand(isDirectory ? $"ls -ld {fileOutput.Trim()}" : $"ls -l {fileOutput.Trim()}");
                            var output = result.Result;

                            var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 0)
                            {
                                var info = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                var name = info[info.Length - 1].Substring(info[info.Length - 1].LastIndexOf('/') + 1);
                                var path = Path.GetDirectoryName(info[info.Length - 1]);

                                MessageBox.Show($"File Name: {name}{Environment.NewLine}" +
                                    $"File Path: {path}{Environment.NewLine}File Permission: {info[0]}{Environment.NewLine}" +
                                    $"File Size: {info[4]} bytes");
                            }
                            else
                            {
                                MessageBox.Show("No file information found.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        private void RefreshUSBFilesListView(string selectedDevice)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                try
                {
                    usbFiles.Items.Clear();

                    var command = $"ls -l /media/admin/{selectedDevice}";
                    var result = sshClient.RunCommand(command);
                    var output = result.Result;

                    var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var fields = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var fileName = fields[fields.Length - 1];

                        var item = new ListViewItem(fileName);
                        usbFiles.Items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        private void RefreshPCFilesListView()
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                var resultPc = sshClient.RunCommand("ls -1 Desktop");
                var outputPc = resultPc.Result;
                var linesPc = outputPc.Split('\n');

                pcFiles.Items.Clear();
                pcFiles.View = View.List;

                foreach (var line in linesPc)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        pcFiles.Items.Add(line.Trim());
                    }
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        private void historicalData_Click(object sender, EventArgs e)
        {
            historicalData hd = new historicalData();
            this.Hide();
            hd.Show();
        }

        private void usbComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedDevice = usbComboBox.SelectedItem.ToString();

            // Update the ListView based on the selected USB device
            RefreshUSBListView(selectedDevice);
        }

        private void RefreshUSBListView(string selectedDevice)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                try
                {
                    usbFiles.Items.Clear();

                    var command = $"ls -l /media/admin/{selectedDevice}";
                    var result = sshClient.RunCommand(command);
                    var output = result.Result;

                    var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var fields = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        var fileName = fields[fields.Length - 1];

                        var item = new ListViewItem(fileName);
                        usbFiles.Items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        private void pcFiles_ItemActivate(object sender, EventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                try
                {
                    if (pcFiles.SelectedItems.Count > 0)
                    {
                        foreach (ListViewItem selectedItem in pcFiles.SelectedItems)
                        {
                            var fileName = selectedItem.Text;
                            var filePath = sshClient.RunCommand($"find /home/admin/Desktop -name '{fileName}' 2>/dev/null");
                            var output = filePath.Result;

                            var fileOrDir = sshClient.RunCommand($"file -b {output}");
                            var isFile = fileOrDir.Result.Contains("regular file");
                            var isDir = fileOrDir.Result.Contains("directory");

                            if (isDir)
                            {
                                var openFolder = sshClient.RunCommand($"ls -1 {output}");
                                var outputOpenFolder = openFolder.Result;
                                var linesOutputOpenFolder = outputOpenFolder.Split('\n');

                                List<ListViewItem> currentListViewItems = new List<ListViewItem>();
                                foreach (ListViewItem item in pcFiles.Items)
                                {
                                    currentListViewItems.Add(item.Clone() as ListViewItem);
                                }
                                pcListViewStack.Push(currentListViewItems);

                                pcFiles.Items.Clear();
                                pcFiles.View = View.List;

                                foreach (var line in linesOutputOpenFolder)
                                {
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        pcFiles.Items.Add(line.Trim());
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show($"Selected item {fileName} is a file.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        private void usbFiles_ItemActivate(object sender, EventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                try
                {
                    if (usbFiles.SelectedItems.Count > 0)
                    {
                        foreach (ListViewItem selectedItem in usbFiles.SelectedItems)
                        {
                            var command = "lsblk -o LABEL /dev/sd* | grep -v \"^$\" | awk 'NR>1 {print $1}' | sort | uniq";
                            var usbDeviceName = sshClient.RunCommand(command);
                            var resultUsbDeviceName = usbDeviceName.Result;
                            var linesResultUsbDeviceName = resultUsbDeviceName.Split('\n');

                            foreach (var deviceName in linesResultUsbDeviceName)
                            {
                                var fileName = selectedItem.Text;
                                var filePath = sshClient.RunCommand($"find /media/admin/{deviceName} -name '{fileName}' 2>/dev/null");
                                var output = filePath.Result;

                                var openFolder = sshClient.RunCommand($"ls -1 {output}");
                                var outputOpenFolder = openFolder.Result;
                                var linesOutputOpenFolder = outputOpenFolder.Split('\n');

                                List<ListViewItem> currentListViewItems = new List<ListViewItem>();
                                foreach (ListViewItem item in usbFiles.Items)
                                {
                                    currentListViewItems.Add(item.Clone() as ListViewItem);
                                }
                                usbListViewStack.Push(currentListViewItems);

                                usbFiles.Items.Clear();
                                usbFiles.View = View.List;

                                foreach (var line in linesOutputOpenFolder)
                                {
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        usbFiles.Items.Add(line.Trim());
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            usbComboBox.Refresh();
            if (usbComboBox.Items.Count == 1)
            {
                usbComboBox.SelectedIndex = 0;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sshClient != null && sshClient.IsConnected)
            {
                var commandResult = sshClient.RunCommand("cat /home/admin/scripts/info.txt");
                var lines = commandResult.Result.Split('\n');
                string deviceName = System.Environment.MachineName;
                lines[0] = $"Device Name: {deviceName}";
                var updatedText = string.Join("\n", lines);
                sshClient.RunCommand($"echo '{updatedText}' > /home/admin/scripts/info.txt");
            }
            else
            {
                MessageBox.Show("Application failed to establish SSH connection.");
            }
        }

        private void usbFiles_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void pcFiles_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void downloadButton_Click(object sender, EventArgs e)
        {
            if (pcFiles.SelectedItems.Count > 0)
            {
                //var host = "192.168.189.128";
                //var username = "raspberry";
                var localDownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Downloads";


                using (var sftpClient = new SftpClient(sshClient.ConnectionInfo))
                {
                    sftpClient.Connect();
                    try
                    {
                        foreach (ListViewItem selectedItem in pcFiles.SelectedItems)
                        {
                            var selectedFile = selectedItem.Text;
                            var remoteFilePath = $"/home/admin/Desktop/{selectedFile}";

                            using (var fileStream = File.Create($"{localDownloadsPath}/{selectedFile}"))
                            {
                                sftpClient.DownloadFile(remoteFilePath, fileStream);
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"{ex.Message}");
                    }
                }

                MessageBox.Show("All selected files downloaded successfully.");
            }
            else
            {
                MessageBox.Show("No file selected");
            }
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files (.)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var host = "192.168.189.128";
                var username = "raspberry";
                var remoteDirectoryPath = "/home/admin/Desktop";

                using (var sftpClient = new SftpClient(sshClient.ConnectionInfo))
                {
                    sftpClient.Connect();
                    int numFilesUploaded = 0;
                    foreach (var localFilePath in openFileDialog.FileNames)
                    {
                        using (var fileStream = File.OpenRead(localFilePath))
                        {
                            sftpClient.UploadFile(fileStream, $"{remoteDirectoryPath}/{Path.GetFileName(localFilePath)}");
                        }

                        numFilesUploaded++;
                    }
                    sftpClient.Disconnect();

                    MessageBox.Show($"All selected files uploaded successfully to {remoteDirectoryPath}.");
                }
            }
            else
            {
                MessageBox.Show("No file selected");
            }
        }

        private void pcFilesUpdateTimer_Tick(object state)
        {
            if (this.IsHandleCreated)
            {
                Invoke((MethodInvoker)delegate {
                    RefreshPCFilesListView();
                });
            }
        }

    }
}