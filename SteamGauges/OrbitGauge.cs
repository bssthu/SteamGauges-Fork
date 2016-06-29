using UnityEngine;
using KSP;
using KSP.IO;
using System;

namespace SteamGauges
{
    class OrbitGauge : Gauge
    {
        public bool showNegativePe;                                      //If true, the gauge will draw negative Periapsis values instead of just 0
        private double greenAlt = 1.2;                                   //This is the default height above the atmosphere at which the green light turns off
        private double circleThresh = 0.01;                              //Default circularization tolerance
        private int burnWindow = 30;                                     //The burn window is AP/PE within this many seconds
        private int MunSafe = 0;                                         //These store the values of the minimum safe orbital altitude for each airless body
        private int MinimusSafe = 0;
        private int MohoSafe = 0;
        private int GillySafe = 0;
        private int IkeSafe = 0;
        private int DresSafe = 0;
        private int VallSafe = 0;
        private int TyloSafe = 0;
        private int BopSafe = 0;
        private int PolSafe = 0;
        private int EelooSafe = 0;
        private int DefaultSafe = 0;                                     //The default safe altitude, in case they add a new, airless world
        private string next = "";

        public double getGreenAlt()
        {
            return greenAlt;
        }
        public double getCircleThresh()
        {
            return circleThresh;
        }
        public int getBurnWindow()
        {
            return burnWindow;
        }

        public void setGreenAlt(double v)
        {
            if (v < 0.9) v = 0.5;   //This seems way lower than anyone would want
            if (v > 5) v = 5;       //This seems way higher than anyone would want
            greenAlt = v;
        }
        public void setCircleThresh(double v)
        {
            if (v < 0) v = 0.0;    //Good luck getting these to be absolutely the same
            if (v > 5) v = 5;       //I don't think counts as a circle anymore
            circleThresh = v;
        }
        public void setBurnWindow(int v)
        {
            if (v < 0) v = 0;   //No negative windows, it already extends to both sides of the AP/PE
            //No absolute max, though one longer than the period doesn't make much sense.
            burnWindow = v;
        }

        //Draw if not minimized
        protected override bool isVisible()
        {
            return !this.isMinimized;
        }

        //Gauge specific actions
        protected override void GaugeActions()
        {
            // This gauge only draws stuff, no need to handle other events
            if (Event.current.type != EventType.repaint)
                return;
            //Draw the face (background)
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
            Orbit o = FlightGlobals.ActiveVessel.GetOrbit();
            //draw the inclination needle
            drawInclination(o);
            //Draw the AP, PE, and Period
            drawAPPEP(o);
            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0.5f, 0.5f, 0.5f));
            //Draw the casing (foreground)
            GUI.DrawTextureWithTexCoords(new Rect(-1f, -1f, 401f * Scale, 408f * Scale), texture, new Rect(0f, 0f, 0.499f, 0.499f));
            //Draw the Ap/Pe/An/Dn lights
            drawTimeLights();
            //Draw the AP/PE lights
            drawLights(o);
        }

        //This code draws the Apoapsis and Apoapsis lights on top of the gauge
        private void drawLights(Orbit o)
        {
            double ap = o.ApA;  //Apoapsis
            double pe = o.PeA;  //Periapsis
            //Which light, if any, do we draw?
            if (Mathf.Abs((float)(ap - pe)) < circleThresh * ap)     //Ap and Pe w/in 1%
            {   //Draw these first, as warnings should take precedance over circularization lights
                GUI.DrawTextureWithTexCoords(new Rect(167f * Scale, 100f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.73875f, 0.4386f, 0.0225f, 0.0209f));
                GUI.DrawTextureWithTexCoords(new Rect(167f * Scale, 172f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.73875f, 0.4386f, 0.0225f, 0.0209f));
            }
            double maxalt = 0;
            CelestialBody currentMainBody = FlightGlobals.currentMainBody;
            if (currentMainBody != null)
            {
                if (currentMainBody.atmosphere)   //There's an atmosphere, so we might be in it
                {
                    maxalt = currentMainBody.atmosphereDepth;
                }
                else  //There's no atmosphere, but we might still be close to the terrain
                {
                    //I was hoping to avoid this, but here are the hard coded altitudes
                    int maxLevel = 5000;
                    string name = currentMainBody.bodyName;
                    switch (name)
                    {
                        case "Mun":
                            maxLevel = MunSafe;
                            break;
                        case "Minmus":
                            maxLevel = MinimusSafe;
                            break;
                        case "Moho":
                            maxLevel = MohoSafe;
                            break;
                        case "Gilly":
                            maxLevel = GillySafe;
                            break;
                        case "Ike":
                            maxLevel = IkeSafe;
                            break;
                        case "Dres":
                            maxLevel = DresSafe;
                            break;
                        case "Vall":
                            maxLevel = VallSafe;
                            break;
                        case "Tylo":
                            maxLevel = VallSafe;
                            break;
                        case "Bop":
                            maxLevel = BopSafe;
                            break;
                        case "Pol":
                            maxLevel = PolSafe;
                            break;
                        case "Eeloo":
                            maxLevel = EelooSafe;
                            break;
                        default:
                            maxLevel = DefaultSafe;
                            break;
                    }
                }
            }
            //Apoapsis light
            if (ap < maxalt)                                    //Red light if AP is inside the atmosphere
            {
                GUI.DrawTextureWithTexCoords(new Rect(167f * Scale, 101f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.615f, 0.4386f, 0.0225f, 0.0209f));
            }
            else if (ap < (greenAlt * maxalt))                       //Green light from top of atmosphere to 20% above (Kerbin - 70k - 84k)
            {
                GUI.DrawTextureWithTexCoords(new Rect(167f * Scale, 101f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.5525f, 0.4386f, 0.0225f, 0.0209f));
            }
            if (o.eccentricity > 1.0)   //Escape trajectory
            {   //Yellow light is escape for Ap
                GUI.DrawTextureWithTexCoords(new Rect(167f * Scale, 101f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.6788f, 0.4386f, 0.0225f, 0.0209f));
            }
            //Periapsis light
            if (pe < 0)
            {   //Red light if Pe is inside the planet
                GUI.DrawTextureWithTexCoords(new Rect(166f * Scale, 171f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.615f, 0.4386f, 0.0225f, 0.0209f));
            }
            else if (pe < maxalt)
            {   //Yellow light if Pe is inside the atmosphere
                GUI.DrawTextureWithTexCoords(new Rect(167f * Scale, 172f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.6788f, 0.4386f, 0.0225f, 0.0209f));
            }
            else if (pe < (greenAlt * maxalt))
            {   //Green light if Pe is close the the atmosphere
                GUI.DrawTextureWithTexCoords(new Rect(167f * Scale, 172f * Scale, 18f * Scale, 17f * Scale), texture, new Rect(0.5525f, 0.4386f, 0.0225f, 0.0209f));
            }

            //Burn window lights.  These lights illuminate w/in burnWindow seconds of AP/PE
            //AP burn window
            if ((o.timeToAp <= burnWindow) || ((o.period - o.timeToAp) < burnWindow))
            {
                GUI.DrawTextureWithTexCoords(new Rect(190f * Scale, 137f * Scale, 11f * Scale, 15f * Scale), texture, new Rect(0.5575f, 0.3993f, 0.01375f, 0.018473f));//446,489
            }
            //PE
            if ((o.timeToPe <= burnWindow) || ((o.period - o.timeToPe) < burnWindow))
            {
                GUI.DrawTextureWithTexCoords(new Rect(190f * Scale, 205f * Scale, 11f * Scale, 15f * Scale), texture, new Rect(0.5575f, 0.3993f, 0.01375f, 0.018473f));//446,489
            }
        }

        //Draws the green Ap Pe An or Dn lights as appropriate
        private void drawTimeLights()
        {
            switch (next)
            {
                case "pe":
                    //Debug.Log("Periapsis next.");
                    GUI.DrawTextureWithTexCoords(new Rect(139f * Scale, 237f * Scale, 28f * Scale, 23f * Scale), texture, new Rect(0.5938f, 0.3563f, 0.035f, 0.0283f));
                    break;
                case "ap":
                    //Debug.Log("Apoapsis next.");
                    GUI.DrawTextureWithTexCoords(new Rect(110f * Scale, 237f * Scale, 28f * Scale, 23f * Scale), texture, new Rect(0.5575f, 0.3563f, 0.035f, 0.0283f));
                    break;
                case "an":
                    //Debug.Log("Ascending node next.");
                    GUI.DrawTextureWithTexCoords(new Rect(171f * Scale, 237f * Scale, 28f * Scale, 23f * Scale), texture, new Rect(0.6338f, 0.3563f, 0.035f, 0.0283f));
                    break;
                case "dn":
                    //Debug.Log("Descending node next.");
                    GUI.DrawTextureWithTexCoords(new Rect(203f * Scale, 237f * Scale, 29f * Scale, 23f * Scale), texture, new Rect(0.6738f, 0.3563f, 0.03625f, 0.0283f));
                    break;
                default:
                    Debug.Log("Error, no time found as next.");
                    break;
            }
        }
        //This code draws the Apoapis, Periapsis, and Period digits onto the gauge
        private void drawAPPEP(Orbit o)
        {
            double ap = o.ApA;
            if (o.eccentricity > 1.0) ap = 0;   //Escape trajectory
            drawDigits(181f, 132f, ap);  //Draw the Apoapsis value
            double pe = o.PeA;
            if (!showNegativePe && pe < 0) pe = 0;  //Don't draw negative values unless requested
            drawDigits(181f, 199f, pe);  //Draw the Periapsis value

            //Time to next of: Ap, Pe, An, Dn, SoI
            double apt = Math.Abs(o.timeToAp);
            double pet = Math.Abs(o.timeToPe);
            double ant = double.MaxValue;
            double dnt = double.MaxValue;
            ITargetable tar = FlightGlobals.fetch.VesselTarget;
            if (tar != null && tar.GetOrbit() != null)
            {
                //This should prevent a few errors
                try
                {
                    ant = TimeOfAscendingNode(o, tar.GetOrbit(), Planetarium.fetch.time) - Planetarium.fetch.time;
                    dnt = TimeOfDescendingNode(o, tar.GetOrbit(), Planetarium.fetch.time) - Planetarium.fetch.time;
                }
                catch { }
            }

            //Parabolic or hyperbolic orbits don't have an apoapsis
            if (o.eccentricity > 1.0) apt = pet;
            //Also draw label on gauge
            double seconds = double.MaxValue;
            if (pet < seconds)
            {
                seconds = pet;
                next = "pe";
            }
            if (apt < seconds)
            {
                seconds = apt;
                next = "ap";
            }
            if (ant >= 0 && ant < seconds)
            {
                seconds = ant;
                next = "an";
            }
            if (dnt >= 0 && dnt < seconds)
            {
                seconds = dnt;
                next = "dn";
            }

            double sec_fraction = seconds;
            int s = (int)Math.Round(sec_fraction);
            sec_fraction -= s;
            int minutes = s/60;
            s -= minutes*60;
            int hours = minutes/60;
            minutes -= hours*60;
            float width = Resources.digits.width;
            float height = Resources.digits.height / 11;
            int x = minutes % 10;
            GUI.DrawTextureWithTexCoords(new Rect(150f * Scale, 261f * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = minutes / 10;
            GUI.DrawTextureWithTexCoords(new Rect(130f * Scale, 261f * Scale, width * Scale, height * Scale), Resources.digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
            x = hours % 10;
            GUI.DrawTextureWithTexCoords(new Rect(100f * Scale, 261f * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = hours / 10;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect(80f * Scale, 261f * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = hours / 100;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect(60f * Scale, 261f * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            //Seconds
            //109, 132
            double fx = s % 10 + sec_fraction;
            GUI.DrawTextureWithTexCoords(new Rect(198f * Scale, 261f * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(fx * 0.091), 1f, 0.091f));
            x = s / 10;
            GUI.DrawTextureWithTexCoords(new Rect(178f * Scale, 261f * Scale, width * Scale, height * Scale), Resources.digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
        }

        //Draws a formatted 5 digit number with an 'm', 'K', or 'M' postfix, or 4 digit negative number
        private void drawDigits(float right, float top, double value)
        {
            if (double.IsNaN(value))
            {
                return;
            }

            float char_width = Resources.orbit_chars.width;
            float char_height = Resources.orbit_chars.height / 3f;
            float output = 0;
            //Below 100,000 m, display raw value and 'm'
            if (value < 100000 && value > -10000)
            {
                output = (float)value;
                GUI.DrawTextureWithTexCoords(new Rect((right - char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.orbit_chars, new Rect(0f, 0f, 1f, 0.33333f));
            }
            //Below 100,000 k, display kilometers and 'K'
            else if (value < 100000000 && value > -10000000)
            {
                output = (float)(value / 1000f);
                GUI.DrawTextureWithTexCoords(new Rect((right - char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.orbit_chars, new Rect(0f, 0.333f, 1f, 0.33333f));
            }
            else //Display megameters and 'M'
            {
                output = (float)(value / 1000000f);
                GUI.DrawTextureWithTexCoords(new Rect((right - char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.orbit_chars, new Rect(0f, 0.6666f, 1f, 0.33333f));
            }
            //Now draw each digit of our output, starting from the right
            for (int i = 1; i < 6; i++)
            {
                if (i == 5 && value < 0)
                {
                    GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * (i + 1))) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.minus, new Rect(0f, 0f, 1f, 1f));
                }
                else
                {
                    int divisor = (int)Mathf.Pow(10f, (i - 1));
                    int x = Math.Abs((int)(output / divisor));
                    x = x % 10;
                    float y = ((float)x * 0.091f);
                    GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * (i + 1))) * Scale, top * Scale, char_width * Scale, 28 * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
                }
            }
        }

        //this code draws the Inclination data to the gauge
        private void drawInclination(Orbit o)
        {
            //90 degrees of inclination and 90 degrees of gauge...convinient
            Vector2 pivotPoint = new Vector2(200 * Scale, 203 * Scale);    //Center of the case
            GUIUtility.RotateAroundPivot((float)(-1f * o.inclination), pivotPoint);  //moves the needle up instead of down
            GUI.DrawTextureWithTexCoords(new Rect(313f * Scale, 199f * Scale, 27f * Scale, 9f * Scale), texture, new Rect(0.6125f, 0.4030f, 0.03375f, 0.01106f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }

        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("OrbitPosition");
            isMinimized = config.GetValue<bool>("OrbitMinimized", true);
            showNegativePe = config.GetValue<bool>("OrbitNegativePe", true);
            greenAlt = config.GetValue<double>("OrbitGreenAlt", 1.2);
            circleThresh = config.GetValue<double>("OrbitCircleThresh", 0.1);
            Scale = (float)config.GetValue<double>("OribtScale", 0.5);
            burnWindow = config.GetValue<int>("OrbitWindow", 30);
            MunSafe = config.GetValue<int>("MunSafe", 7100);
            MinimusSafe = config.GetValue<int>("MinimusSafe", 5750);
            MohoSafe = config.GetValue<int>("MohoSafe", 6850);
            GillySafe = config.GetValue<int>("GillySafe", 6400);
            IkeSafe = config.GetValue<int>("IkeSafe", 12750);
            DresSafe = config.GetValue<int>("DresSafe", 5700);
            VallSafe = config.GetValue<int>("VallSafe", 8000);
            TyloSafe = config.GetValue<int>("TyloSafe", 11300);
            BopSafe = config.GetValue<int>("BopSafe", 21800);
            PolSafe = config.GetValue<int>("PolSafe", 5600);
            EelooSafe = config.GetValue<int>("EelooSafe", 3900);
            DefaultSafe = config.GetValue<int>("DefaultSafe", 7000);
        }

        public override void save(PluginConfiguration config)
        {
            config.SetValue("OrbitPosition", windowPosition);
            config.SetValue("OrbitMinimized", isMinimized);
            config.SetValue("OrbitNegativePe", showNegativePe);
            config.SetValue("OrbitGreenAlt", greenAlt);
            config.SetValue("OrbitCircleThresh", circleThresh);
            config.SetValue("OribtScale", (double)Scale);
            config.SetValue("OrbitWindow", burnWindow);
            config.SetValue("MunSafe", MunSafe);
            config.SetValue("MinmusSafe", MinimusSafe);
            config.SetValue("MohoSafe", MohoSafe);
            config.SetValue("GillySafe", GillySafe);
            config.SetValue("IkeSafe", IkeSafe);
            config.SetValue("DresSafe", DresSafe);
            config.SetValue("VallSafe", VallSafe);
            config.SetValue("TyloSafe", TyloSafe);
            config.SetValue("BopSafe", BopSafe);
            config.SetValue("PolSafe", PolSafe);
            config.SetValue("EelooSafe", EelooSafe);
            config.SetValue("DefaultSafe", DefaultSafe);
        }

        //Lots of MechJeb code for time to Ascending/Descending nodes

        private static double ClampRadiansTwoPi(double angle)
        {
            angle = angle % (2 * Math.PI);
            if (angle < 0) return angle + 2 * Math.PI;
            else return angle;
        }

        //can probably be replaced with Vector3d.xzy?
        public static Vector3d SwapYZ(Vector3d v)
        {
            return v.xzy;
            //return v.Reorder(132);
        }

        //normalized vector perpendicular to the orbital plane
        //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
        public static Vector3d SwappedOrbitNormal(Orbit o)
        {
            return -SwapYZ(o.GetOrbitNormal()).normalized;
        }


        //Gives the true anomaly (in a's orbit) at which a crosses its ascending node 
        //with b's orbit.
        //The returned value is always between 0 and 360.
        public static double AscendingNodeTrueAnomaly(Orbit a, Orbit b)
        {
            Vector3d vectorToAN = Vector3d.Cross(SwappedOrbitNormal(a), SwappedOrbitNormal(b));
            return TrueAnomalyFromVector(a, vectorToAN);
        }

        //Converts a direction, specified by a Vector3d, into a true anomaly.
        //The vector is projected into the orbital plane and then the true anomaly is
        //computed as the angle this vector makes with the vector pointing to the periapsis.
        //The returned value is always between 0 and 360.
        public static double TrueAnomalyFromVector(Orbit o, Vector3d vec)
        {
            Vector3d projected = Vector3d.Exclude(SwappedOrbitNormal(o), vec);
            Vector3d vectorToPe = SwapYZ(o.eccVec);
            double angleFromPe = Math.Abs(Vector3d.Angle(vectorToPe, projected));


            //If the vector points to the infalling part of the orbit then we need to do 360 minus the
            //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
            //orbit normal and vector to the periapsis. This gives a vector that points to center of the 
            //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the orbit.
            if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(SwappedOrbitNormal(o), vectorToPe))) < 90)
            {
                return angleFromPe;
            }
            else
            {
                return 360 - angleFromPe;
            }
        }

        //Originally by Zool, revised by The_Duck
        //Converts an eccentric anomaly into a mean anomaly.
        //For an elliptical orbit, the returned value is between 0 and 2pi
        //For a hyperbolic orbit, the returned value is any number
        public static double GetMeanAnomalyAtEccentricAnomaly(Orbit o, double E)
        {
            double e = o.eccentricity;
            if (e < 1) //elliptical orbits
            {
                return ClampRadiansTwoPi(E - (e * Math.Sin(E)));
            }
            else //hyperbolic orbits
            {
                return (e * Math.Sinh(E)) - E;
            }
        }

        //Originally by Zool, revised by The_Duck
        //Converts a true anomaly into an eccentric anomaly.
        //For elliptical orbits this returns a value between 0 and 2pi
        //For hyperbolic orbits the returned value can be any number.
        //NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then an ArgumentException is thrown
        public static double GetEccentricAnomalyAtTrueAnomaly(Orbit o, double trueAnomaly)
        {
            double e = o.eccentricity;
            while (trueAnomaly > 360) trueAnomaly -= 360;
            //trueAnomaly = MuUtils.ClampDegrees360(trueAnomaly);
            trueAnomaly = trueAnomaly * (Math.PI / 180);


            if (e < 1) //elliptical orbits
            {
                double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                double sinE = Math.Sqrt(1 - (cosE * cosE));
                if (trueAnomaly > Math.PI) sinE *= -1;


                return ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            }
            else  //hyperbolic orbits
            {
                double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                if (coshE < 1) throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by orbit with eccentricity " + o.eccentricity);


                double E = Math.Log(coshE + Math.Sqrt(coshE * coshE - 1));
                if (trueAnomaly > Math.PI) E *= -1;


                return E;
            }
        }

        //position relative to the primary
        public static Vector3d SwappedRelativePositionAtUT(Orbit o, double UT)
        {
            return SwapYZ(o.getRelativePositionAtUT(UT));
        }


        //position in world space
        public static Vector3d SwappedAbsolutePositionAtUT(Orbit o, double UT)
        {
            return o.referenceBody.position + SwappedRelativePositionAtUT(o, UT);
        }


        //distance between two orbiting objects at a given time
        public static double Separation(Orbit a, Orbit b, double UT)
        {
            return (SwappedAbsolutePositionAtUT(a, UT) - SwappedAbsolutePositionAtUT(b, UT)).magnitude;
        }

        //mean motion is rate of increase of the mean anomaly
        public static double MeanMotion(Orbit o)
        {
            return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
        }


        //The mean anomaly of the orbit.
        //For elliptical orbits, the value return is always between 0 and 2pi
        //For hyperbolic orbits, the value can be any number.
        public static double MeanAnomalyAtUT(Orbit o, double UT)
        {
            double ret = o.meanAnomalyAtEpoch + MeanMotion(o) * (UT - o.epoch);
            if (o.eccentricity < 1) ret = ClampRadiansTwoPi(ret);
            return ret;
        }


        //The next time at which the orbiting object will reach the given mean anomaly.
        //For elliptical orbits, this will be a time between UT and UT + o.period
        //For hyperbolic orbits, this can be any time, including a time in the past, if
        //the given mean anomaly occurred in the past
        public static double UTAtMeanAnomaly(Orbit o, double meanAnomaly, double UT)
        {
            double currentMeanAnomaly = MeanAnomalyAtUT(o, UT);
            double meanDifference = meanAnomaly - currentMeanAnomaly;
            if (o.eccentricity < 1) meanDifference = ClampRadiansTwoPi(meanDifference);
            return UT + meanDifference / MeanMotion(o);
        }


        //NOTE: this function can throw an ArgumentException, if o is a hyperbolic orbit with an eccentricity
        //large enough that it never attains the given true anomaly
        public static double TimeOfTrueAnomaly(Orbit o, double trueAnomaly, double UT)
        {
            return UTAtMeanAnomaly(o, GetMeanAnomalyAtEccentricAnomaly(o, GetEccentricAnomalyAtTrueAnomaly(o, trueAnomaly)), UT);
        }


        //Gives the true anomaly (in a's orbit) at which a crosses its descending node 
        //with b's orbit.
        //The returned value is always between 0 and 360.
        public static double DescendingNodeTrueAnomaly(Orbit a, Orbit b)
        {
            double ans = AscendingNodeTrueAnomaly(a, b) + 180;
            while (ans > 360) ans -= 360;
            return ans;
            //return MuUtils.ClampDegrees360(AscendingNodeTrueAnomaly(a, b) + 180);
        }

        //Returns the next time at which a will cross its ascending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if 
        //the ascending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "ascending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfAscendingNode(Orbit a, Orbit b, double UT)
        {
            return TimeOfTrueAnomaly(a, AscendingNodeTrueAnomaly(a, b), UT);
        }


        //Returns the next time at which a will cross its descending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if 
        //the descending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "descending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfDescendingNode(Orbit a, Orbit b, double UT)
        {
            return TimeOfTrueAnomaly(a, DescendingNodeTrueAnomaly(a, b), UT);
        }

    }
}
