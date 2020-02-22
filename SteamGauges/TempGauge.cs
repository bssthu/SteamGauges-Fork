using UnityEngine;
using KSP;
using KSP.IO;
using System;

namespace SteamGauges
{
    class TempGauge : Gauge
    {
        public override string getTextureName() { return "temp"; }
        public override string getTooltipName() { return "Temp Gauge"; }

        //Draw if not minimized
        protected override bool isVisible()
        {
            return !this.isMinimized;
        }

        //Gauge specific actions
        protected override void GaugeActions()
        {
            // This gauge only draws stuff, no need to handle other events
            if (Event.current.type != EventType.Repaint)
                return;
            //Debug.Log("(SG) Temp Gauge Actions");
            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0.5f, 0.5f, 0.5f));
            //Draw the face (background)
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0f, 0.5f, 0.5f));
            //Draw the digits
            drawDigits();
            //draw the temperature needle
            drawTempNeedle();
            //Draw ablation scale
            drawAblation();
            //Draw the outer case
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407 * Scale), texture, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
        }

        //Draws the needle indicating max part temperature
        private void drawTempNeedle()
        {
            //We rotate from left 29⁰ for 0% to right 31⁰ for 100%
            //That is 60⁰ total
            double rotation = SteamShip.MaxPartTemp * 60;
            //Now correct for the needle starting at vertical
            rotation -= 29;
            //Debug.Log("(SG) Temp percent "+(int)(SteamShip.MaxPartTemp*100)+"% Needle rotation: "+(int)rotation);
            Vector2 pivotPoint = new Vector2(200 * Scale, 325 * Scale);    //needle rotation point
            GUIUtility.RotateAroundPivot((float)(rotation), pivotPoint);
            GUI.DrawTextureWithTexCoords(new Rect(195f * Scale, 104f * Scale, 10f * Scale, 240f * Scale), texture, new Rect(0.7850f, 0.0700f, 0.0125f, 0.2838f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }

        //Covers the ablation lights as appropriate
        private void drawAblation()
        {
            //Debug.Log("(SG) Drawing ablation "+(SteamShip.AblationRem*100)+"%");
            //each if statement draws the grey square over the appropriate light
            if (SteamShip.AblationRem < .9)
                GUI.DrawTextureWithTexCoords(new Rect(244f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .8)
                GUI.DrawTextureWithTexCoords(new Rect(232f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .7)
                GUI.DrawTextureWithTexCoords(new Rect(220f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .6)
                GUI.DrawTextureWithTexCoords(new Rect(208f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .5)
                GUI.DrawTextureWithTexCoords(new Rect(196f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .4)
                GUI.DrawTextureWithTexCoords(new Rect(184f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .3)
                GUI.DrawTextureWithTexCoords(new Rect(173f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .2)
                GUI.DrawTextureWithTexCoords(new Rect(161f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem < .1)
                GUI.DrawTextureWithTexCoords(new Rect(149f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
            if (SteamShip.AblationRem == 0)
                GUI.DrawTextureWithTexCoords(new Rect(137f * Scale, 236f * Scale, 9f * Scale, 23f * Scale), texture, new Rect(0.69125f, 0.1167f, 0.01125f, 0.0283f));
        }


        //Draws the 4 temperature digits
        private void drawDigits()
        {
            //Get each of the digits
            float ones;
            int tens, hundreds, thousands;
            thousands = (int) SteamShip.MaxPartTempActual / 1000;
            hundreds = (int) (SteamShip.MaxPartTempActual % 1000) / 100;
            tens = (int)(SteamShip.MaxPartTempActual % 100) / 10;
            ones = (float) SteamShip.MaxPartTempActual % 10;
            //Debug.Log("(SG) Part Temp: "+SteamShip.MaxPartTempActual+" = "+thousands.ToString()+hundreds.ToString()+tens.ToString()+ones.ToString());
            //draw thousands
            GUI.DrawTextureWithTexCoords(new Rect(150f * Scale, 139f * Scale, 20f * Scale, 29f * Scale), texture, new Rect(.56625f, .0147f + (0.0356f * thousands), 0.025f, 0.0356f));
            //draw hundreds
            GUI.DrawTextureWithTexCoords(new Rect(176f * Scale, 139f * Scale, 20f * Scale, 29f * Scale), texture, new Rect(.56625f, .0147f + (0.0356f * hundreds), 0.025f, 0.0356f));
            //draw tens
            GUI.DrawTextureWithTexCoords(new Rect(200f * Scale, 139f * Scale, 20f * Scale, 29f * Scale), texture, new Rect(.56625f, .0147f + (0.0356f * tens), 0.025f, 0.0356f));
            //draw ones
            GUI.DrawTextureWithTexCoords(new Rect(225f * Scale, 139f * Scale, 20f * Scale, 29f * Scale), texture, new Rect(.56625f, .0147f + (0.0356f * ones), 0.025f, 0.0356f));
        }

        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("TempPosition");
            isMinimized = config.GetValue<bool>("TempMinimized", true);
            Scale = (float)config.GetValue<double>("TempScale", 0.5);
        }

        //There is nothing to save for this gauge.
        public override void save(PluginConfiguration config)
        {
            config.SetValue("TempPosition", windowPosition);
            config.SetValue("TempMinimized", isMinimized);
            config.SetValue("TempScale", Scale);
        }
    }
}
