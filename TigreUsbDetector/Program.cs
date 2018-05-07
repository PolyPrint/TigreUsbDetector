using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using DateTime = System.DateTime;
using File = System.IO.File;

namespace TigreUsbDetector
{
    class Program
    {
        private static string _dataDir;
        private static string _driveName;

        static void Main()
        {
            _dataDir = ConfigurationManager.AppSettings["TigreDataDirectory"];
            _driveName = ConfigurationManager.AppSettings["DriveName"];

            File.OpenWrite("./Log.txt").Close();
            Directory.CreateDirectory("./Saved");
            using (var control = new UsbControl())
            {
                //Console.ReadLine();
                Thread.Sleep(Timeout.Infinite);
            }
        }


        private class UsbControl : IDisposable
        {
            // used for monitoring plugging and unplugging of USB devices.
            private readonly ManagementEventWatcher _watcherAttach;
            //private ManagementEventWatcher watcherDetach;

            public UsbControl()
            {
                // Add USB plugged event watching
                _watcherAttach = new ManagementEventWatcher();
                _watcherAttach.EventArrived += Attaching;
                _watcherAttach.Query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
                _watcherAttach.Start();

                // Add USB unplugged event watching
                //watcherDetach = new ManagementEventWatcher();
                //watcherDetach.EventArrived += Detaching;
                //watcherDetach.Query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 3");
                //watcherDetach.Start();
            }

            public void Dispose()
            {
                _watcherAttach.Stop();
                //watcherDetach.Stop();
                //you may want to yield or Thread.Sleep
                _watcherAttach.Dispose();
                //watcherDetach.Dispose();
                //you may want to yield or Thread.Sleep
            }

            /// <summary>
            /// Fires when Win32_Volume changes volume, like the insertion of a USB drive or attachment of
            /// any storage volume. 
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void Attaching(object sender, EventArrivedEventArgs e)
            {
                if (sender != _watcherAttach)
                    return;
                var strbld = new StringBuilder();
                NetworkConnection connection = null;
                strbld.AppendLine("====DETECTED USB====" + DateTime.Now.ToString("F"));
                Console.WriteLine("====DETECTED USB====" + DateTime.Now.ToString("F"));
                try
                {
                    connection = new NetworkConnection(@"\\pp-tuc-corvette", new NetworkCredential("tgabb", "tg2696"));
                }
                catch
                {
                    var msg =
                        "Tried to create new NetworkConnection to PP-TUC-CORVETTE and was unable to do so. Will try to continue without doing this";
                    strbld.AppendLine(msg);
                    Console.WriteLine(msg);
                }
                try
                {
                    var fcount = Directory.GetFiles(_driveName).Length;
                    strbld.AppendLine($"{fcount}Directory.GetFiles(_driveName).Length}} FILES");
                    Console.WriteLine($"{fcount} FILES");
                    string dir = null;
                    if(fcount > 0)
                    dir = Directory.CreateDirectory($"./Saved/{DateTime.Now.ToString("F").Replace(":", ".")}").FullName;
                    foreach (var file in Directory.GetFiles(_driveName))
                    {
                        try
                        {
                            var fname = Path.GetFileName(file);
                            var path = Path.Combine(_dataDir, fname);
                            strbld.AppendLine($@"Processing file {fname}");
                            Console.WriteLine($@"Processing file {fname}");
                            strbld.AppendLine("\tCopying...");
                            Console.WriteLine("\tCopying...");
                            File.Copy(file,Path.Combine(dir,fname));
                            File.Copy(file, path);
                            strbld.AppendLine("\tDeleting...");
                            Console.WriteLine("\tDeleting...");
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            strbld.AppendLine(ex.Message);
                            Console.WriteLine(ex.Message);
                            var msg =
@"An error has occurred whike trying to process the USB drive from Tigre Slitter. Please note the time and contact the IT Department
Se ha producido un error Tenga en cuenta la hora y el departamento de TI de contacto";
                            MessageBox.Show(msg, "Problem with Tigre USB", MessageBoxButtons.OK, MessageBoxIcon.Error,
                                MessageBoxDefaultButton.Button1, (MessageBoxOptions) 0x40000);
                            return;
                        }
                    }

                    strbld.AppendLine("====PROCESSING FINISHED====");
                    Console.WriteLine("====PROCESSING FINISHED====");
                    MessageBox.Show("Tigre USB Processed. " +
                                    "Please remove and place back into Tigre usb slot",
                        "Remove USB", MessageBoxButtons.OK, MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1, (MessageBoxOptions) 0x40000);
                }
                catch (Exception ex)
                {
                    strbld.AppendLine(ex.Message);
                    var msg =
@"An error has occurred whike trying to process the USB drive from Tigre Slitter. Please note the time and contact the IT Department
Se ha producido un error Tenga en cuenta la hora y el departamento de TI de contacto";
                    MessageBox.Show(msg, "Problem with Tigre USB", MessageBoxButtons.OK, MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1, (MessageBoxOptions) 0x40000);
                }
                finally
                {
                    File.AppendAllText("./Log.txt",strbld.ToString());
                    connection?.Dispose();
                }
            }

            //void Detaching(object sender, EventArrivedEventArgs e)
            //{
            //    if(sender!=watcherDetach)
            //        return;

            //}
            
            ~UsbControl()
            {
                Dispose(); // for ease of readability I left out the complete Dispose pattern
            }
        }
    }
}
