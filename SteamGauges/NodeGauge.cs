using UnityEngine;
using KSP;
using KSP.IO;
using System;

namespace SteamGauges
{
    class NodeGauge : Gauge
    {
        private double deltaV;                                           //Total delta v for this burn
        private double deltaVRem;                                        //Delta v remaining for this burn
        private int throttleDelay;                                       //How much time it takes the throttle to go from 0-100%
        public static bool useCalculatedBurn;                                  //Should I use my calculated burn time, or KSP's?
        private bool autoBurn;                                           //Enable automatic burn?
        private bool autoShutdown;                                       //Enable automatic shutoff?
        private double minDv=double.MaxValue;                            //The minimum Dv remaining in the burn
        private double lastTime;                                         //Last time the min Dv was updated
        private long timeToBurn=999;                                     //Stored value of time until burn
        private Rect burn_toggle;
        private Rect shutdown_toggle;

        public bool getUseCalcBurn()
        {
            return useCalculatedBurn;
        }

        public void setUseCalcBurn(bool v)
        {
            useCalculatedBurn = v;
        }

        public int getThrottleDelay()
        {
            return throttleDelay;
        }

        public void setThrottleDelay(int v)
        {
            if (v < 0) v = 0;   //Shouldn't want to make it take less time
            //No restrictive max value...though more than about 10 seconds seems awfully long.
            throttleDelay = v;
        }

        //Draw if not minimized and if there is a maneuver node active
        protected override bool isVisible()
        {
            if (this.isMinimized) return false;
            try
            {
                if (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count < 1) return false;  //Don't draw unless there's a node
            }
            catch
            {
                return false;   //If there is an error while finding maneuver node count, then clearly we won't be drawing nodes now.
            }
            return true;
        }

        //Gauge specific actions
        protected override void GaugeActions()
        {
            ManeuverNode node;
            burn_toggle = new Rect(108 * Scale, 75 * Scale, 69 * Scale, 45 * Scale);
            shutdown_toggle = new Rect(108 * Scale, 298 * Scale, 69 * Scale, 45 * Scale);
            try
            {
                node = FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.ToArray()[0];
            }
            catch
            {
                return;
            }
            automation(node);
            if (Event.current.type == EventType.repaint)
                PaintGauge(node);
            //Auto Burn toggle
            if (Event.current.type == EventType.MouseUp && burn_toggle.Contains(Event.current.mousePosition))
            {
                autoBurn = !autoBurn;
                home.SaveMe();
            }
            //Auto Shutdown toggle
            if (Event.current.type == EventType.MouseUp && shutdown_toggle.Contains(Event.current.mousePosition))
            {
                autoShutdown = !autoShutdown;
                home.SaveMe();
            }
        }

        //Draws the gauge textures to the screen
        private void PaintGauge(ManeuverNode node)
        {
            
            //Draw the face (background)
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0f, 0.5f, 0.499f));
            //PatchedConicSolver PCS = FlightGlobals.ActiveVessel.patchedConicSolver;
            //Draw the dV needle
            drawNeedle();
            //Draw  the digital readouts
            drawNumbers(node);

            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
            {
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0.5f, 0.5f, 0.5f));
            }
            //Draw the casing (foreground)
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.5f, 0.5f, 0.5f, 0.5f));

            //Draw the buttons
            if (autoBurn)
            {
                GUI.DrawTextureWithTexCoords(burn_toggle, texture, new Rect(0.5800f, 0.3649f, 0.08625f, 0.0553f));  //Green Burn
            }
            else
            {
                GUI.DrawTextureWithTexCoords(burn_toggle, texture, new Rect(0.6713f, 0.3649f, 0.08625f, 0.0553f));  //Grey Burn
            }
            if (autoShutdown)
            {
                GUI.DrawTextureWithTexCoords(shutdown_toggle, texture, new Rect(0.7625f, 0.3649f, 0.08625f, 0.0553f));  //Green Stop
            }
            else
            {
                GUI.DrawTextureWithTexCoords(shutdown_toggle, texture, new Rect(0.8563f, 0.3649f, 0.08625f, 0.0553f));  //Grey Stop
            }
        }

        //Allows for automatic initiation and termination of burns
        private void automation(ManeuverNode myNode)
        {
            //Auto shutdown
            if (autoShutdown)
            {
                if ((myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit).magnitude< 0.5 || offTarget(myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit),7.6f)) && timeToBurn < 1)
                {
                    autoShutdown = false;
                    FlightInputHandler.state.mainThrottle = 0;
                    Debug.Log("(SG) Burn complete, throttle back.");
                }
            }
        
            //if (offTarget(myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit),7.2f)) Debug.Log("Off target!");
            //Auto burn
            if (autoBurn)
            {
                if (timeToBurn <= 5)
                    TimeWarp.SetRate(0, true);
                if (timeToBurn <= 50 && TimeWarp.CurrentRateIndex > 2)
                    TimeWarp.SetRate(2, true);
                if (timeToBurn <= 500 && TimeWarp.CurrentRateIndex > 3)
                    TimeWarp.SetRate(3, true);
                if (timeToBurn <= 5000 && TimeWarp.CurrentRateIndex > 4)
                    TimeWarp.SetRate(4, true);
                if (timeToBurn < 1 && FlightInputHandler.state.mainThrottle == 0 && !offTarget(myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit), 2f))
                {
                    //Add check for w/in 2 degrees
                    autoBurn = false;
                    FlightInputHandler.state.mainThrottle = 1;
                    Debug.Log("(SG) Node reached, commencing burn!");
                }
            }
            //update min delta V every .2 seconds
            if (Planetarium.GetUniversalTime() > lastTime + 0.2)
            {
                lastTime = Planetarium.GetUniversalTime();
                if (myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit).magnitude < minDv)
                    minDv = myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit).magnitude;
                if (timeToBurn > 0) minDv = myNode.DeltaV.magnitude;    //suck it blue!
            }
        }

        //Returns true if vec is more than 2 degrees from the nose in either pitch or yaw
        private bool offTarget(Vector3 vec, float amount)
        {
            Transform self = FlightGlobals.ActiveVessel.ReferenceTransform;
            //Determine the number of degrees vec differs from vessel orientation
            Vector3 rVel = new Vector3();
            rVel.x = SteamShip.AngleAroundNormal(vec, self.up, self.forward);
            rVel.y = SteamShip.AngleAroundNormal(vec, self.up, self.right);
            if (Math.Abs(rVel.x) > amount) return true;
            if (Math.Abs(rVel.y) > amount) return true;
            return false;
        }
        

        //Draws the digital readouts to the gauge
        private void drawNumbers(ManeuverNode myNode)
        {
            //dv = Isp * ln (m0 / m1)
            //e^(dv/ISP) = m0/m1
            //m1 = m0/e^(dv/ISP)    ...I think.
            //mass flow = thrust/isp
            deltaV = myNode.DeltaV.magnitude;                                       //The burn's ΔV
            deltaVRem = myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit).magnitude;   //Remaining ΔV in the burn
            //res r = calculateThrust(FlightGlobals.ActiveVessel);                    //Actually calculates thrust, mass, and Isp
            //Debug.Log("Mass: " + Math.Round(r.mass, 2) + " Thrust: " + Math.Round(r.thrust, 2) + " ISP: " + Math.Round(r.isp, 2));
            //double mass = SteamShip.Mass / Math.Pow(Math.E, (deltaVRem / (SteamShip.ISP*9.82)));    //Mass after burn   Changed from 9.821
            //Debug.Log("Final Mass: " + Math.Round(mass, 2)+" Burn Mass: "+Math.Round(r.mass-mass,2));
            //double rate = SteamShip.MaxThrust / (SteamShip.ISP*9.82);                                  //Mass flow rate, rounded to 5 digits
            //double burnTime = (SteamShip.Mass - mass)/rate;                                 //Mass to burn over rate should give time, but doesn't
            //burnTime += SteamShip.EngineAccel+SteamShip.EngineDecel;                        //Compensate for slow throttles
            double burnTime = SteamShip.BurnTime;
            NavBallBurnVector bv = FlightUIController.fetch.GetComponentsInChildren<NavBallBurnVector>()[0];
            double bt2 = bv.estimatedBurnTime;
            if (double.IsInfinity(bt2) || double.IsNaN(bt2)) bt2 = 0; //Assume its good if not infinity or NaN
            //Draw values
            drawDigits(182, 131, deltaV);
            //If we are past the node time, time until burn is 0
            if (Planetarium.GetUniversalTime() < myNode.UT)
            {
                if (useCalculatedBurn)
                {
                    timeToBurn = (long) ((myNode.UT - Planetarium.GetUniversalTime()) - (burnTime/2.0));
                    drawTime(219, 198, timeToBurn);    //draw actual time to burn start
                }
                else
                {
                    timeToBurn = (long) ((myNode.UT - Planetarium.GetUniversalTime()) - (bt2 / 2));
                    drawTime(219, 198, timeToBurn);    //draw actual time to KSP's burn start
                }
            }
            else
            {
                drawTime(219, 198, 0);    //Use 0 as time to burn if past node
            }
            if (useCalculatedBurn)
                drawTime(219, 261, burnTime);   //Draw burn time.
            else
                drawTime(219, 261, bt2);        //Draw KSP's burn time
        }

        //Draws the needle for % of dV remaining
        private void drawNeedle()
        {
            double percent;
            if (deltaV == 0) deltaV = 0.01; //prevents divide by zero
            percent = deltaVRem / deltaV;
            if (percent > 1) percent = 1;   //Clamp it
            if (percent < 0) percent = 0;
            percent *= -180;                //convert to degrees
            //Draw the throttle needle
            Vector2 pivotPoint = new Vector2(200 * Scale, 202 * Scale);    //Center of the case
            GUIUtility.RotateAroundPivot(-180f * FlightInputHandler.state.mainThrottle, pivotPoint);
            GUI.DrawTextureWithTexCoords(new Rect(194f*Scale, 331f*Scale, 9f * Scale, 27f * Scale), texture, new Rect(0.965f, 0.2875f, 0.0112f, 0.0332f));
            //GUI.DrawTexture(new Rect(0f, 0f, Resources.Node_needle.width * Scale, Resources.Node_needle.height * Scale), Resources.Node_needle);
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
            //Now Draw the delta v needle
            GUIUtility.RotateAroundPivot((float)percent, pivotPoint);
            GUI.DrawTextureWithTexCoords(new Rect(198f*Scale, 316f*Scale, 9f * Scale, 27f * Scale), texture, new Rect(0.965f, 0.3255f, 0.0112f, 0.0332f));//65
            //GUI.DrawTexture(new Rect(0f, 0f, Resources.Node_dv_needle.width * Scale, Resources.Node_dv_needle.height * Scale), Resources.Node_dv_needle);
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }

        private struct res 
        {
            public double thrust;
            public double mass;
            public double isp;
        }

        //Calculates and returns the amount of thrust available at full throttle
        private res calculateThrustOld(Vessel v)
        {
            res r = new res();
            if (v.loaded)   //I might make an unloaded version later, but for now we'll stick with loaded vessels
            {
                foreach (Part p in v.parts)
                {
                    if (p.physicalSignificance == Part.PhysicalSignificance.FULL)
                    {
                        r.mass += p.mass;
                    }
                    r.mass += p.GetResourceMass();  //I suppose a part could have no physical significance, but its resources could?
                   // if ((p.State == PartStates.ACTIVE) || (p.State == PartStates.IDLE))
                    //{
                        foreach (PartModule pm in p.Modules)
                        {
                            if (pm.moduleName.Equals("ModuleEngines"))
                            {
                                ModuleEngines e = pm as ModuleEngines;
                                /*Debug.Log("Engine: "+p.InternalModelName);
                                String status = p.partName+" status: ";
                                if (e.engineShutdown) status += "Shutdown ";
                                if (e.getFlameoutState) status += "Flamed Out ";
                                if (e.getIgnitionState) status += "Ignited ";
                                if (e.isEnabled) status += "Enabled ";
                                if (e.isOperational) status += "Operational ";
                                status += e.status;
                                status += e.statusL2;
                                Debug.Log(status); */
                                if ((e != null) &&  e.isOperational)
                                {
                                    //Debug.Log("Active engine!");
                                    r.isp += e.maxThrust / e.atmosphereCurve.Evaluate((float)v.staticPressurekPa);
                                    r.thrust += e.maxThrust;
                                }
                            }
                        }
                    //}
                }
            }
            r.isp = r.thrust / r.isp;   //Weighted average of ISPs
            return r;
        }

        //Draws a formatted 5 digit number with an 'm', 'K', or 'M' postfix
        private void drawDigits(float right, float top, double value)
        {
            float char_width = Resources.orbit_chars.width;
            float char_height = Resources.orbit_chars.height / 3f;
            float output = 0;
            //Below 100,000 m, display raw value and 'm'
            if (value < 100000)
            {
                output = (float)value;
                GUI.DrawTextureWithTexCoords(new Rect((right - char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.orbit_chars, new Rect(0f, 0f, 1f, 0.33333f));
            }
            //Below 100,000 k, display kilometers and 'K'
            else if (value < 100000000)
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
                int divisor = (int)Mathf.Pow(10f, (i - 1));
                int x = (int)(output / divisor);
                x = x % 10;
                float y = ((float)x * 0.091f);
                GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * (i + 1))) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
            }
        }

        //Draws a time in the format HHH : MM : SS
        private void drawTime(float right, float top, double time)
        {
            double seconds = time;
            double minutes = 0;
            double hours = 0;
            float width = Resources.digits.width;
            float height = Resources.digits.height / 11;
            //time is in seconds, so crunch how many minutes there are
            while (seconds > 60)
            {
                minutes++;
                seconds -= 60;
            }
            //now how many hours were there?
            while (minutes > 60)
            {
                hours++;
                minutes -= 60;
            }
            //Overflow check for max of 999:59:59
            if (hours > 999)
            {
                hours = 999;
                minutes = 59;
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
            //draw hours
            x = (int)hours % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (5 * width) - 20) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)hours / 10;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (6 * width) - 20) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)hours / 100;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (7 * width) - 20) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
        }


        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("NodePosition");
            isMinimized = config.GetValue<bool>("NodeMinimized", false);
            Scale = (float)config.GetValue<double>("NodeScale", 0.5);
            throttleDelay = config.GetValue<int>("NodeThrottle", 2);
            useCalculatedBurn = config.GetValue<bool>("NodeCalculatedBurn", true);
            autoBurn = config.GetValue<bool>("NodeAutoBurn", false);
            autoShutdown = config.GetValue<bool>("NodeAutoStop", false);
        }

        public override void save(PluginConfiguration config)
        {
            config.SetValue("NodePosition", windowPosition);
            config.SetValue("NodeMinimized", isMinimized);
            config.SetValue("NodeScale", (double)Scale);
            config.SetValue("NodeThrottle", throttleDelay);
            config.SetValue("NodeCalculatedBurn", useCalculatedBurn);
            config.SetValue("NodeAutoBurn", autoBurn);
            config.SetValue("NodeAutoStop", autoShutdown);
        }
    }
}
