using System;
using System.Runtime.InteropServices;
using Math = System.Math;

namespace SimFeedback.telemetry
{
    // See https://www.condorsoaring.com/manual_en/#simkits-and-udp-outputs

    public struct TelemetryData
    {


        /* Sample package with extended log
            time=17.0253499342059
            airspeed=0.812818467617035
            altitude=501.60693359375
            vario=-2.16707849502563
            evario=-5.43059587478638
            nettovario=-6.46879529953003
            integrator=-9.5718240737915
            compass=132.906799316406
            slipball=-0.0295154601335526
            turnrate=5.21215806656983E-5
            yawstringangle=0.37086609005928
            radiofrequency=123.5
            yaw=2.31966114044189
            pitch=0.1672772616148
            bank=0.138154044747353
            quaternionx=0.0524225421249866
            quaterniony=0.0945194885134697
            quaternionz=0.923272669315338
            quaternionw=0.369042932987213
            ax=13.2301874160767
            ay=2.68185544013977
            az=0.504308462142944
            vx=0.00299414922483265
            vy=0.000755416869651526
            vz=-7.29845996829681E-5
            rollrate=-0.00998216681182384
            pitchrate=-0.00116933498065919
            yawrate=-5.48266996247548E-8
            gforce=1.12094616671221
            height=0.61981201171875
            wheelheight=-0.0360181517899036
            turbulencestrength=1.04095232486725
            surfaceroughness=0
            MC=0
            water=0

            // Sample package without extended data
        0   time=17.0123384582337
            airspeed=30.9783802032471
            altitude=797.574401855469
            vario=-1.06086719036102
            evario=-1.06253170967102
            nettovario=-0.336621403694153
            integrator=-1.07146024703979
            compass=149.594223022461
            slipball=-0.000419455405790359
            turnrate=-0.0704940482974052
        10  yawstringangle=-0.00415756786242127
            radiofrequency=123.5
            yaw=2.61091184616089
            pitch=0.085761547088623
            bank=-0.07082399725914
            quaternionx=0.0547523014247417
            quaterniony=-0.00940066296607256
            quaternionz=0.867230594158173
            quaternionw=0.494839400053024
            ax=1.42042005062103
        20  ay=0.608248353004456
            az=3.65034604072571
            vx=-12.5160303115845
            vy=-27.1460018157959
            vz=2.45487499237061
            rollrate=-0.0281974282115698
            pitchrate=-0.0304326992481947
            yawrate=0.0169996526092291
            gforce=1.38097426699328
         */

        // Note: all values are doubles with ‘.’ as decimal separator
        // * available only if ExtendedData1=1 in UDP.ini
        private double time;                 // in-game display time				decimal hours
        private double airspeed;             // was documented wrong...
        private double altitude;             // altimeter reading				m or ft according to units selected
        private double vario;                // pneumatic variometer reading		m/s
        private double evario;               // electronic variometer reading    m/s
        private double nettovario;           // netto variometer value			m/s
        private double integrator;           // integrator value					m/s
        private double compass;              // compass reading					degrees
        private double slipball;             // slip ball deflection angle       rad
        private double turnrate;             // turn indicator reading			rad/s
        private double yawstringangle;       // yawstring angle					rad
        private double radiofrequency;       // radio frequency					MHz
        private double yaw;                  // yaw								rad
        private double pitch;                // pitch						    rad
        private double bank;                 // bank								rad
        private double quaternionx;          // quaternion x						
        private double quaterniony;          // quaternion y						
        private double quaternionz;          // quaternion z						
        private double ax;                   // acceleration vector x		    m/s2
        private double ay;                   // acceleration vector y		    m/s2
        private double az;                   // acceleration vector z		    m/s2
        private double vx;                   // speed vector x					m/s
        private double vy;                   // speed vector y					m/s
        private double vz;                   // speed vector z					m/s
        private double rollrate;             // roll rate (local system)			rad/s
        private double pitchrate;            // pitch rate (local system) y		rad/s
        private double yawrate;              // yaw rate (local system) z		rad/s
        private double gforce;               // g forces							
        // Extra Values from here
        private double height;               // * height of cg above ground		m
        private double wheelheight;          // * height of wheel above ground	m
        private double turbulencestrength;   // * turbulence strength				
        private double surfaceroughness;     // * surface roughness					
        private double hudmessages;          // * HUD message text separated by ; | flaps ** flaps position index : 0=most negative to MAXFLAPS-1 | MC ** MacCready setting m/s | water ** Water ballast content kg

        // Own calculated values bases on upper values
        private double heave;
        private double sway;
        private double surge;

        #region For SimFeedback Available Values
        public double Time { get; set; }

        public double AirSpeed { get; set; }

        public double Pitch
        {
            get => LoopAngle(ConvertRadiansToDegrees(pitch),90);
            set => pitch = value;
        }

        public double Yaw
        {
            get => 180 - ConvertRadiansToDegrees(yaw);
            set => yaw = value;
        }

        public double Roll
        {
            get => LoopAngle(ConvertRadiansToDegrees(bank),90);
            set => bank = value;
        }

        public double Heave
        {
            get => ConvertAccel(heave);
            set => heave = value;
        }

        public double Sway
        {
            get => ConvertAccel(sway);
            set => sway = value;
        }

        public double Surge
        {
            get => ConvertAccel(surge);
            set => surge = value;
        }

        public double RollRate
        {
            get => ConvertRadiansToDegrees(rollrate);
            set => rollrate = value;
        }

        public double PitchRate
        {
            get => ConvertRadiansToDegrees(pitchrate);
            set => pitchrate = value;

        }

        public double YawRate
        {
            get => ConvertRadiansToDegrees(yawrate);
            set => yawrate = value;
        }

        public double TurnRate
        {
            get => ConvertRadiansToDegrees(turnrate);
            set => turnrate = value;
        }

        public double YawStringAngle
        {
            get => ConvertRadiansToDegrees(yawstringangle);
            set => yawstringangle = value;
        }

        public double SlipBall
        {
            get => ConvertRadiansToDegrees(slipball);
            set => slipball = value;
        }

        public double SlipBallAlternative
        {
            get => LoopAngle(ConvertRadiansToDegrees(slipball), 90);
            set => slipball = value;
        }

        public double PitchAlternative
        {
            get => (double)Math.Sin(pitch);
            set => pitch = value;
        }

        public double RollAlternative
        {
            get => (double)(Math.Cos(pitch) * Math.Sin(bank));
            set => pitch = value;
        }

        public double YawAlternative
        {
            get => (double)Math.Sin(yaw);
            set => yaw = value;
        }

        public double SurgeAlternative { get; set; }

        public double SpeedX { get; set; }
        public double SpeedY { get; set; }
        public double SpeedZ { get; set; }
        public double Altitude { get; set; }
        public double Vario { get; set; }
        public double Evario { get; set; }
        public double Nettovario { get; set; }
        public double Integrator { get; set; }
        public double Compass { get; set; }
        public double Radiofrequency { get; set; }
        public double Quaternionx { get; set; }
        public double Quaterniony { get; set; }
        public double Quaternionz { get; set; }
        public double Quaternionw { get; set; }
        public double Gforce { get; set; }
        public double Height { get; set; }
        public double Wheelheight { get; set; }
        public double Turbulencestrength { get; set; }
        public double Surfaceroughness { get; set; }
        public long ElapsedMilliseconds { get; set; }

        #endregion

        #region Conversion calculations
        private static double ConvertRadiansToDegrees(double radians)
        {
            var degrees = (double)(180 / Math.PI) * radians;
            return degrees;
        }

        private static double ConvertAccel(double accel)
        {
            return (double) (accel / 9.80665);
        }

        private double LoopAngle(double angle, double minMag)
        {

            double absAngle = Math.Abs(angle);

            if (absAngle <= minMag)
            {
                return angle;
            }

            double direction = angle / absAngle;

            //(180.0f * 1) - 135 = 45
            //(180.0f *-1) - -135 = -45
            double loopedAngle = (180.0f * direction) - angle;

            return loopedAngle;
        }
        #endregion
    }
}