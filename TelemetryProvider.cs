using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
        private TelemetryData lastTelemetryData;
        private Stopwatch swData;

        public TelemetryProvider()
        {
            Author = "ashupp / ashnet GmbH";
            Version = Assembly.LoadFrom(Assembly.GetExecutingAssembly().Location).GetName().Version.ToString();
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
            lastTelemetryData = new TelemetryData();

            UdpClient socket = new UdpClient {ExclusiveAddressUse = false};
            socket.Client.Bind(new IPEndPoint(IPAddress.Parse(_ipAddr),_portNum));
            var endpoint = new IPEndPoint(IPAddress.Parse(_ipAddr), _portNum);
            Stopwatch sw = new Stopwatch();
            swData = new Stopwatch();
            sw.Start();
            swData.Start();
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
                    IsRunning = true;
                    Byte[] received = socket.Receive(ref endpoint);
                    string resp = Encoding.UTF8.GetString(received);
                    TelemetryData telemetryData = ParseReponse(resp);


                    TelemetryEventArgs args = new TelemetryEventArgs(
                        new Condor2TelemetryInfo(telemetryData, lastTelemetryData));
                    RaiseEvent(OnTelemetryUpdate, args);
                    lastTelemetryData = telemetryData;
                    //Thread.Sleep(SamplePeriod);
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
            sw.Stop();
            swData.Stop();
            IsConnected = false;
            IsRunning = false;
        }

        private TelemetryData ParseReponse(string resp)
        {
            TelemetryData telemetryData = new TelemetryData();

            string[] lines = resp.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            if (lines.Length >= 5)
            {


                var dict = new Dictionary<string, float>();
                // Todo: Einlesen in dictionary
                foreach (var line  in lines)
                {
                    var tmpLineItems = line.Split('=');
                    if(tmpLineItems.Length == 2)
                        dict.Add(tmpLineItems[0], float.Parse(tmpLineItems[1], CultureInfo.InvariantCulture));
                }

                if (dict.ContainsKey("time")) telemetryData.Time = dict["time"];
                if (dict.ContainsKey("airspeed")) telemetryData.AirSpeed = dict["airspeed"];
                if (dict.ContainsKey("altitude")) telemetryData.Altitude = dict["altitude"];
                if (dict.ContainsKey("vario")) telemetryData.Vario = dict["vario"];
                if (dict.ContainsKey("evario")) telemetryData.Evario = dict["evario"];
                if (dict.ContainsKey("nettovario")) telemetryData.Nettovario = dict["nettovario"];
                if (dict.ContainsKey("integrator")) telemetryData.Integrator = dict["integrator"];
                if (dict.ContainsKey("compass")) telemetryData.Compass = dict["compass"];
                if (dict.ContainsKey("slipball")) telemetryData.SlipBall = dict["slipball"];
                if (dict.ContainsKey("turnrate")) telemetryData.TurnRate = dict["turnrate"];
                if (dict.ContainsKey("yawstringangle")) telemetryData.YawStringAngle = dict["yawstringangle"];
                if (dict.ContainsKey("radiofrequency")) telemetryData.Radiofrequency = dict["radiofrequency"];
                if (dict.ContainsKey("yaw")) telemetryData.Yaw = dict["yaw"];
                if (dict.ContainsKey("pitch")) telemetryData.Pitch = dict["pitch"];
                if (dict.ContainsKey("bank")) telemetryData.Roll = dict["bank"];
                if (dict.ContainsKey("quaternionx")) telemetryData.Quaternionx = dict["quaternionx"];
                if (dict.ContainsKey("quaterniony")) telemetryData.Quaterniony = dict["quaterniony"];
                if (dict.ContainsKey("quaternionz")) telemetryData.Quaternionz = dict["quaternionz"];
                if (dict.ContainsKey("quaternionw")) telemetryData.Quaternionw = dict["quaternionw"];
                if (dict.ContainsKey("ax")) telemetryData.Surge = dict["ax"];
                if (dict.ContainsKey("ay")) telemetryData.Sway = dict["ay"];
                if (dict.ContainsKey("az")) telemetryData.Heave = dict["az"];
                if (dict.ContainsKey("vx")) telemetryData.SpeedX = dict["vx"];
                if (dict.ContainsKey("vy")) telemetryData.SpeedY = dict["vy"];
                if (dict.ContainsKey("vz")) telemetryData.SpeedZ = dict["vz"];
                if (dict.ContainsKey("rollrate")) telemetryData.RollRate = dict["rollrate"];
                if (dict.ContainsKey("pitchrate")) telemetryData.PitchRate = dict["pitchrate"];
                if (dict.ContainsKey("yawrate")) telemetryData.YawRate = dict["yawrate"];
                if (dict.ContainsKey("gforce")) telemetryData.Gforce = dict["gforce"];
                if (dict.ContainsKey("height")) telemetryData.Height = dict["height"];
                if (dict.ContainsKey("wheelheight")) telemetryData.Wheelheight = dict["wheelheight"];
                if (dict.ContainsKey("turbulencestrength")) telemetryData.Turbulencestrength = dict["turbulencestrength"];
                if (dict.ContainsKey("surfaceroughness")) telemetryData.Surfaceroughness = dict["surfaceroughness"];
                //if (dict.ContainsKey("hudmessages")) telemetryData.Hudmessages = dict["hudmessages"];


                // Surge alternative calculation
                if (dict.ContainsKey("ax") && dict.ContainsKey("airspeed") && dict.ContainsKey("time"))
                {
                    telemetryData.ElapsedMilliseconds = swData.ElapsedMilliseconds;
                    telemetryData.SurgeAlternative = ((dict["airspeed"] - lastTelemetryData.AirSpeed) / (telemetryData.ElapsedMilliseconds - lastTelemetryData.ElapsedMilliseconds) * 100) ;
                }
            }

            return telemetryData;
        }
    }
}