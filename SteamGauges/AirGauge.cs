using UnityEngine;
using KSP;
using KSP.IO;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace SteamGauges
{
    class AirGauge : Gauge
    {
        public float criticalAOA;
        private float _minMach;
        public bool useEAS;
        private bool _first;

        public override string getTextureName() { return "air"; }
        public override string getTooltipName() { return "Air Gauge"; }

        //Constructor for button-specific initialization
        //Especially button construction
        public AirGauge()
        {
            GaugeButton eastas = new GaugeButton();
            eastas.active = true;
            eastas.permPosition = new Rect(91f, 198f, 49f, 49f);
            eastas.onTexture = new Rect(0.7036f, 0.4263f, 0.0613f, 0.0602f);
            eastas.offTexture = new Rect(0.6350f, 0.4263f, 0.0613f, 0.0602f);
            buttons = new GaugeButton[1];
            buttons[0] = eastas;
            _first = true;   //Testing for first run, should load saved EAS/TAS selection
        }

        //Specific conditions in which this gauge should be visible
        protected override bool isVisible()
        {
            //Don't draw if the user has minimized this gauge
            if (this.isMinimized) return false;
            //Draw if in an atmosphere
            if (FlightGlobals.ActiveVessel.mainBody.atmosphere && (FlightGlobals.ActiveVessel.altitude < FlightGlobals.ActiveVessel.mainBody.atmosphereDepth))
                return true;
            //If not, don't draw
            return false;
        }

        //The specific actions that make this guage do its stuff.
        //Called from inside the OnWindow() method
        protected override void GaugeActions()
        {
            if (_first)
            {
                _first = false;
                buttons[0].active = useEAS;
            }
            useEAS = buttons[0].active;

            // This code only draws stuff, no need to handle other events
            if (Event.current.type != EventType.repaint)
                return;

            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, base_width * Scale, base_height * Scale), texture, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
            //speed tape background
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, base_width * Scale, base_height * Scale), texture, new Rect(0f, 0f, 0.5f, 0.5f));
            Vessel v = FlightGlobals.ActiveVessel;
            //Draw speed needles
            double M;
            drawSpeedNeedles(v, out M);
            //Draw casing
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, base_width * Scale, base_height * Scale), texture, new Rect(0f, 0.5f, 0.5f, 0.4999f));
            //Draw Mach value
            drawMach(M);
            //Draw AoA & stall
            drawAoA(v);
            //Draw Air needle
            drawAirNeedle(v);
        }

        //Draws the needles around the outside of the gauge
        private void drawSpeedNeedles(Vessel v, out double M)
        {
            double easCoeff, tas;
            //getAirspeedInfo(v, out M, out tas, out easCoeff);
            M = SteamShip.Mach;
            tas = SteamShip.TAS;
            easCoeff = SteamShip.EASCoef;
            if (!useEAS) easCoeff = 1;

            //Draw terminal needle
            drawSpeedNeedle(getTerm(v) * easCoeff, 0, 0, new Rect(624, 414, 14, 42));

            //Draw sound speed needle
            if (M > 0)
                drawSpeedNeedle(tas * easCoeff / M, 0, 0, new Rect(715, 417, 8, 31));

            //draw airspeed needle
            drawSpeedNeedle(tas * easCoeff, 0, 5, new Rect(660, 412, 9, 27));
        }

        //Draws a needle onto the gauge, which one depending on which tile is specified
        //via a.g.
        private void drawSpeedNeedle(double term, float dx, float dy, Rect tile)
        {
            double angle = 0;
            //First 90 degrees is o-100 m/s
            if (term < 100)
            {
                angle = term / 1.11111;
            }
            else if (term < 675)
            {
                term -= 100; //0-575 over 248 degrees
                //575/255 = 2.318 knots/degree or 0.4313 deg/knot
                angle = term * 0.4435;
                angle += 90d;
            }
            else
            {
                angle = 355d;
            }
            //Now rotate the GUI and draw the needle
            Vector2 pivotPoint = new Vector2(200 * Scale, 203 * Scale);    //Center of the case
            GUIUtility.RotateAroundPivot((float)angle, pivotPoint);
            //GUI.DrawTextureWithTexCoords(new Rect(193f * Scale, 45f * Scale, 14f * Scale, 42f * Scale), texture, new Rect(0.7800f, 0.4398f, 0.0175f, 0.0516f));
            float w = this.texture.width, h = this.texture.height;
            GUI.DrawTextureWithTexCoords(new Rect((200f + dx - tile.width*0.5f) * Scale, (87f + dy - tile.height) * Scale, tile.width * Scale, tile.height * Scale),
                this.texture,new Rect(tile.x/w, 1f - (tile.y+tile.height)/h, tile.width/w, tile.height/h));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }

        //Draws the digital Mach readout
        private void drawMach(double m)
        {
            if (m < _minMach) m = 0;
            drawDigits(262f, 119f, 2, m);
        }

        //Draws digital AoA readout and Stall light as appropriate
        private void drawAoA(Vessel v)
        {
            double aoa = getAOA(v.GetSrfVelocity());
            if (v.Landed) aoa = 0;  //Reduce jitter on the ground
            drawDigits(223f, 157f, 1, aoa);
            if (Math.Abs(aoa) > criticalAOA && !v.Landed)
                GUI.DrawTextureWithTexCoords(new Rect(228f * Scale, 166f * Scale, 80f * Scale, 18f * Scale), texture, new Rect(0.8225f, 0.3342f, 0.100f, 0.0221f)); //RED stall
            else
                GUI.DrawTextureWithTexCoords(new Rect(228f * Scale, 166f * Scale, 80f * Scale, 18f * Scale), texture, new Rect(0.8225f, 0.3686f, 0.100f, 0.0221f)); //Stall
        }

        //Draws the needle for intake air
        private void drawAirNeedle(Vessel v)
        {
            //double prov = airMass(v);
            //double req = getRequiredAir(v);
            double prov, req;
            getRequiredAir(v, out req, out prov);
            double percent = 6;
            if (req > 0)
                percent = prov / req;
            //if (SteamGauges.debug) Debug.Log("Air Percent: " + Math.Round(prov, 2) + "/" + Math.Round(req, 2) + " = " + Math.Round(percent, 2));
            //Debug.Log("Air Percent: " + Math.Round(percent, 2));
            double angle = 0;
            //right 180 deg is 1 to 5
            if (percent > 6) percent = 6d;
            if (percent > 2)
            {
                percent -= 2d;  //Now 0-4
                percent /= 4d;  //now 0-1
                angle = percent * 180d;   //convert to angle
                angle = 180d - angle;  //rotate the other way
            }
            else if (percent > 1)
            {
                //left 180 is 0-1
                percent -= 1d;
                angle = percent * 90d;
                angle = 90d - angle;
                angle += 180d;
            }
            else
            {
                //0-1 for last 85 degrees
                angle = percent * 85d;
                angle = 85d - angle;
                angle += 270;
            }
            //Debug.Log("Final Percent: " + Math.Round(percent,2));
            //Debug.Log("Air angle: " + Math.Round(angle, 1));
            //Now rotate the GUI and draw the needle
            Vector2 pivotPoint = new Vector2(200 * Scale, 261 * Scale);    //Center of air gauge
            GUIUtility.RotateAroundPivot((float)angle, pivotPoint);
            GUI.DrawTextureWithTexCoords(new Rect(195f * Scale, 205f * Scale, 11f * Scale, 66f * Scale), texture, new Rect(0.8925f, 0.4066f, 0.01375f, 0.0811f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }

        //Returns the current terminal velocity for vessel v in true m/s
        public static float getTerm(Vessel v)
        {
            //Use FAR data if installed
            SteamShip.InitFAR();
            if (SteamShip.far_GetTermVel != null && v == FlightGlobals.ActiveVessel)
                return (float)SteamShip.far_GetTermVel();

            double totalMass = 0d;
            double massDrag = 0d;
            foreach (Part part in v.parts)
            {
                if (part.physicalSignificance != Part.PhysicalSignificance.NONE)
                {
                    double partMass = part.mass + part.GetResourceMass();
                    totalMass += partMass;
                    massDrag += partMass * part.maximum_drag;
                }
            }
            double gravity = FlightGlobals.getGeeForceAtPosition(v.CoM).magnitude;
            double atmosphere = v.atmDensity;
            double terminalVelocity = 0d;
            if (atmosphere > 0)
            {
                terminalVelocity = Math.Sqrt((2 * totalMass * gravity) / (atmosphere * massDrag * 1));  //1 should be drag index, which doesn't exist any more
            }

            return (float) terminalVelocity;
        }

        //Returns the current (true) Mach number of vessel v
        /*public static float GetMachNumber(Vessel v)
        {
            //Use FAR data if installed
            InitFAR();
            if (far_GetMachNumber != null)
                return far_GetMachNumber(v.mainBody, (float)v.altitude, v.srf_velocity);

            Vector3 velocity = v.GetSrfVelocity();
            float MachNumber = 0;
            CelestialBody body = FlightGlobals.currentMainBody;
            float temp = FlightGlobals.getExternalTemperature((float)v.altitude, body);
            double soundspeed = 331.3 * Math.Sqrt(1 + (temp / 273.15));  //wikipedia's practical formula for dry air
            MachNumber = Math.Abs(velocity.magnitude) / (float)soundspeed;
            if (MachNumber < 0)
                MachNumber = 0;
            return MachNumber;
        } */

        //Returns the difference in angle between vec and vessel orientation
        private float getAOA(Vector3 vec)
        {
            Transform self = FlightGlobals.ActiveVessel.ReferenceTransform;
            return AngleAroundNormal(vec, self.up, self.right)*-1f;
        }
        //return signed angle in relation to normal's 2d plane
        //From NavyFish's docking alignment
        private float AngleAroundNormal(Vector3 a, Vector3 b, Vector3 up)
        {
            return AngleSigned(Vector3.Cross(up, a), Vector3.Cross(up, b), up);
        }

        //-180 to 180 angle
        //From NavyFish's docking alignment
        private float AngleSigned(Vector3 v1, Vector3 v2, Vector3 up)
        {
            if (Vector3.Dot(Vector3.Cross(v1, v2), up) < 0) //greater than 90 i.e v1 left of v2
                return -Vector3.Angle(v1, v2);
            return Vector3.Angle(v1, v2);
        }

        //Another try at getting some good air information
        /*private double airMass(Vessel v)
        {
            double air = 0;
            foreach (Part P in v.parts)
            {
                foreach (PartModule PM in P.Modules)
                {
                    if (PM is ModuleResourceIntake)
                    {
                        ModuleResourceIntake MRI = PM as ModuleResourceIntake;
                        if (MRI.resourceName.Equals("IntakeAir") && MRI.intakeEnabled)
                        {
                            air += ((MRI.airSpeedGui+MRI.maxIntakeSpeed) * MRI.area * MRI.unitScalar);
                        }
                    }
                }
            }
            air *= v.atmDensity;
            return air;
        }*/

        //Returns the intake air stored in all parts in the vessel, replicating the display
        private double getIntakeAirOld(Vessel v)
        {
            double air = 0;
            foreach (Part P in v.parts)
            {
                foreach (PartResource PR in P.Resources)
                {
                    if (PR.resourceName.Equals("IntakeAir"))
                        air += PR.amount;
                }
            }
            return air;
        }

        //Returns the intake air required by all engines in the vessel
        //Intake air calculation courtesy of FAR and a.g.
        public static double getRequiredAir(Vessel v, out double airReq, out double airAvailable)
        {
            double dt = TimeWarp.fixedDeltaTime;
            //if (SteamGauges.debug) Debug.Log("dT: " + Math.Round(dt, 3));
            airReq = airAvailable = 0;
            foreach (Part P in v.parts)
            {
                foreach (PartModule PM in P.Modules)
                {
                    if (PM is ModuleEngines)
                    {
                        ModuleEngines ME = PM as ModuleEngines;
                        if (ME.engineShutdown || !ME.EngineIgnited)
                            continue;
                        foreach (Propellant Pro in ME.propellants)
                        {
                            if (Pro.name.Equals("IntakeAir"))
                            {
                                //if (SteamGauges.debug) Debug.Log("Air Req: " + Math.Round(Pro.currentRequirement,2));
                                airReq += Pro.currentRequirement;
                            }
                        }
                    }
                    else if (PM is ModuleEnginesFX)  //tricky RAPIERs!
                    {
                        ModuleEnginesFX MFX = PM as ModuleEnginesFX;
                        if (MFX.engineShutdown || !MFX.EngineIgnited)
                            continue;
                        foreach (Propellant Pro in MFX.propellants)
                        {
                            if (Pro.name.Equals("IntakeAir"))
                            {
                                //if (SteamGauges.debug) Debug.Log("Air Req: " + Math.Round(Pro.currentRequirement, 2));
                                airReq += Pro.currentRequirement;
                            }
                        }
                    }
                    else if (PM is ModuleResourceIntake)
                    {
                        ModuleResourceIntake MRI = PM as ModuleResourceIntake;
                        if (MRI.intakeEnabled && MRI.resourceName.Equals("IntakeAir"))
                        {
                            //if (SteamGauges.debug) Debug.Log("Air In: " + Math.Round(MRI.airFlow*dt, 2));
                            airAvailable += MRI.airFlow * dt;
                        }
                    }
                }
            }
            //Debug.Log("Calc Air: " + Math.Round(airAvailable/airReq, 2));
            //Debug.Log("Sim Air: " + Math.Round(SteamShip.AirPercent, 2));
            //if (SteamGauges.debug) Debug.Log("Air Req: " + Math.Round(airReq, 2) + " In: " + Math.Round(airAvailable, 2));
            return airReq;
        }

        

        //Draws a formatted 2 digit number with 1 or 2 decimal places and sign 
        private void drawDigits(float right, float top, int places, double value)
        {
            if (places > 2) places = 2;
            float width = Resources.digits.width;
            float height = Resources.digits.height / 11;
            float val = 0f;
            bool negative = false;
            if (value > 99.9) val = 99.9f;   //check for overflow
            else if (value < -9.9)  //Chec for underflow
            {
                val = -9.9f;
            }
            else val = (float)value;    //assignment here prevents over/underflow
            if (val < 0)                //Check for a negative value
            {
                val *= -1;    //flip for calculations/drawing
                negative = true;
            }
            //get the decimal digit(s)
            float dec;
            float y;
            int offset = 0;
            if (places == 2)
            {
                offset = 1;
                dec = (val * 100f) % 10f;
                y = ((float)dec * 0.091f);
                GUI.DrawTextureWithTexCoords(new Rect((right - width) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
                dec = (int)(val * 10f) % 10f;
                y = dec * 0.091f;
                GUI.DrawTextureWithTexCoords(new Rect((right - width*2) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
            }
            else
            {
                dec = (val * 10f) % 10f;
                y = (dec * 0.091f);
                GUI.DrawTextureWithTexCoords(new Rect((right - width) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
            }
            
            //Now draw each digit of our output, starting from the right
            for (int i = 1; i < 3; i++)
            {
                if ((i == 2) && (negative)) //if we're on the last digit, print the - if ncessary
                {
                    GUI.DrawTexture(new Rect((right - (width * (3+offset)) - 8) * Scale, top * Scale, width * Scale, height * Scale), Resources.minus);
                    break;  //don't draw the last digit
                }
                int divisor = (int)Mathf.Pow(10f, i - 1);
                int x = (int)(val / divisor);
                x = x % 10;
                y = ((float)x * 0.091f);
                GUI.DrawTextureWithTexCoords(new Rect((right - (width * (i + 1+offset)) - 8) * Scale, top * Scale, width * Scale, height * Scale), Resources.digits, new Rect(0f, y, 1f, 0.091f));
            }
        }

        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("AirPosition");
            isMinimized = config.GetValue<bool>("AirMinimized", false);
            Scale = (float)config.GetValue<double>("AirScale", 0.5);
            criticalAOA = (float)config.GetValue<double>("AirAoA", 25);
            _minMach = (float)config.GetValue<double>("AirMinMach", 0.4);
            useEAS = config.GetValue<bool>("AirUseEAS", true);
        }

        public override void save(PluginConfiguration config)
        {
            config.SetValue("AirPosition", windowPosition);
            config.SetValue("AirMinimized", isMinimized);
            config.SetValue("AirScale", (double)Scale);
            config.SetValue("AirMinMach", (double)_minMach);
            config.SetValue("AirAoA", criticalAOA);
            config.SetValue("AirUseEAS", useEAS);
        }
    }
}
