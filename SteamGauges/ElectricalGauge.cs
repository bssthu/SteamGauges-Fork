using UnityEngine;
using KSP.IO;
using System.Collections.Generic;
using System;

namespace SteamGauges
{
    class ElectricalGauge : Gauge
    {
        public override string getTextureName() { return "elec"; }
        public override string getTooltipName() { return "Electrical Gauge"; }

        //Draw if not minimized
        protected override bool isVisible()
        {
            return !this.isMinimized;
        }

        protected override void GaugeActions()
        {
            // This code only draws stuff, no need to handle other events
            if (Event.current.type != EventType.Repaint)
                return;
            //Draw the face (background)
            GUI.DrawTextureWithTexCoords(new Rect(-2f, -1f, 402f * Scale, 409f * Scale), texture, new Rect(0f, 0f, 0.5f, 0.5f));
            //Draw the needles
            capacityNeedle();
            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
            {
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0.5f, 0.5f, 0.5f));
            }
            //Draw the casing (foreground)
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
        }

        //Draws both needles!
        private void capacityNeedle()
        {
            double rate = SteamShip.ElecRate;
            float rateRotate = 0;
            //There are 13 deg per zone
            //And three zones 
            //multiply rate by 1.166667
            if (Math.Abs(rate) < 1)
            {
                rateRotate = (float) rate*-13f;
            }
            else if (Math.Abs(rate) < 10)
            {
                rateRotate = (float) rate*-2.6f;
            }
            else
                rateRotate = (float) rate*-1.166667f;                   //rate to degrees
            Vector2 pivotPoint = new Vector2(323f*Scale, 217f*Scale);   //right edge of the case
            GUIUtility.RotateAroundPivot(-1f*rateRotate, pivotPoint);   //rotate in the correct direction
            GUI.DrawTextureWithTexCoords(new Rect(109f*Scale, 210f*Scale, 220f * Scale, 14f * Scale), texture, new Rect(0.5775f, 0.3547f, 0.2703f, 0.0175f));
            GUI.matrix = Matrix4x4.identity;
            //Amount stuff
            double percent = SteamShip.ChargePercent;
            //There are 72 degrees, split evenly above and below 0 if 50% is 0
            float deg = -72f * (float) percent;
            //Now convert the percentage into degrees from 50% by subtracting 36
            deg += 36f;
            //150*5 pixel needle
            pivotPoint = new Vector2(71f*Scale, 217f*Scale);    //Left edge of the case
            GUIUtility.RotateAroundPivot(deg, pivotPoint);
            GUI.DrawTextureWithTexCoords(new Rect(72f*Scale, 210f*Scale, 220f * Scale, 14f * Scale), texture, new Rect(0.5775f, 0.3722f, 0.2703f, 0.0175f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }


        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("ElectricPosition");
            isMinimized = config.GetValue<bool>("ElectricMinimized");
            Scale = (float) config.GetValue<double>("ElectricScale");
        }

        public override void save(PluginConfiguration config)
        {
            config.SetValue("ElectricPosition", windowPosition);
            config.SetValue("ElectricMinimized", isMinimized);
            config.SetValue("ElectricScale", (double)Scale);
        }
    }
}
