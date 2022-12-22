using UnityEngine;
using KSP.IO;
using KSP_Log;

namespace SteamGauges
{
    class NavGauge : Gauge
    {
        private Rect nav_select;

        public override string getTextureName() { return "nav"; }
        public override string getTooltipName() { return "Nav Gauge"; }


        //Draw unless minimized
        protected override bool isVisible()
        {
            //if (!this.isMinimized && FinePrint.WaypointManager.navIsActive())
            if (this.isMinimized)
                return false;
            else
                return true;
        }

        //Gauge specific actions
        protected override void GaugeActions()
        {
            nav_select = new Rect(335 * Scale, 25 * Scale, 48 * Scale, 48 * Scale);
            //handle mouse input
            if (Event.current.type == EventType.MouseUp && nav_select.Contains(Event.current.mousePosition))
            {
                SteamShip.NavWaypointId = SteamShip.NavWaypointId + 1;
            }
            if (Event.current.type == EventType.Repaint)
            {
                //Draw the compass card (and bearing pointer)
                drawCard();
                //Draw the static elements
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0.5f, .5f, .5f));
                //draw the dme
                drawNumbers();
            }
        }

        //Draws the rotating compass card that always shows vessel heading up
        private void drawCard()
        {
            //Determine vessle heading
            float heading = (float) SteamShip.Heading;
            //rotate
            Vector2 pivotPoint = new Vector2(200 * Scale, 204 * Scale);     //Center of gauge
            GUIUtility.RotateAroundPivot(-1 * heading, pivotPoint);         //hopefully rotate opposite direction
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
            drawPointer(heading);
            //draw the OFF flag if necessary
            if (SteamShip.NavHeading == -1 && SteamShip.NavDist == -1)
                GUI.DrawTextureWithTexCoords(new Rect(56 * Scale, 127 * Scale, 50 * Scale, 21 * Scale), texture, new Rect(0.8012f, 0.4287f, 0.0625f, 0.0258f));
        }

        //draw the needle that points towards the waypoint
        private void drawPointer(float hdg)
        {
            //To determine relative bearing, we need vessel heading and waypoint heading
            double brng = SteamShip.NavHeading;
            if (SteamGauges.debug) Log.Info("(SG) Nav waypoint Hdg: " + brng);
            if (brng > 180)
                brng = (360d - brng)*-1d;
            brng -= hdg;
            if (brng > 180)
                brng = (360d - brng);
            if (SteamShip.NavHeading == -1) brng = 90;    //peg to 90 if "OFF"
            if (SteamGauges.debug) Log.Info("(SG) Nav waypoint Brng: " + brng*-1);
            //rotate
            Vector2 pivotPoint = new Vector2(200 * Scale, 204 * Scale);    //Center of gauge
            GUIUtility.RotateAroundPivot((float) brng, pivotPoint);
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, 0f, 0.5f, 0.5f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
        }

        //draw the numbers that show distanc to the waypoint
        private void drawNumbers()
        {
            //Draw distance first
            if (SteamGauges.debug) Log.Info("(SG) Nav waypoint dist: " + SteamShip.NavDist/1000+"km");
            double dist = SteamShip.NavDist;
            if (dist == -1) dist = 00;
            int d = 0;  //digit
            if (dist > 999900) dist = 999900; //maximum distance is 999.9km
            if (dist > 1000)
            {
                dist /= 1000;  //use km unless we're close
                //draw the k
                GUI.DrawTextureWithTexCoords(new Rect(70f * Scale, 25f * Scale, 12f * Scale, 19f * Scale), texture, new Rect(0.63125f, 0.42875f, 0.015f, 0.02334f));
            }
            else
            {
                //draw the blank
                GUI.DrawTextureWithTexCoords(new Rect(70f * Scale, 25f * Scale, 12f * Scale, 19f * Scale), texture, new Rect(0.63125f, 0.40295f, 0.015f, 0.02334f));
            }
            //Hundreds
            d = (int)(dist / 100f);
            GUI.DrawTextureWithTexCoords(new Rect(15f * Scale, 25f * Scale, 12f * Scale, 19f * Scale), texture, new Rect(0.59875f, 0.42752f - (d * 0.0295f), 0.015f, 0.02334f));
            //Tens    
            d = (int)((dist % 100f)/10f);
            GUI.DrawTextureWithTexCoords(new Rect(26f * Scale, 25f * Scale, 12f * Scale, 19f * Scale), texture, new Rect(0.59875f, 0.42752f - (d * 0.0295f), 0.015f, 0.02334f));
            //ones
            d = (int) (dist % 10f);
            GUI.DrawTextureWithTexCoords(new Rect(38f * Scale, 25f * Scale, 12f * Scale, 19f * Scale), texture, new Rect(0.59875f, 0.42752f - (d * 0.0295f), 0.015f, 0.02334f));
            //tenths
            float dd = (float) (dist % 1f)*10f;
            GUI.DrawTextureWithTexCoords(new Rect(58f * Scale, 25f * Scale, 12f * Scale, 19f * Scale), texture, new Rect(0.59875f, 0.42752f - (dd * 0.0295f), 0.015f, 0.02334f));
            //Now draw ETE  -  t = d/v
            double t = SteamShip.NavDist / FlightGlobals.ActiveVessel.horizontalSrfSpeed;
            if (t < 0) t = 359999;  //don't let us do negative times
            int hh = (int) (t / 3600);
            int mm = (int) (t % 3600 / 60);
            int ss = (int) (t % 60);
            if (t > 359999) //99:59:59
            { hh = 99; mm = 59; ss = 59; }  //set all to highest value
            if (SteamGauges.debug) Log.Info("(SG) Waypoint ETE: " + hh + ":" + mm + ":" + ss);
            //10H, 0-9
            d = (int)(hh / 10);
            GUI.DrawTextureWithTexCoords(new Rect(15f * Scale, 365f * Scale, 14f * Scale, 16f * Scale), texture, new Rect(0.7f, 0.4312f - (d * 0.0246f), 0.0175f, 0.01965f));
            //1H, 0-9
            d = (int)(hh % 10);
            GUI.DrawTextureWithTexCoords(new Rect(29f * Scale, 365f * Scale, 14f * Scale, 16f * Scale), texture, new Rect(0.7f, 0.4312f - (d * 0.0246f), 0.0175f, 0.01965f));
            //10M, 0-5
            d = mm / 10;
            GUI.DrawTextureWithTexCoords(new Rect(51f * Scale, 365f * Scale, 14f * Scale, 16f * Scale), texture, new Rect(.66375f, 0.4312f - (d * 0.0246f), 0.0175f, 0.01965f));
            //1M, 0-9
            d = mm % 10;
            GUI.DrawTextureWithTexCoords(new Rect(65f * Scale, 365f * Scale, 14f * Scale, 16f * Scale), texture, new Rect(0.7f, 0.4312f - (d * 0.0246f), 0.0175f, 0.01965f));
            //10S, 0-5
            d = ss / 10;
            GUI.DrawTextureWithTexCoords(new Rect(87f * Scale, 365f * Scale, 14f * Scale, 16f * Scale), texture, new Rect(.66375f, 0.4312f - (d * 0.0246f), 0.0175f, 0.01965f));
            //1S, 0-9
            d = ss % 10;
            GUI.DrawTextureWithTexCoords(new Rect(99f * Scale, 365f * Scale, 14f * Scale, 16f * Scale), texture, new Rect(0.7f, 0.4312f - (d * 0.0246f), 0.0175f, 0.01965f));
        }

        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("NavPosition");
            isMinimized = config.GetValue<bool>("NavMinimized",false);
            Scale = (float)config.GetValue<double>("NavScale", 0.5f);
        }

        public override void save(PluginConfiguration config)
        {
            config.SetValue("NavPosition", windowPosition);
            config.SetValue("NavMinimized", isMinimized);
            config.SetValue("NavScale", (double)Scale);
        }

    }
}
