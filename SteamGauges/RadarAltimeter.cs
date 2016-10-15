//ufo
using UnityEngine;
using KSP.IO;
using System;

namespace SteamGauges
{
    class RadarAltimeter : Gauge
    {
        private int greenLight = 1000;                                 //Green light comes on below this altitude
        private int yellowLight = 100;                                 //Yellow light comes on below this altitude
        private int redLight = 10;                                     //Red light comes on below this altitude
        private bool auto_burn;                                        //If true, the RA will automatically start a burn at the computed suicide altitude
        private bool contact_stop;                                     //If true, the RA will automatically stop a burn at ground level
        public int contact_alt;                                        //Altitude at which the contact burn cuts the engines
        private bool burning = false;                                  //State variable to determine if burning should be stoped at ground level
        public int calibration { get; set; }                           //Calibrates the gauge to read 0 when landed
        private long ra;                                               //Might as well just store this instead of passing it everywhere
        private bool first;

        public override string getTextureName() { return "ra"; }
        public override string getTooltipName() { return "Radar Altimeter"; }

        public int getRedLight()
        {
            return redLight;
        }
        public int getYellowLight()
        {
            return yellowLight;
        }
        public int getGreenLight()
        {
            return greenLight;
        }
        public void setRed(int v)
        {
            if (v < 0) v = 0;
            if (v > 20000) v = 0;
            redLight = v;
        }
        public void setYellow(int v)
        {
            if (v < 0) v = 0;
            if (v > 20000) v = 0;
            yellowLight = v;
        }
        public void setGreen(int v)
        {
            if (v < 0) v = 0;
            if (v > 20000) v = 0;
            greenLight = v;
        }

        //Gauge specific initialization, especially buttons
        public RadarAltimeter()
        {
            //Scale = 0.5f;
            //last5 = new long[5];
            GaugeButton b = new GaugeButton();
            b.active = false;
            b.permPosition = new Rect(124f, 243f, 60f, 47f);
            b.offTexture = new Rect(0.5275f, 0.4265f, 0.075f, 0.0576f);  //Off toggle
            b.onTexture = new Rect(0.5275f, 0.3738f, 0.075f, 0.0576f);   //On toggle
            buttons = new GaugeButton[2];
            buttons[0] = b;
            b = new GaugeButton();
            b.active = false;
            b.permPosition = new Rect(216f, 243f, 60f, 47f);
            b.offTexture = new Rect(0.5275f, 0.3211f, 0.075f, 0.0576f);  //Off toggle
            b.onTexture = new Rect(0.5275f, 0.2696f, 0.075f, 0.0576f);   //On toggle
            buttons[1] = b;
            first = true;   //On first run, set buttons to saved configuration
            ra = 5500;
            useDrawButtons = false; //Let me draw my own buttons!
        }

        //Draw if not minimized and below 25,000 m
        protected override bool isVisible()
        {
            //Compute current AGL altitude
            //double terrain = FlightGlobals.ActiveVessel.terrainAltitude;
            //if (terrain < 0) terrain = 0;   //Fixes RA over the ocean.
            //ra = (long)(FlightGlobals.ActiveVessel.mainBody.GetAltitude(FlightGlobals.ActiveVessel.CoM) - terrain);
            ra = SteamShip.RA - calibration;
            if (ra < 0) ra = 0; //Can't over calibrate
            if (this.isMinimized) return false;
            if (ra > 5500) return false; //don't do anything if we're too high
            return true;
        }

        protected override void GaugeActions()
        {
            if (first)
            {
                first = false;
                buttons[0].active = auto_burn;
                buttons[1].active = contact_stop;
            }
            //Get button states
            auto_burn = buttons[0].active;
            contact_stop = buttons[1].active;
            // This gauge only draws stuff, no need to handle other events
            if (Event.current.type != EventType.repaint)
                return;
            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, base_width * Scale, base_height * Scale), texture, new Rect(0f, 0.5f, 0.5f, 0.5f));
            //Draw the face
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, base_width * Scale, base_height * Scale), texture, new Rect(0f, 0f, 0.5f, 0.5f));
            //Draw the Time to Impact
            //drawTTI();
            //needle_pos = suicideNeedle(suicideBurn());
            //GUI.DrawTextureWithTexCoords(needle_pos, texture, new Rect(0.8568f, 0.7816f, 0.1347f, 0.0134f));
            //Draw the warning light
            //drawLight((long)ra);

            if (auto_burn)
            {
                GUI.DrawTextureWithTexCoords(buttons[0].position, texture, buttons[0].onTexture);  //Green On Light
            }
            else
            {
                GUI.DrawTextureWithTexCoords(buttons[0].position, texture, buttons[0].offTexture);  //Grey Off light
            }

            if (contact_stop)
            {
                GUI.DrawTextureWithTexCoords(buttons[1].position, texture, buttons[1].onTexture);  //Green On Light
            }
            else
            {
                GUI.DrawTextureWithTexCoords(buttons[1].position, texture, buttons[1].offTexture);  //Grey Off light
            }
            //Draw the RA needle
            rotateNeedle(ra, true);
            //Rect needle_pos = moveNeedle((long)ra);
            //GUI.DrawTextureWithTexCoords(needle_pos, texture, new Rect(0.8568f, 0.8009f, 0.1347f, 0.0148f));
            //Draw the suicide needle
            rotateNeedle(suicideBurn(), false);
            //Draw the casing
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, base_width * Scale, base_height * Scale), texture, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
        }

        //Rotates and draws the radar altimeter needle for the round gauge
        //If height_needle is true, draws the central indicator needle
        //If false, draws the suicide carrot
        private void rotateNeedle(long alt, bool height_needle)
        {
            double rot = 0;
            if (alt > 5000 || alt == -1) rot = 350;
            else if (alt < 400)
            {
                rot = alt * 0.3535;
            }
            else if (alt < 500)
            {
                rot = (alt - 400) * 0.385;
                rot += 141.5;
            }
            else if (alt < 1000)
            {
                rot = (alt - 500) * 0.18;
                rot += 180;
            }
            else if (alt < 2000)
            {
                rot = (alt - 1000) * 0.0151;
                rot += 270;
            }
            else   //alt <= 5000
            {
                rot = (alt - 2000) * 0.01663;
                rot += 285.1;
            }
            Vector2 pivotPoint = new Vector2(200 * Scale, 204 * Scale);    //Center of gauge
            GUIUtility.RotateAroundPivot((float)rot, pivotPoint);
            if (height_needle)
                GUI.DrawTextureWithTexCoords(new Rect(170f * Scale, 62f * Scale, 61f * Scale, 168f * Scale), texture, new Rect(0.7113f, 0.2169f, 0.0763f, 0.2059f));   //Large needle
            else
                GUI.DrawTextureWithTexCoords(new Rect(192f * Scale, 43f * Scale, 16f * Scale, 13f * Scale), texture, new Rect(0.7388f, 0.4387f, 0.0200f, 0.0159f));   //Suicide carrot
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }

        //Computes and displays time to impact the the vessel is
        //descending towards a body with no atmosphere.
        private void drawTTI()
        {
            /*Vessel ship = FlightGlobals.ActiveVessel;
            double vertspeed = FlightGlobals.ActiveVessel.verticalSpeed;
            if (!ship.Landed && vertspeed < 0 && ship.orbit.PeA <= 0 && !ship.mainBody.atmosphere)
            {
                double g = FlightGlobals.getGeeForceAtPosition(ship.CoM).magnitude;
                double Vf = Math.Sqrt((vertspeed * vertspeed) + 2 * ra * g);
                //t = 2d/(Vi+Vf)
                double tti = (Math.Abs(Vf) - Math.Abs(vertspeed)) / g;
                tti++;  //So you hit the ground at 0, not 1 second after 0
                //if (ra <= 0) tti = 0;
                int min = (int) (tti / 60);
                int sec = (int)(tti % 60);
                drawTime(95f, 627f, tti);
                return;
            } */
            drawTime(95f, 627f, SteamShip.TTI); //That's all that's left with SteamShip integration...
        }

        //Calculates and returns suicide altitude using Hetsin's equations
        private long suicideBurn()
        {
            /*Vessel v = FlightGlobals.ActiveVessel;
            if (v.Landed || v.orbit.PeA > 0 || v.mainBody.atmosphere) return -1;  //Not calculating this
            long sa = 0;
            double avgG = v.mainBody.gravParameter / ((v.mainBody.Radius + FlightGlobals.ActiveVessel.terrainAltitude) * (v.mainBody.Radius + FlightGlobals.ActiveVessel.terrainAltitude));
            //Debug.Log("Srf G: "+Math.Round(avgG, 2));
            avgG += FlightGlobals.getGeeForceAtPosition(v.CoM).magnitude;
            avgG /= 2;
            //Debug.Log("Curr G: "+Math.Round(FlightGlobals.getGeeForceAtPosition(v.CoM).magnitude, 2));
            //Debug.Log("Avg G: "+Math.Round(avgG,2));
            double vdv = Math.Sqrt((2 * avgG * ra) + (v.verticalSpeed*v.verticalSpeed));
            //Debug.Log("Vert Dv: " + Math.Round(vdv, 2)+"m/s");
            //Use mass and max available thrust
            double mass = 0;
            double max_thrust = 0;
            double isp = 0;
            foreach (Part P in v.parts)
            {
                //Vessel Mass
                if (P.physicalSignificance == Part.PhysicalSignificance.FULL)
                    mass += P.mass;
                mass += P.GetResourceMass();
                //Module loop
                foreach (PartModule pm in P.Modules)
                {
                    if (pm is ModuleEngines)
                    {
                        ModuleEngines ME = pm as ModuleEngines;
                        //Thrust
                        //if (ME.EngineIgnited && ME.isEnabled && !ME.getFlameoutState)
                        if (ME.isOperational)
                        {
                            isp += ME.maxThrust / ME.atmosphereCurve.Evaluate((float)v.staticPressure);
                            max_thrust += ME.maxThrust;
                        }
                    }
                }
            }
            isp = max_thrust / isp; //Weighted average of active engine ISP's
            //Debug.Log("Current Mass: " + Math.Round(mass,2) + "t Thrust: " + max_thrust+"kn ISP: "+Math.Round(isp, 2)+"s");
            //Altitude Fraction = (Vertical dv ^2) / (2 * 1000 * Thrust (kN))
            double altFrac = (vdv * vdv) / (2 * 1000 * max_thrust);
            //Debug.Log("Altitude Fraction: " + Math.Round(altFrac, 2));
            //m-avg = (m0 + (m0 / e ^ (dv / (Isp * 9.82)))) / 2
            //double avgMass = (mass + (mass / Math.Pow(Math.E, (vdv / isp * 9.82)))) / 2;
            double avgMass = (mass / Math.Pow(Math.E, (vdv / (isp * 9.82))));
            //Debug.Log("Final Mass: " + Math.Round(avgMass, 2)+"t");
            avgMass = (mass + avgMass) / 2;
            //Debug.Log("Avg Mass: " + Math.Round(avgMass, 2) + "t");
            sa = (long)Math.Round(altFrac * avgMass*1000);
            //Debug.Log("SA: " + sa+"m");
            */
            //if (!auto_burn)
            //burning = false;
            if (auto_burn && !burning && ra <= (SteamShip.SA + 10) && FlightGlobals.ActiveVessel.verticalSpeed <= 0 && ra > 1)
            {
                burning = true;
                auto_burn = false;
                buttons[0].active = false;
                FlightInputHandler.state.mainThrottle = 1.0f;
                Debug.Log("Auto burn initiated!");
            }

            //Contact-Stop calculations
            if (contact_stop && ra <= contact_alt)
            {
                burning = false;
                contact_stop = false;
                buttons[1].active = false;
                FlightInputHandler.state.mainThrottle = 0f;
                Debug.Log("Contact! Burn stopped!");
            }
            if (contact_stop && burning && FlightGlobals.ActiveVessel.verticalSpeed > 2)
            {
                burning = false;
                contact_stop = false;
                auto_burn = false;
                buttons[1].active = false;
                FlightInputHandler.state.mainThrottle = 0f;
                Debug.Log("Climbing! Burn stopped!");
            }
            return SteamShip.SA;
        }


        //Returns a rectangle for the location of the suicide burn needle.
        private Rect suicideNeedle(long sa)
        {
            //Rectangles go left, top, width, height
            int nh = 10;   //dynamic is best, even though most of this is static
            int nw = 64;
            int bh = 673;
            int bw = 200;
            int lines = 33;
            //Ok, so this part won't be as dynamic, because I'm not quite pegging the needle
            bh -= 34;  //take out the border
            bw -= 17;
            //place along the left side boarder
            float left = 18;
            float pix = 600 / lines;    //should be dynamic, but my lines texture is currently the same size as the background
            Vessel active = FlightGlobals.ActiveVessel;
            if (active == null) return new Rect(left * Scale, 337 * Scale, nw * Scale, nh * Scale); //Peg the needle at the bottom
            if (sa < 0) sa = 0; //Helps, hopefully
            if (sa > 21000)     //peg at the top
            {
                float top = 27;
                //draw an "OFF" flag here?
                return new Rect(left * Scale, top * Scale - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (sa > 15000)    //20-15k
            {
                //37 pixels, 2 blocks
                float m = sa - 15000;
                float percent = m / 5000f;
                percent *= 37;
                float top = 73f - percent;
                return new Rect(left * Scale, top * Scale - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (sa > 3000) //15-3k
            {
                //218 pixels, 12 blocks
                float m = sa - 3000;
                float percent = m / 12000f;
                percent *= 218;
                float top = 291f - percent;
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (sa > 1000) //3k-1k
            {
                //73 pixels, 4 blocks
                float m = sa - 1000;
                float percent = m / 2000f;
                percent *= 73;
                float top = 365f - percent;
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (sa > 700)  //700-1000
            {
                //37 pixels, 2 blocks
                float m = sa - 700;
                float percent = m / 300f;
                percent *= 37;
                float top = 400f - percent;
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (sa > 100) //700-100
            {
                //109 pixels, 6 blocks
                float m = sa - 100;           //silly here, I know
                float percent = m / 600f;    //what percentage of this stage am I at?
                percent *= 109;              //Now how many pixels is that
                float top = 509f - percent; //How many pixels is that from the top of the window?
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (sa > 50) //100-50
            {
                //37 pixels, 2 blocks
                float m = sa - 50;           //change range to 0-50
                float percent = m / 50f;    //what percentage of this stage am I at?
                percent *= 37;              //Now how many pixels is that
                float top = 545f - percent; //How many pixels is that from the top of the window?
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (sa >= 0)  // 50 - 0
            {
                //91 pixels, 50 m
                //Subtract the min value from the reading, should yield a number from range-0
                float m = sa - 0;           //silly here, I know
                float percent = m / 50f;    //what percentage of this stage am I at?
                percent *= 91;              //Now how many pixels is that
                float top = 637f - percent; //How many pixels is that from the top of the window?
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else return new Rect(left * Scale, 337 * Scale, nw * Scale, nh * Scale); //peg it at the bottom
        }

        //Draws the altitude warning light
        private void drawLight(long ra)
        {
            Vessel active = FlightGlobals.ActiveVessel;
            if (ra < redLight)
            {   //475x673
                GUI.DrawTextureWithTexCoords(new Rect(21f * Scale, 28f * Scale, 17f * Scale, 17f * Scale), texture, new Rect(0.8758f, 0.9212f, .0379f, 0.0253f));  //419,52
            }
            else if (ra < yellowLight)
            {
                GUI.DrawTextureWithTexCoords(new Rect(21f * Scale, 28f * Scale, 17f * Scale, 17f * Scale), texture, new Rect(0.8758f, 0.8900f, .0379f, 0.0253f));
            }
            else if (ra < greenLight)
            {
                GUI.DrawTextureWithTexCoords(new Rect(21f * Scale, 28f * Scale, 17f * Scale, 17f * Scale), texture, new Rect(0.8758f, 0.9539f, .0379f, 0.0253f));
            }
        }

        //Returns a rectangle for the position to draw the altimeter needle to
        private Rect moveNeedle(long ra)
        {
            //Rectangles go left, top, width, height
            int nh = 10;   //dynamic is best, even though most of this is static
            int nw = 64;
            int bh = 673;
            int bw = 200;
            int lines = 33;
            //Ok, so this part won't be as dynamic, because I'm not quite pegging the needle
            bh -= 34;  //take out the border
            bw -= 17;
            //place along the right side boarder
            float left = bw - nw;
            float pix = 600 / lines;    //should be dynamic, but my lines texture is currently the same size as the background
            Vessel active = FlightGlobals.ActiveVessel;
            if (active == null) return new Rect(left * Scale, 27f * Scale - ((nh / 2) * Scale), nw * Scale, nh * Scale); //draw an "OFF" flag here?
            if (ra < 0) ra = 0; //Helps, hopefully
            if (ra > 21000)     //peg at the top
            {
                float top = 27;
                //draw an "OFF" flag here?
                return new Rect(left * Scale, top * Scale - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (ra > 15000)    //20-15k
            {
                //37 pixels, 2 blocks
                float m = ra - 15000;
                float percent = m / 5000f;
                percent *= 37;
                float top = 73f - percent;
                return new Rect(left * Scale, top * Scale - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (ra > 3000) //15-3k
            {
                //218 pixels, 12 blocks
                float m = ra - 3000;
                float percent = m / 12000f;
                percent *= 218;
                float top = 291f - percent;
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (ra > 1000) //3k-1k
            {
                //73 pixels, 4 blocks
                float m = ra - 1000;
                float percent = m / 2000f;
                percent *= 73;
                float top = 365f - percent;
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (ra > 700)  //700-1000
            {
                //37 pixels, 2 blocks
                float m = ra - 700;
                float percent = m / 300f;
                percent *= 37;
                float top = 400f - percent;
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (ra > 100) //700-100
            {
                //109 pixels, 6 blocks
                float m = ra - 100;           //silly here, I know
                float percent = m / 600f;    //what percentage of this stage am I at?
                percent *= 109;              //Now how many pixels is that
                float top = 509f - percent; //How many pixels is that from the top of the window?
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (ra > 50) //100-50
            {
                //37 pixels, 2 blocks
                float m = ra - 50;           //change range to 0-50
                float percent = m / 50f;    //what percentage of this stage am I at?
                percent *= 37;              //Now how many pixels is that
                float top = 545f - percent; //How many pixels is that from the top of the window?
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else if (ra >= 0)  // 50 - 0
            {
                //91 pixels, 50 m
                //Subtract the min value from the reading, should yield a number from range-0
                float m = ra - 0;           //silly here, I know
                float percent = m / 50f;    //what percentage of this stage am I at?
                percent *= 91;              //Now how many pixels is that
                float top = 637f - percent; //How many pixels is that from the top of the window?
                return new Rect(left * Scale, (top * Scale) - ((nh / 2) * Scale), nw * Scale, nh * Scale);
            }
            else return new Rect(left * Scale, 337 * Scale, nw * Scale, nh * Scale); //peg it at the bottom
        }

        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("RadarPosition");
            isMinimized = config.GetValue<bool>("RadarMinimized");
            redLight = config.GetValue<int>("RadarRedLight", 10);
            yellowLight = config.GetValue<int>("RadarYellowLight", 100);
            greenLight = config.GetValue<int>("RadarGreenLight", 1000);
            Scale = (float)config.GetValue<double>("RadarScale", 0.5f);
            calibration = config.GetValue<int>("RadarCalibration", 0);
            auto_burn = config.GetValue<bool>("RadarAutoBurn", false);
            contact_stop = config.GetValue<bool>("RadarContactStop", false);
            contact_alt = config.GetValue<int>("RadarContactAlt", 1);
        }

        public override void save(PluginConfiguration config)
        {
            config.SetValue("RadarPosition", windowPosition);
            config.SetValue("RadarMinimized", isMinimized);
            config.SetValue("RadarRedLight", redLight);
            config.SetValue("RadarYellowLight", yellowLight);
            config.SetValue("RadarGreenLight", greenLight);
            config.SetValue("RadarScale", (double)Scale);
            config.SetValue("RadarCalibration", calibration);
            config.SetValue("RadarAutoBurn", auto_burn);
            config.SetValue("RadarContactStop", contact_stop);
            config.SetValue("RadarContactAlt", contact_alt);
        }

        //Draws a time in the format MM : SS
        private void drawTime(float right, float top, double time)
        {
            double seconds = time;
            double minutes = 0;
            //double hours = 0;
            float width = Resources.digits.width;
            float height = Resources.digits.height / 11;
            //time is in seconds, so crunch how many minutes there are
            while (seconds > 60)
            {
                minutes++;
                seconds -= 60;
            }
            //now how many hours were there?
            /*while (minutes > 60)
            {
                hours++;
                minutes -= 60;
            } */
            //Overflow check for max of 99:59
            if (minutes > 99)
            {
                //hours = 999;
                minutes = 99;
                seconds = 59;
            }
            //draw Seconds

            int x = (int)seconds % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - width) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)seconds / 10;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (2 * width)) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
            //draw minutes
            x = (int)minutes % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (3f * width) - 10) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)minutes / 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (4f * width) - 10) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
        }
    }
}
