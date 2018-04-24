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

            using (var control = new UsbControl())
            {
                Console.ReadLine();
                //Thread.Sleep(Timeout.Infinite);
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

            void Attaching(object sender, EventArrivedEventArgs e)
            {
                if (sender != _watcherAttach)
                    return;
                try
                {
                    Console.WriteLine("====DETECTED USB====");
                    Console.WriteLine($"{Directory.GetFiles(_driveName).Length} FILES");
                    //using (var connection =
                    //    new NetworkConnection(@"\\pp-tuc-corvette", new NetworkCredential("tgabb", "tg2696")))
                    //{
                        foreach (var file in Directory.GetFiles(_driveName))
                        {
                            try
                            {
                                var fname = Path.GetFileName(file);
                                var path = Path.Combine(_dataDir, fname);
                                Console.WriteLine($@"Processing file {fname}");
                                Console.WriteLine("\tCopying...");
                                File.Copy(file, path);
                                Console.WriteLine("\tDeleting...");
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                                var msg =
                                    @"An error has occurred whike trying to process the USB drive from Tigre Slitter. Please note the time and contact the IT Department
Se ha producido un error Tenga en cuenta la hora y el departamento de TI de contacto";
                                MessageBox.Show(msg, "Problem with Tigre USB", MessageBoxButtons.OK, MessageBoxIcon.Error, 
                                    MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000); 
                                return;
                            }
                        }
                    //}
                    Console.WriteLine("====PROCESSING FINISHED====");
                    MessageBox.Show("Tigre USB Processed. " +
                                    "Please remove and place back into Tigre usb slot",
                        "Remove USB", MessageBoxButtons.OK, MessageBoxIcon.Information, 
                        MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    var msg =
                        @"An error has occurred whike trying to process the USB drive from Tigre Slitter. Please note the time and contact the IT Department
Se ha producido un error Tenga en cuenta la hora y el departamento de TI de contacto";
                    MessageBox.Show(msg, "Problem with Tigre USB", MessageBoxButtons.OK, MessageBoxIcon.Error, 
                        MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000); 
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
