using System.ServiceProcess;
using System.Threading;

namespace MIP_SDK_Tray_Manager
{
    internal class TrayManagerService : ServiceBase
    {
        private Logs _trayManager;
        private Thread _workerThread;
        public TrayManagerService()
        {
            ServiceName = "MIPSDK_TrayManager";
        }
        protected override void OnStart(string[] args)
        {
            _trayManager = new Logs();
            _workerThread = new Thread(_trayManager.MainLogic);
            _workerThread.Start();
        }
        protected override void OnStop()
        {
            _trayManager = null;
            _workerThread?.Abort();
        }
    }
}