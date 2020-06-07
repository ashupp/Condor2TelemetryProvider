using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ILogger = SimFeedback.log.ILogger;

namespace SimFeedback.telemetry
{
    public sealed class TelemetryProvider : AbstractTelemetryProvider
    {

        private const int _portNum = 55278;
        private const string _ipAddr = "127.0.0.1";
        private bool _isStopped = true;
        private Thread _t;


        public TelemetryProvider()
        {
            Author = "ashupp / ashnet GmbH";
            Version = "0.0.1.0";
            BannerImage = @"img\banner_condor2.png";
            IconImage = @"img\icon_condor2.png";
            TelemetryUpdateFrequency = 60;
        }

        public override string Name => "condor2";

        public override void Init(ILogger logger)
        {
            base.Init(logger);
            Log("Initializing Condor2TelemetryProvider");
        }

        public override string[] GetValueList()
        {
            return GetValueListByReflection(typeof(TelemetryData));
        }

        public override void Stop()
        {
            if (_isStopped) return;
            LogDebug("Stopping Condor2TelemetryProvider");
            _isStopped = true;
            if (_t != null) _t.Join();
        }

        public override void Start()
        {
            if (_isStopped)
            {
                LogDebug("Starting Condor2TelemetryProvider");
                _isStopped = false;
                _t = new Thread(Run);
                _t.Start();
            }
        }

        private void Run()
        {
            TelemetryData lastTelemetryData = new TelemetryData();

            UdpClient socket = new UdpClient {ExclusiveAddressUse = false};
            socket.Client.Bind(new IPEndPoint(IPAddress.Parse(_ipAddr),_portNum));
            var endpoint = new IPEndPoint(IPAddress.Parse(_ipAddr), _portNum);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!_isStopped)
            {
                try
                {

                    // get data from game, 
                    if (socket.Available == 0)
                    {
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            IsRunning = false;
                            IsConnected = false;
                            Thread.Sleep(1000);
                        }
                        continue;
                    }
                    IsConnected = true;

                    Byte[] received = socket.Receive(ref endpoint);
                    string resp = Encoding.UTF8.GetString(received);
                    TelemetryData telemetryData = ParseReponse(resp);

                    IsRunning = true;

                    TelemetryEventArgs args = new TelemetryEventArgs(
                        new Condor2TelemetryInfo(telemetryData, lastTelemetryData));
                    RaiseEvent(OnTelemetryUpdate, args);
                    lastTelemetryData = telemetryData;

                    sw.Restart();
                }
                catch (Exception e)
                {
                    LogError("Condor2TelemetryProvider Exception while processing data", e);
                    IsConnected = false;
                    IsRunning = false;
                    Thread.Sleep(1000);
                }
            }

            IsConnected = false;
            IsRunning = false;
        }

        private TelemetryData ParseReponse(string resp)
        {
            TelemetryData telemetryData = new TelemetryData();

            string[] lines = resp.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            if (lines.Length >= 29)
            {
                telemetryData.Time = float.Parse(lines[0].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.AirSpeed = float.Parse(lines[1].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.TurnRate = float.Parse(lines[9].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.YawStringAngle = float.Parse(lines[10].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Yaw = float.Parse(lines[12].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Pitch = float.Parse(lines[13].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Roll = float.Parse(lines[14].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Surge = float.Parse(lines[19].Split('=')[1], CultureInfo.InvariantCulture);   // ax
                telemetryData.Sway = float.Parse(lines[20].Split('=')[1], CultureInfo.InvariantCulture);    // ay
                telemetryData.Heave = float.Parse(lines[21].Split('=')[1], CultureInfo.InvariantCulture);   // az
                telemetryData.RollRate = float.Parse(lines[25].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.PitchRate = float.Parse(lines[26].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.YawRate = float.Parse(lines[27].Split('=')[1], CultureInfo.InvariantCulture);
            }

            return telemetryData;
        }
    }
}