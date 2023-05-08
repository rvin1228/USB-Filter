using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Renci.SshNet;
using IronPython.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace GUIResearch
{
    public partial class restoreDestination : Form
    {
        private SshClient sshClient;
        public restoreDestination()
        {
            InitializeComponent();
        }
        public string SelectedFullPath { get; set; }

        private void restoreDestination_Load(object sender, EventArgs e)
        {
            sshClient = new SshClient("raspberry", "admin", "raspberry");
            sshClient.Connect();
            destDialog.Items.Add("Desktop");

            // Check for connected USB devices and add them to the checklist
            var usbDrivesCommandResult = sshClient.RunCommand("ls /media/admin/");
            var usbDrives = usbDrivesCommandResult.Result.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
            foreach (var drive in usbDrives)
            {
                destDialog.Items.Add(Path.GetFileName(drive));
            }

            destDialog.SelectionMode = SelectionMode.One;
            sshClient.Disconnect();
            sshClient.Dispose();
        }

        private void selectButton_Click(object sender, EventArgs e)
        {
            if (destDialog.Items.Count > 0 && destDialog.SelectedItem != null)
            {
                if (destDialog.SelectedItem.ToString() == "Desktop")
                {
                    SelectedFullPath = "/home/admin/Desktop";
                }
                else
                {
                    SelectedFullPath = $"/media/admin/{destDialog.SelectedItem.ToString()}";
                }

                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
            }

            this.Close();
        }
    }
}
