using UnityEngine;
using KSP;
using KSP.IO;

namespace SteamGauges
{
    class RendezvousGauge : Gauge
    {
        //public static Rect windowPosition = new Rect(350, 200, 200, 204);     //The position for our little window (left, top, width, height)
        //private static Rect lastPosition = windowPosition;                    //Used so I don't over-save
        //public static bool isMinimized;                                       //Is the window currently minimized?
        private int closestGreen = 1000;                                        //Default distance at which the green light comes on
        private int closestYellow = 100;                                        //Default distance at which the yellow light comes on
        private int closestRed = 10;                                            //Default distance at which the red light comes on
       // private static float Scale = 1;                                       //Scale of how large the gauge is
       
        public override string getTextureName() { return "rz"; }
        public override string getTooltipName() { return "Rendezvous Gauge"; }

        public RendezvousGauge()
        {
            windowPosition = new Rect(350, 200, 200, 204);
        }

        public int getGreen()
        {
            return closestGreen;
        }
        public int getYellow()
        {
            return closestYellow;
        }
        public int getRed()
        {
            return closestRed;
        }

        public void setGreen(int v)
        {
            if (v < 0) v = 0;   //No negative distances, please
            //I don't know what a good max would be...so eat your heart out
            closestGreen = v;
        }
        public void setYellow(int v)
        {
            if (v < 0) v = 0;   //No negative distances, please
            //I don't know what a good max would be...so eat your heart out
            closestYellow = v;
        }
        public void setRed(int v)
        {
            if (v < 0) v = 0;   //No negative distances, please
            //I don't know what a good max would be...so eat your heart out
            closestRed = v;
        }

        //Draw if not minimized and if there is a target selected
        protected override bool isVisible()
        {
            if (this.isMinimized) return false;
            if (FlightGlobals.fetch.VesselTarget == null) return false;   //Don't draw unless there is a target
            return true;
        }

        //Do gauge specific things.
        protected override void GaugeActions()
        {
            // This gauge only draws stuff, no need to handle other events
            if (Event.current.type != EventType.Repaint)
                return;
            //Left gauge - Orbital information
            //Draw the face (background)
            GUI.DrawTextureWithTexCoords(new Rect(179f * Scale, 46f * Scale, 180f * Scale, 315f * Scale), texture, new Rect(0.0358f, 0.0217f, 0.15f, 0.2625f));   //Left background
            GUI.DrawTextureWithTexCoords(new Rect(400f * Scale, 0f * Scale, 400f * Scale, 407f * Scale), texture, new Rect(0.3333f, 0.6608f, 0.3333f, 0.3392f));   //Center background
            Orbit o = null;
            try
            {
                o = FlightGlobals.fetch.VesselTarget.GetOrbit();
            }
            catch
            {
                //Draw "NO TGT" markers - this shouldn't really happen any more with gauge hiding
                GUI.DrawTextureWithTexCoords(new Rect(65f * Scale, 131f * Scale, 122f * Scale, 160f * Scale), texture, new Rect(0.2083f, 0.1633f, 0.1017f, 0.1333f));    //Left No Tgt
                GUI.DrawTextureWithTexCoords(new Rect(446f * Scale, 88f * Scale, 77f * Scale, 15f * Scale), texture, new Rect(0.4808f, 0.2008f, 0.0642f, 0.0125f));   //Center No Tgt
                GUI.DrawTextureWithTexCoords(new Rect(865f * Scale, 131f * Scale, 164f * Scale, 162f * Scale), texture, new Rect(0.3225f, 0.1600f, 0.1367f, 0.1350f));   //Right No Tgt
                GUI.DrawTextureWithTexCoords(new Rect(1014f * Scale, 101f * Scale, 17f * Scale, 17f * Scale), texture, new Rect(0.54f, 0.2625f, 0.0142f, 0.0142f));  //Right distance light
                return;
            }
            //draw the inclination needle
            drawInclination(o);
            //Draw the digits on the left gauge
            leftGauge(o);
            //Center gauge - relative velocity
            centerGauge();
            //Right gauge - Distance, Closest approach
            rightGauge();
            //Draw the bezels, if selected
            if (SteamGauges.drawBezels)
            {
                GUI.DrawTextureWithTexCoords(new Rect(0f * Scale, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0.6608f, 0.3333f, 0.3392f));   //Left Bezel
                GUI.DrawTextureWithTexCoords(new Rect(800f * Scale, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.6667f, 0.6608f, 0.3333f, 0.3392f));   //Right Bezel
            }
            //Draw the casings (foreground)
            GUI.DrawTextureWithTexCoords(new Rect(0f * Scale, 1f * Scale, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0.3217f, 0.3333f, 0.3392f));   //Left Casing
            GUI.DrawTextureWithTexCoords(new Rect(400f * Scale, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.3333f, 0.3217f, 0.3333f, 0.3392f));   //Center Casing
            GUI.DrawTextureWithTexCoords(new Rect(799f * Scale, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.6667f, 0.3217f, 0.3333f, 0.3392f));   //Right Casing
        }

        //This code draws the relative velocity pointer
        private void centerGauge()
        {
            ITargetable itarget = FlightGlobals.fetch.VesselTarget;
            if (itarget != null)    //Don't draw data unless there's a target
            {
                Vector3 tarDelta = FlightGlobals.ship_tgtVelocity;  //Relative velocity to target.  Z = vertical, Y = horizontal, X = Forward
                drawDigits2(526f, 77f, tarDelta.x);     //draw closure rate ("towards" the target"), similar to navball     
                //Now draw the wires and cursor for x and y drift
                float dX = (float) tarDelta.y;  //I know these are backwards, but it makes sense to me
                float dY = (float) tarDelta.z;
                //Negative check
                bool xNeg = false;
                bool zNeg = false;
                if (dX < 0)
                {
                    xNeg = true;
                    dX *= -1f;
                }
                if (dY < 0)
                {
                    zNeg = true;
                    dY *= -1f;
                }
                //Range limit
                if (dX > 10) dX = 10f;
                if (dY > 10) dY = 10f;
                //Movement isn't linear or log, so check for each range independantly
                //Center is at 200, 202
                float x = 0f;
                if (dX < 1)  //0-1 ranges from 200 +/- 48
                {
                    x = dX * 48;
                }
                else if (dX < 5)     //1-5 ranges 4 m/s over another 48 pixels
                {
                    x = (dX - 1f) / 4f;
                    x *= 48f;
                    //move past 0-1 band
                    x += 48f;
                }
                else //5 - 10 ranges a final 5 m/s over 48 pixels
                {
                    x = (dX - 5) / 5f;
                    x *= 48f;
                    //move past 1-5 band
                    x += 96f;
                }
                float y = 0f;
                if (dY < 1)  //0-1 ranges from 100 +/- 45
                {
                    y = dY * 45f;
                }
                else if (dY < 5)     //1-5 ranges another 23 over 4 m/s
                {
                    y = (dY - 1f) / 4f;
                    y *= 45f;
                    //move past 0-1 band
                    y += 45f;
                }
                else //5 - 10 ranges a final 23 pixels over 5 m/s
                {
                    y = (dY - 5f) / 5f;
                    y *= 45f;
                    //move past 1-5 band
                    y += 90f;
                }
                //Convert back to negative values if needed
                if (xNeg) x *= -1;
                if (zNeg) y *= -1;
                //Draw the x wire, an 8x345 texture
                GUI.DrawTextureWithTexCoords(new Rect((592f + x) * Scale, 43f * Scale, 8f * Scale, 345f * Scale), texture, new Rect(0.4675f, 0.0133f, 0.0067f, 0.2875f));
                //Draw the y wire, an 345x8 texture
                GUI.DrawTextureWithTexCoords(new Rect(432f * Scale, (198f - y) * Scale, 345f * Scale, 8f * Scale), texture, new Rect(0.4825f, 0.2933f, 0.2875f, 0.0067f));
                //Draw the center marker, an 47x46 texture
                GUI.DrawTextureWithTexCoords(new Rect((573f + x) * Scale, (179f - y) * Scale, 47f * Scale, 46f * Scale), texture, new Rect(0.5125f, 0.2208f, 0.0392f, 0.0383f));
            }
            else        //Draw the "OFF" flag, and centered needles
            {
                GUI.DrawTextureWithTexCoords(new Rect(592f * Scale,  43f * Scale, 8f * Scale, 345f * Scale), texture, new Rect(0.4675f, 0.0133f, 0.0067f, 0.2875f));    //X Wire
                GUI.DrawTextureWithTexCoords(new Rect(432f * Scale, 198f * Scale, 345f * Scale, 8f * Scale), texture, new Rect(0.4825f, 0.2933f, 0.2875f, 0.0067f));    //Y Wire
                GUI.DrawTextureWithTexCoords(new Rect(576f * Scale, 180f * Scale, 47f * Scale, 46f * Scale), texture, new Rect(0.5125f, 0.2208f, 0.0392f, 0.0383f));    //Center Mark
                GUI.DrawTextureWithTexCoords(new Rect(693f * Scale,  28f * Scale, 77f * Scale, 77f * Scale), texture, new Rect(0.5717f, 0.2192f, 0.0642f, 0.0642f));    //OFF Flag
            }
        }

        //This code draws the target distance and closest approach
        private void rightGauge()
        {
            ITargetable itarget = FlightGlobals.fetch.VesselTarget;
            if (itarget != null && itarget.GetOrbit() != null)
            {
                Vessel target = itarget.GetVessel();
                //Calculate and draw the distance between active vessal and target
                Vector3d aPos = FlightGlobals.ActiveVessel.orbit.pos;
                Vector3d tPos = itarget.GetOrbit().pos;
                //double distance = Vector3d.Distance(tPos, aPos);
                double distance = SteamShip.TargetDist;
                drawDigits(981, 131, distance);
                //Draw closure light
                if (distance <closestRed )  //red light
                {
                    GUI.DrawTextureWithTexCoords(new Rect(1014f * Scale, 101f * Scale, 17f * Scale, 17f * Scale), texture, new Rect(0.5058f, 0.2625f, 0.0142f, 0.0142f));
                }
                else if (distance < closestYellow)   //yellow light
                {
                    GUI.DrawTextureWithTexCoords(new Rect(1014f * Scale, 101f * Scale, 17f * Scale, 17f * Scale), texture, new Rect(0.5225f, 0.2625f, 0.0142f, 0.0142f));
                }
                else if (distance < closestGreen)   //green light
                {
                    GUI.DrawTextureWithTexCoords(new Rect(1014f * Scale, 101f * Scale, 17f * Scale, 17f * Scale), texture, new Rect(0.4875f, 0.2625f, 0.0142f, 0.0142f));
                }
                
                //Crunch closest distance, and display
                //res Result = getClosestApproach();
                drawDigits(982, 197, SteamShip.ClosestApproach);
                drawTime(1019, 260, SteamShip.ClosestTime);
                //Maybe surround this gauge with a AN/DN offset?
            }
        }


        //private struct res { public double dist; public double time; }

       /* private res getClosestApproach()
        {
            //Crunch closest distance
            //I cunck my orbit into 100 pieces, and find between which two chuncks the minimum occurs...
            Orbit O = FlightGlobals.ActiveVessel.GetOrbit();
            Orbit TO = FlightGlobals.fetch.VesselTarget.GetOrbit();
            res Result = new res();
            if (TO == null) //Check for targets with no orbit...like MechJeb guidance I guess.
            {
                Result.dist = 0;
                Result.time = 0;
                return Result;
            }
            double period = O.period;
            double bestDist = double.MaxValue;
            double bestTime = 0;
            double t = Planetarium.GetUniversalTime();
            for (double d = 0; d < period; d += period / 100)    //Look at 100 places around the orbit
            {
                if (d == 0) continue;   //skip the first time
                double dist = Vector3d.Distance(O.getPositionAtUT(t + d), TO.getPositionAtUT(t + d));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTime = d;
                }
            }
            //Now, do it again, but over a small section of the orbit centered on the above
            double start = bestTime - (period / 100);
            double end = bestTime + (period / 100);
            for (double d = start; d < end; d += period / 1000)    //Look at 100 places within this chunck of orbit
            {
                if (d == start) continue;   //Skip the first time
                double dist = Vector3d.Distance(O.getPositionAtUT(t + d), TO.getPositionAtUT(t + d));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTime = d;
                }
            }
            //And one last time, which is probably overkill
            start = bestTime - (period / 1000);
            end = bestTime + (period / 1000);
            for (double d = start; d < end; d += period / 10000)    //Look at 100 places within this chunck of orbit
            {
                if (d == start) continue;   //For ease of computation
                double dist = Vector3d.Distance(O.getPositionAtUT(t + d), TO.getPositionAtUT(t + d));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTime = d;   //previous time is my start time
                }
            }
            
            Result.dist = bestDist;
            Result.time = bestTime;
            return Result;
        } */

        //This code draws the Apoapis, Periapsis, and Period digits onto the gauge
        private void leftGauge(Orbit o)
        {
            //Check for no assigned target, don't draw anything ("NO TGT" is on the background) 
            if (o == null) return;
            //Target Apoapsis
            drawDigits(182f, 132f, o.ApA);
            //Target Periapsis
            drawDigits(182f, 200f, o.PeA);
            //Target Altitude
            drawDigits(182f, 262f, o.altitude);
        }

        //this code draws the Inclination data to the gauge
        private void drawInclination(Orbit o)
        {
            if (o != null)
            {
                //We're actually drawing the difference between the active vessel's inclination and the target's inclination
                double delta = o.inclination - FlightGlobals.ActiveVessel.GetOrbit().inclination;
                //full size needle
                Vector2 pivotPoint = new Vector2(200 * Scale, 202 * Scale);            //Center of the case
                GUIUtility.RotateAroundPivot((float)(delta), pivotPoint);              //rotate the needle
                GUI.DrawTextureWithTexCoords(new Rect(313f * Scale, 199f * Scale, 27f * Scale, 9f * Scale), texture, new Rect(0.4875f, 0.2467f, 0.0225f, 0.0075f)); //Needle
                //GUI.DrawTexture(new Rect(0f, 0f, 400 * Scale, 407* Scale), Resources.orbit_needle);  //draw the needle
                GUI.matrix = Matrix4x4.identity;                                        //Reset rotation matrix
            }
            else
            {
                GUI.DrawTextureWithTexCoords(new Rect(313f * Scale, 199f * Scale, 27f * Scale, 9f * Scale), texture, new Rect(0.4875f, 0.2467f, 0.0225f, 0.0075f)); //Needle
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.orbit_needle.width* Scale, Resources.orbit_needle.height*Scale), Resources.orbit_needle);  //draw centered needle
            }
        }

        //Draws a formatted 3 digit number with 1 decimal place and sign 
        private void drawDigits2(float right, float top, double value)
        {
            float width = Resources.digits.width;
            float height = Resources.digits.height / 11;
            float val = 0f;
            bool negative = false;
            if (value > 999.9) val = 999.9f;   //check for overflow
            else if (value < -99.9)  //Chec for underflow
            {
                val = -99.9f;
            }
            else val = (float)value;    //assignment here prevents over/underflow
            if (val < 0)                //Check for a negative value
            {
                val *= -1;    //flip for calculations/drawing
                negative = true;
            }
            //get the decimal digit
            int dec = (int) (val * 10) % 10;
            float y = ((float)dec * 0.091f);
            //print("Decimal: " + dec);
            GUI.DrawTextureWithTexCoords(new Rect((right-width) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
            //Now draw each digit of our output, starting from the right
            for (int i = 1; i < 4; i++)
            {
                if ((i == 3) && (negative)) //if we're on the last digit, print the - if ncessary
                {
                    GUI.DrawTexture(new Rect((right - (width*4)-8) * Scale, top * Scale, width * Scale, height * Scale), Resources.minus); 
                    break;  //don't draw the last digit
                }
                int divisor = (int) Mathf.Pow(10f, i - 1);
                int x = (int)(val / divisor);
                x = x % 10;
                y = ((float)x * 0.091f);
                GUI.DrawTextureWithTexCoords(new Rect((right - (width * (i+1)) - 8)*Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
            }
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
                GUI.DrawTextureWithTexCoords(new Rect((right-char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.orbit_chars, new Rect(0f, 0f, 1f, 0.33333f));
            }
            //Below 100,000 k, display kilometers and 'K'
            else if (value < 100000000)
            {
                output = (float)(value / 1000f);
                GUI.DrawTextureWithTexCoords(new Rect((right-char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.orbit_chars, new Rect(0f, 0.333f, 1f, 0.33333f));
            }
            else //Display megameters and 'M'
            {
                output = (float)(value / 1000000f);
                GUI.DrawTextureWithTexCoords(new Rect((right-char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.orbit_chars, new Rect(0f, 0.6666f, 1f, 0.33333f));
            }
            //Now draw each digit of our output, starting from the right
            for (int i = 1; i < 6; i++)
            {
                //int divisor = 10 ^ (i - 1); //I don't know why this doesn't work, but it fails utterly.
                int divisor = (int) Mathf.Pow(10f, (i - 1));    //This actually funcitons correctly.
                int x = (int) (output / divisor);
                x = x % 10;
                float y = ((float)x * 0.091f);
                GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * (i+1))) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
            }
        }

        //Draws a time in the format HHH : MM : SS
        private void drawTime(float right, float top, double time)
        {
            double seconds = time;
            double minutes = 0;
            double hours = 0;
            float width = Resources.digits.width;
            float height = Resources.digits.height/11;
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
            
            int x = (int) seconds % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - width) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int) seconds / 10;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (2*width)) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
            //draw minutes
            x = (int) minutes % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (3f*width)-10) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x =(int)  minutes / 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (4f*width)-10) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
            //draw hours
            x = (int) hours % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (5*width)-20) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x =(int)  hours / 10;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (6*width)-20) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int) hours / 100;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (7*width)-20) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
        }

        //Load the configurable values
        public override void load(PluginConfiguration config)
        {            
            windowPosition = config.GetValue<Rect>("RZPosition",new Rect(100f, 100f, 600f, 204f));
            isMinimized = config.GetValue<bool>("RZMinimized", false);
            closestRed = config.GetValue<int>("RZRedDist", 10);
            closestYellow = config.GetValue<int>("RZYellowDist", 100);
            closestGreen = config.GetValue<int>("RZGreenDist", 1000);
            Scale = (float) config.GetValue<double>("RZScale", 0.5);
        }

        //Save the configurable values
        public override void save(PluginConfiguration config)
        {
            config.SetValue("RZPosition", windowPosition);
            config.SetValue("RZMinimized", isMinimized);
            config.SetValue("RZRedDist", closestRed);
            config.SetValue("RZYellowDist", closestYellow);
            config.SetValue("RZGreenDist", closestGreen);
            config.SetValue("RZScale", (double) Scale);
        }
    }
}