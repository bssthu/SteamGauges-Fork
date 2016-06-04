using UnityEngine;
using KSP.IO;

namespace SteamGauges
{
    class MagneticCompass : Gauge
    {
        //Draw unless minimized
        protected override bool isVisible()
        {
            return !this.isMinimized;
        }

        protected override void GaugeActions()
        {
            // This code only draws stuff, no need to handle other events
            if (Event.current.type != EventType.repaint)
                return;
            //Draw the compass band
            drawHeading();
            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0f, .2909f, .3527f, .7091f));
            //Draw the Compass
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 400f * Scale, 407f * Scale), texture, new Rect(0.3528f, .2909f, .3527f, .7091f));
        }

        //Returns an 124x85 rectangle that covers the portion of the compass band centered on the current heading
        private void drawHeading()
        {
            //Taken from MechJeb via KSP forums
            double vesselHeading;
            Vector3d CoM, MoI, up;
            Quaternion rotationSurface, rotationVesselSurface;
            Vessel vessel = FlightGlobals.ActiveVessel;
            CoM = vessel.findWorldCenterOfMass();
            MoI = vessel.findLocalMOI(CoM);
            up = (CoM - vessel.mainBody.position).normalized;
            //Vector3d north = Vector3.Exclude(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized; //obsolete
            Vector3d north = Vector3.ProjectOnPlane(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized;
            rotationSurface = Quaternion.LookRotation(north, up);
            rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.transform.rotation) * rotationSurface);
            vesselHeading = rotationVesselSurface.eulerAngles.y;
            //End MechJeb code
            //Now that we have our heading, determine which part of the compass band we want to show
            //We have the heading, and now need to know at what percentage left we need to start
            float offset = 0.7848f;             //Start here to center N in the window
            vesselHeading *= 0.00215;          //Convert heading into percentage left
            offset -= (float) vesselHeading;   //Move left that percentage

            //Draw just the part we need
            GUI.DrawTextureWithTexCoords(new Rect(71f*Scale, 110f*Scale, 247f * Scale, 170f * Scale), texture, new Rect(offset, 0f, .2178f, .2909f));
            //GUI.DrawTextureWithTexCoords(new Rect(71 * Scale, 112 * Scale, 247 * Scale, 167 * Scale), Resources.compass_band, new Rect(offset, 0f, 0.2178f, 1f));
        }

        public override void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("CompassPosition");
            isMinimized = config.GetValue<bool>("CompassMinimized");
            Scale = (float) config.GetValue<double>("CompassScale", 0.5f);
        }

        public override void save(PluginConfiguration config)
        {
            config.SetValue("CompassPosition", windowPosition);
            config.SetValue("CompassMinimized", isMinimized);
            config.SetValue("CompassScale", (double)Scale);
        }
    }
}
