using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class CS2 : SteamCMDAgent
    {
        // Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.CS2",
            author = "ohmcodes",
            description = "WindowsGSM plugin for supporting Counter Strike 2 Dedicated Server",
            version = "2.1.0",
            url = "https://github.com/ohmcodes/WindowsGSM.CS2",
            color = "#FFA500"
        };

        // Constructor
        public CS2(ServerConfig serverData) : base(serverData) => _serverData = serverData;
        private readonly ServerConfig _serverData;
        public new string Error, Notice;

        // SteamCMD installer settings
        public override bool loginAnonymous => true;
        public override string AppId => "730";

        // Game server fixed variables
        public override string StartPath => "game\\bin\\win64\\cs2.exe";
        public string FullName = "Counter Strike 2 Dedicated Server";
        public bool AllowsEmbedConsole = false;
        public int PortIncrements = 1;
        public object QueryMethod = new A2S();

        // Game server default values
        public string ServerName = "wgsm_cs2_dedicated";
        public string Defaultmap = "de_mirage";
        public string Maxplayers = "10";
        public string Port = "27015";
        public string QueryPort = "27016";
        public string Additional = "+sv_logfile 1";

        private Process _serverProcess;

        // Create a default cfg for the game server after installation
        public async void CreateServerCFG() { }

        // Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            string param = $"-dedicated +hostname {_serverData.ServerName} +map {_serverData.ServerMap} -ip {_serverData.ServerIP} -port {_serverData.ServerPort} -maxplayers {_serverData.ServerMaxPlayer} {_serverData.ServerParam}";

            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            try
            {
                _serverProcess.OutputDataReceived += ServerOutputDataReceived;
                _serverProcess.ErrorDataReceived += ServerOutputDataReceived;
                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();
                return _serverProcess;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null;
            }
        }

        // Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() => ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "quit"));
        }

        // Restart server function
        public async Task Restart(Process p)
        {
            await Stop(p);
            await Start();
        }

        // Update server function
        public new async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            p.WaitForExit();
            return p;
        }

        // Validate installation
        public new bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        // Validate import path
        public new bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            if (!File.Exists(exePath))
            {
                Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
                return false;
            }
            return true;
        }

        // Get local build
        public new string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        // Get remote build
        public new async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }

        // Handle server output
        private void ServerOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
                MainWindow._serverMetadata[int.Parse(_serverData.ServerID)].ServerConsole.Add(e.Data);
            }
        }
    }
}
