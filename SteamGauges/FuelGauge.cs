using UnityEngine;
using System.Collections.Generic;
using KSP.IO;

namespace SteamGauges
{
    class FuelGauge : Gauge
    {
        
        private float fuel_red_cutoff = 0.01f;                 //Below this percent, the fuel gauge will light up red
        private float fuel_yellow_cutoff= 0.25f;               //Below this percent, the fuel gauge will light up yellow
        private float fuel_green_cutoff = 1;                   //Below this percetn, the fuel gauge will light up green
        private float mono_red_cutoff = 0.01f;                 //Below this percent, the mono gauge will light up red
        private float mono_yellow_cutoff = 0.25f;              //Below this percent, the mono gauge will light up yellow
        private float mono_green_cutoff = 1;                   //Below this percent, the mono gauge will light up green
        private bool stageFuel = false;                        //If true, display only active stage fuel

        public override string getTextureName() { return "fuel"; }
        public override string getTooltipName() { return "Fuel Gauge"; }

        public bool getStagetFuel()
        {
            return stageFuel;
        }
        public float getFuelRed()
        {
            return fuel_red_cutoff;
        }
        public float getFuelYellow()
        {
            return fuel_yellow_cutoff;
        }
        public float getFuelGreen()
        {
            return fuel_green_cutoff;
        }
        public float getMonoRed()
        {
            return mono_red_cutoff;
        }
        public float getMonoYellow()
        {
            return mono_yellow_cutoff;
        }
        public float getMonoGreen()
        {
            return mono_green_cutoff;
        }
      

        public void setStageFuel(bool v)
        {
            stageFuel = v;  //No validation here!
        }
        public void setFuelRed(float v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            fuel_red_cutoff = v;
        }
        public void setFuelYellow(float v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            fuel_yellow_cutoff = v;
        }
        public void setFuelGreen(float v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            fuel_green_cutoff = v;
        }
        public void setMonoRed(float v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            mono_red_cutoff = v;
        }
        public void setMonoYellow(float v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            mono_yellow_cutoff = v;
        }
        public void setMonoGreen(float v)
        {
            if (v < 0) v = 0;
            if (v > 1) v = 1;
            mono_green_cutoff = v;
        }

        //Draw if not minimized
        protected override bool isVisible()
        {
            return !this.isMinimized;
        }

        //Gauge specific actions
        protected override void GaugeActions()
        {
            // This code only draws stuff, no need to handle other events
            if (Event.current.type != EventType.Repaint)
                return;
            //Draw the face (background)
            GUI.DrawTextureWithTexCoords(new Rect(36f * Scale, 75f * Scale, 323 * Scale, 247 * Scale), texture, new Rect(0.04625f, 0.0571f, 0.40125f, 0.3514f));
            //Draw the monopropellant needle and status light
            monoStatus();
            //Draw the fuel needle and status light
            fuelStatus();
            //Draw the bezel if wanted
            if (SteamGauges.drawBezels)
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400 * Scale, 407 * Scale), texture, new Rect(0f, 0.4186f, 0.5f, 0.5814f));
            //Draw the casing (foreground)
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400 * Scale, 407 * Scale), texture, new Rect(0.5f, 0.4186f, 0.5f, 0.5814f));
        }

        //Determines the current and max fuel (not oxidizer) of the active vessel, and the required needle position
        private void fuelStatus()
        {
            /*
            double current = 0;
            double max = 0;
            Vessel active = FlightGlobals.ActiveVessel;
            if (!stageFuel)     //Global calculation
            {
                foreach (Part p in active.parts)
                {
                    if (active.vesselType == VesselType.EVA)
                    {
                        foreach (PartModule PM in p.Modules)
                        {
                            if (PM is KerbalEVA)
                            {
                                KerbalEVA eva = PM as KerbalEVA;
                                current = eva.Fuel;
                                max = eva.FuelCapacity;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (PartResource pr in p.Resources)
                        {
                            if (pr.resourceName.Equals("LiquidFuel"))
                            {
                                current += pr.amount;
                                max += pr.maxAmount;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                //Do crazy stage only calculations here
                int stage = active.currentStage;
                foreach (Part p in active.parts)
                {
                    //Staging.CurrentStage
                    //if (p.Resources.Contains("LiquidFuel"))
                    //{
                    foreach (PartResource pr in p.Resources)
                    {
                        if (pr.resourceName.Equals("LiquidFuel"))
                        {
                            current += pr.amount;
                            max += pr.maxAmount;
                            break;
                        }
                    }
                    //}
                }
            } 
            if (max == 0) max = 1;  //prevents divide by 0
            //Should check for some kind of "OFF" flag for vessels w/no fuel
            //print("Found " + electricCharge + "/" + electricMax + " charge.");
            double percent = current / max;
            */
            double percent = 0;
            if (FlightGlobals.ActiveVessel.vesselType == VesselType.EVA)
                percent = SteamShip.EVAFuelPercent;
            else
                percent = SteamShip.FuelPercent;
            //double percent = SteamShip.liquidFuel / SteamShip.liquidMax;
            //Status light
            if (percent <= fuel_red_cutoff)   //these values are pulled from the config file
            {
                GUI.DrawTextureWithTexCoords(new Rect(60f*Scale, 133f*Scale, 15*Scale, 15*Scale), texture, new Rect(0.6775f, 0.3443f, 0.01875f, 0.0214f));
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.fuel_red.width * Scale, Resources.fuel_red .height*Scale), Resources.fuel_red);
            }
            else if (percent <= fuel_yellow_cutoff)
            {
                GUI.DrawTextureWithTexCoords(new Rect(62f * Scale, 134f * Scale, 15 * Scale, 15 * Scale), texture, new Rect(0.615f, 0.3443f, 0.01875f, 0.0214f));
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.fuel_yellow.width * Scale, Resources.fuel_yellow .height*Scale), Resources.fuel_yellow);
            }
            else if (percent <= fuel_green_cutoff)
            {
                GUI.DrawTextureWithTexCoords(new Rect(62f * Scale, 134f * Scale, 15 * Scale, 15 * Scale), texture, new Rect(0.555f, 0.3443f, 0.01875f, 0.0214f));
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.fuel_green.width * Scale, Resources.fuel_green.height*Scale), Resources.fuel_green);
            }
            //There are 72 degrees, split evenly left and right of 50%
            float deg = 72f * (float)percent;
            //The needle starts off pointing up, 
            deg -= 36f;
            //150*5 pixel needle
            Vector2 pivotPoint = new Vector2(186f*Scale, 326f*Scale);    //bottom edge of the case
            GUIUtility.RotateAroundPivot(deg, pivotPoint);
            //GUI.DrawTexture(new Rect(179f * Scale, 120f * Scale, Resources.fuel_needle.width * Scale, Resources.fuel_needle.height*Scale), Resources.fuel_needle);
            GUI.DrawTextureWithTexCoords(new Rect(179f * Scale, 120f * Scale, 14f * Scale, 220f * Scale), texture, new Rect(0.805f, 0.0643f, 0.0175f, 0.3143f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation
        }


        //Determines the current and max mono propellant of the active vessel, and the required needle position
        private void monoStatus()
        {
            /*
            double current = 0;
            double max = 0;
            Vessel active = FlightGlobals.ActiveVessel;
            foreach (Part p in active.parts)
            {
                //if (p.Resources.Contains("MonoPropellant"))
                //{
                    foreach (PartResource pr in p.Resources)
                    {
                        if (pr.resourceName.Equals("MonoPropellant"))
                        {
                            current += pr.amount;
                            max += pr.maxAmount;
                            break;
                        }
                    }
                //}
            }
            if (max == 0) max = 1;  //prevents divide by 0
            //Should check for an "OFF" type flag for vessals w/no mono pro
            double percent = current / max;
            */ 
            double percent = SteamShip.MonoPercent;
            //double percent = SteamShip.monoFuel / SteamShip.monoMax;
            //Status light
            if (percent <= mono_red_cutoff)   //these values are pulled from the config file
            {
                GUI.DrawTextureWithTexCoords(new Rect(61f * Scale, 251f * Scale, 15 * Scale, 15 * Scale), texture, new Rect(0.6775f, 0.3443f, 0.01875f, 0.0214f));
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.fuel_mono_red.width*Scale, Resources.fuel_mono_red.height*Scale), Resources.fuel_mono_red);
            }
            else if (percent <= mono_yellow_cutoff)
            {
                GUI.DrawTextureWithTexCoords(new Rect(62f * Scale, 252f * Scale, 15 * Scale, 15 * Scale), texture, new Rect(0.615f, 0.3443f, 0.01875f, 0.0214f));
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.fuel_mono_yellow.width * Scale, Resources.fuel_mono_yellow.height*Scale), Resources.fuel_mono_yellow);
            }
            else if (percent <= mono_green_cutoff)
            {
                GUI.DrawTextureWithTexCoords(new Rect(62f * Scale, 252f * Scale, 15 * Scale, 15 * Scale), texture, new Rect(0.555f, 0.3443f, 0.01875f, 0.0214f));
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.fuel_mono_green.width * Scale, Resources.fuel_mono_green.height*Scale), Resources.fuel_mono_green);
            }
            //There are 72 degrees, split evently left and right of 50%
            float deg = -72f * (float)percent;
            //The needle starts off pointing down, so to correct we rotate CW 36 extra degrees
            deg += 36f;
            Vector2 pivotPoint = new Vector2(187f*Scale, 75f*Scale);    //top edge of the case
            GUIUtility.RotateAroundPivot(deg, pivotPoint);
            GUI.DrawTextureWithTexCoords(new Rect(180f * Scale, 75f * Scale, 14f * Scale, 220f * Scale), texture, new Rect(0.7825f, 0.0643f, 0.0175f, 0.3143f));  //626, 655
            //GUI.DrawTexture(new Rect(180f*Scale, 75f* Scale, Resources.mono_needle.width * Scale, Resources.mono_needle.height*Scale), Resources.mono_needle);
            GUI.matrix = Matrix4x4.identity;    //Reset rotation
        }

        //Load configuration values
        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("FuelPosition");
            isMinimized = config.GetValue<bool>("FuelMinimized");
            fuel_red_cutoff = (float) config.GetValue<double>("FuelRed", 0.1f);             //10% default
            fuel_yellow_cutoff = (float) config.GetValue<double>("FuelYellow", 0.25f);      //25% default
            fuel_green_cutoff = (float)config.GetValue<double>("FuelGreen", 1);             //100% default
            mono_red_cutoff = (float) config.GetValue<double>("FuelMonoRed", 0.1f);         //10% default
            mono_yellow_cutoff = (float) config.GetValue<double>("FuelMonoYellow", 0.25f);  //25% default
            mono_green_cutoff = (float)config.GetValue<double>("FuelMonoGreen", 1);         //100% default
            Scale = (float) config.GetValue<double>("FuelScale", 0.5f); 
        }

        //Save configuration values
        public override void save(PluginConfiguration config)
        {
            config.SetValue("FuelPosition", windowPosition);
            config.SetValue("FuelMinimized", isMinimized);
            config.SetValue("FuelRed", (double) fuel_red_cutoff);
            config.SetValue("FuelYellow", (double) fuel_yellow_cutoff);
            config.SetValue("FuelGreen", (double)fuel_green_cutoff);
            config.SetValue("FuelMonoRed", (double) mono_red_cutoff);
            config.SetValue("FuelMonoYellow", (double) mono_yellow_cutoff);
            config.SetValue("FuelMonoGreen", (double)mono_green_cutoff);
            config.SetValue("FuelScale", (double) Scale);
        }
    }
}
