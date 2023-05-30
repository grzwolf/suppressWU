using System;
using System.Timers;
using Topshelf;
using System.ServiceProcess;

//
// suppressWU
// ==========
//    stops 'Windows Update Service' = "wuauserv" - https://stackoverflow.com/questions/17793202/how-can-you-programatically-disable-windows-update-in-xp-using-c-sharp
//    stops Windows process "runtimebroker"       - https://www.howtogeek.com/268240/what-is-runtime-broker-and-why-is-it-running-on-my-pc/
//
//
// How-To create a C# Windows-Service with Topshelf Console-Application
// ====================================================================
//
//    Start Visual Studio and create a new C# Console-Application
//    get topshelf package
//    reference Topshelf.dll in Solution Explorer
//
//    Run cmd.exe as administrator
//
//    install & activate
//    Run the command: .\suppressWU install
//    Run the command: .\suppressWU start
//
//    deactivate & deinstall
//    Run the command: .\suppressWU stop
//    Run the command: .\suppressWU uninstall
// 
// command line arguments
// http://docs.topshelf-project.com/en/latest/overview/commandline.html
//

namespace suppressWU
{
    public class WorkerThread
    {
        // worker logic
        readonly Timer _timer;
        public WorkerThread()
        {
            _timer = new Timer(5000) ;
            _timer.AutoReset = true;
            _timer.Elapsed +=  onTimerElapsed;
            logEvent("init done");
        }
        public void Start() { 
            _timer.Start();
            logEvent("started");
        }
        public void Stop() { 
            _timer.Stop();
            logEvent("stopped");
        }
        private void onTimerElapsed( Object source, ElapsedEventArgs e )
        {
            stopWindowsUpdate();
            stopProcess("runtimebroker");
        }

        // stop a windows process: here runtimebroker
        void stopProcess(string processToStop)
        {
            System.Diagnostics.Process[] procArray = System.Diagnostics.Process.GetProcesses();
            foreach ( System.Diagnostics.Process p in procArray ) {
                string ProcessName = p.ProcessName;
                ProcessName = ProcessName.ToLower();
                if ( ProcessName.CompareTo(processToStop) == 0 ) {
                    try {
                        p.Kill();
                        logEvent(processToStop + " stopped");
                    } catch {
                        logEvent(processToStop + " stop failed");
                    }
                    break;
                }
            }
        }

        // stop a windows service: here windows update
        void stopWindowsUpdate()
        {
            ServiceController sc = new ServiceController("wuauserv");
            if ( sc == null ) {
                logEvent("FAIL: no wuauserv object");
                return;
            }
            try {
                if ( sc != null ) {
                    if ( sc.Status == ServiceControllerStatus.Running ) {
                        sc.Stop();
                        logEvent("wuauserv stopped");
                    }
                }
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                sc.Close();
            } catch ( Exception ex ) {
                logEvent("FAIL: exception = " + ex.Message);
            }
        }

        // logging: This PC --> Manage --> Event Viewer --> Windows Logs --> Application
        void logEvent( string message )
        {
            using ( System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog("Application") ) {
                eventLog.Source = "suppressWU";
                eventLog.WriteEntry(message, System.Diagnostics.EventLogEntryType.Information, 101, 1);
            }
        }
    }

    class Program
    {
        static void Main( string[] args )
        {
            HostFactory.Run(x =>                                 
            {
                x.Service<WorkerThread>(s =>                     
                {
                    s.ConstructUsing(name => new WorkerThread());
                    s.WhenStarted(tc => tc.Start());             
                    s.WhenStopped(tc => tc.Stop());              
                });

                x.RunAsLocalSystem();                            
                x.SetDescription("Suppress Windows Update");     
                x.SetDisplayName("SuppressWindowsUpdate");       
                x.SetServiceName("suppressWU");                  
            });                                                  
        }
    }
}
