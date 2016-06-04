using UnityEngine;
using KSP;
using KSP.IO;
using System;

namespace SteamGauges
{
    class HudGauge : MonoBehaviour
    {
        public static Rect windowPosition = new Rect(350, 200, 200, 204);       //The position for our little window (left, top, width, height)
        private static Rect lastPosition = windowPosition;                      //Used so I don't over-save
        private static bool isMinimized;                                        //Is the window currently minimized?
        private static float Scale;                                             //Scales the gauge to different sizes
        public static bool rotatePitch;                                         //If true, rotates the pitch ladder w/aircraft roll
        public static bool drawSpdAlt;                                          //If true, draws the exact speend and altitude and VVI values in the center of the HUD
        public static bool drawOrbital;                                         //If true, draws additional information around the HUD, like AP, PE, etc
        public static bool drawThrottle;                                        //If true, draws a throttle in the lower right corner of the HUD
        public static bool drawTargetInfo;                                      //If true, and a target is selected, draws target info
        public static bool drawNodeInfo;                                        //If true, and a node exists, draws node info
        public static bool drawIndicators;                                      //If true, draws indicators for SAS, RCS, Gear, Lights, and Brakes
        public static bool useGPWS;                                             //If true, annunciates GPWS warnings in the HUD
        public static bool ivaOnly;                                             //If true, only displays the HUD in IVA views
        public static int Red;                                                  //Red component of the HUD's color
        public static int Green;                                                //Green component of the HUD's color
        public static int Blue;                                                 //Blue component of the HUD's color
        private static double maxAlt = 0;                                       //maximum altitude, for GPWS computations

        public static float getScale()
        {
            return Scale;
        }

        //Set the scale, clamp between .12 and 1
        public static void setScale(float v)
        {
            if (v < 0.12f) v = 0.12f;
            if (v > 1) v = 1;
            Scale = v;
        }

        public static void Initialize()
        {
            lastPosition = windowPosition;
            RenderingManager.AddToPostDrawQueue(3, OnDraw);
        }

        //return signed angle in relation to normal's 2d plane
        //From NavyFish's docking alignment
        private static float AngleAroundNormal(Vector3 a, Vector3 b, Vector3 up)
        {
            return AngleSigned(Vector3.Cross(up, a), Vector3.Cross(up, b), up);
        }

        //-180 to 180 angle
        //From NavyFish's docking alignment
        private static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 up)
        {
            if (Vector3.Dot(Vector3.Cross(v1, v2), up) < 0) //greater than 90 i.e v1 left of v2
                return -Vector3.Angle(v1, v2);
            return Vector3.Angle(v1, v2);
        }

        //What to do when we are drawn
        public static void OnDraw()
        {
            if (!isMinimized)   //don't draw if minimized
            {
                if (!MapView.MapIsEnabled)  //Don't draw in map view
                {
                    if ((!ivaOnly) || (ivaOnly && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)))    //Only draw in IVA if IVA only toggle is on
                    {
                        //Window scaling
                        windowPosition.width = Resources.HUD_bg.width * Scale;
                        windowPosition.height = Resources.HUD_bg.height * Scale;
                        //Check window off screen
                        if ((windowPosition.xMin + windowPosition.width) < 20) windowPosition.xMin = 20 - windowPosition.width; //left limit
                        if (windowPosition.yMin + windowPosition.height < 20) windowPosition.yMin = 20 - windowPosition.height; //top limit
                        if (windowPosition.xMin > Screen.width - 20) windowPosition.xMin = Screen.width - 20;   //right limit
                        if (windowPosition.yMin > Screen.height - 20) windowPosition.yMin = Screen.height - 20; //bottom limit
                        windowPosition = GUI.Window(8911, windowPosition, OnWindow, "", SteamGauges._labelStyle); //labelStyle makes my window invisible, which is nice
                    }
                }
            }
        }

        //basically, the layout function, but also adds dragability
        private static void OnWindow(int WindowID)
        {
            //Alpha blending
            Color tmpColor = GUI.color;
            //Special thanks to a.g. from the KSP forums for his help with scaling the textures, and helping allow the coloration to work
            GUI.color = new Color((float)Red/255f, (float)Green/255f, (float)Blue/255f, SteamGauges.Alpha);
            //Draw the static elements (background)
            GUI.DrawTexture(new Rect(0f, 0f, Resources.HUD_bg.width * Scale, Resources.HUD_bg.height * Scale), Resources.HUD_bg);
            
            Vessel v = FlightGlobals.ActiveVessel;
            drawPitchLadder(v);
            drawHeadingTape(v);
            drawAirspeedTape(v);
            drawAltitudeTape(v);
            drawExtraInfo(v);
            GPWS(v);
            //Draw the bezel, if selected
            if (SteamGauges.drawBezels)
            {
               // GUI.DrawTexture(new Rect(0f, 0f, Resources.Node_bezel.width * Scale, Resources.Node_bezel.height * Scale), Resources.Node_bezel);
            }
            //Draw the casing (foreground)
            //GUI.DrawTexture(new Rect(0f, 0f, Resources.Node_fg.width * Scale, Resources.Node_fg.height * Scale), Resources.Node_fg);

            GUI.color = tmpColor;   //reset Alpha blend

            //Make it dragable
            if (!SteamGauges.windowLock)
                GUI.DragWindow();

            //Save check so we only save after draging
            if (windowPosition.x != lastPosition.x || windowPosition.y != lastPosition.y)
            {
                SteamGauges.SaveMe();
            }
        }

        //Draws additional info around the HUD, like Ap, Pe, etc
        //Taken from composite 407x96 texture
        private static void drawExtraInfo(Vessel v)
        {
            //G meter only drawn outiside "normal" limits, but otherwise gets drawn all the time
            if (v.geeForce > 2)
            {
                //Draw the "G"
                GUI.DrawTextureWithTexCoords(new Rect(840f * Scale, 640f * Scale, 44f * Scale, 26f * Scale), Resources.HUD_extras, new Rect(0.0830f, 0.9558f, 0.0430f, 0.0339f));
                //Draw the value
                drawDigits(830, 640, Math.Round(v.geeForce, 2), true, false, false);
            }
            //Orbital information
            if ((drawOrbital) && (v.altitude > 10000) && (v.orbit.ApA > 20000))    //Only display orbital info once we're on our way
            {
                //Draw static labels
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 80f*Scale, 275f*Scale), Resources.HUD_extras, new Rect(0f, 0.6419f, 0.07813f, 0.3581f));
                //Apoapsis
                drawDigits(165, 65, v.orbit.ApA, false, true, true);
                //Periapsis, if above 0
                if (v.orbit.PeA > 0)
                {
                    drawDigits(165, 100, v.orbit.PeA, false, true, true); //Periapsis
                    drawTime(165, 205, v.orbit.timeToPe);   //Time to Pe
                }
                else
                {
                    drawDigits(165, 100, 0, false, true, true);
                    drawTime(165, 205, 0);  //Time to Pe    
                }
                //Time to Ap
                drawTime(165, 150, v.orbit.timeToAp);
                //Inclination
                drawDigits(165, 240, v.orbit.inclination, false, false, false);
            }
            //Throttle
            if (drawThrottle)
            {
                //Draw static scale
                GUI.DrawTextureWithTexCoords(new Rect(932f*Scale, 359f*Scale, 92f*Scale, 375f*Scale), Resources.HUD_extras, new Rect(0.9102f, 0.0443f, 0.0898f, 0.4883f));
                float vert = (float)FlightInputHandler.state.mainThrottle * 100 * 3.24f; //Throttle *3.24
                vert = 700 - vert;  //Move up from bottom of scale
                //Throttle arrow
                GUI.DrawTextureWithTexCoords(new Rect(988 * Scale, vert * Scale, 16 * Scale, 19 * Scale), Resources.HUD_extras, new Rect(0.3027f, 0.9727f, 0.0156f, 0.0247f));
            }
            if (drawIndicators)
            {
                //SAS flag
                //VesselSAS.dampingmode
                //if (v.vesselSAS.dampingMode) GUILayout.Label("Damping");
                if (FlightInputHandler.state.killRot) GUI.DrawTextureWithTexCoords(new Rect(720 * Scale, 11 * Scale, 88 * Scale, 33 * Scale), Resources.HUD_extras, new Rect(0.2168f, 0.9440f, 0.0859f, 0.0430f));
                //RCS flag
                if (!FlightInputHandler.RCSLock) GUI.DrawTextureWithTexCoords(new Rect(255 * Scale, 11 * Scale, 88 * Scale, 34 * Scale), Resources.HUD_extras, new Rect(0.1270f, 0.9440f, 0.0859f, 0.0443f));
                foreach (Part P in v.parts)
                {
                    foreach (PartModule pm in P.Modules)
                    {
                        if (pm.moduleName.Equals("ModuleLandingGear"))
                        {
                            //print("Found gear module in " + P.partName);
                            ModuleLandingGear mlg = (ModuleLandingGear)pm;
                            if (mlg.brakesEngaged)
                            {
                                //Brakes
                                GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 110 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.4307f, 0.9349f, 0.0461f, 0.0625f));
                            }
                            if (mlg.gearState.Equals(ModuleLandingGear.GearStates.DEPLOYED))
                            {
                                //Gear deployed
                                GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 10 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.3193f, 0.9375f, 0.0461f, 0.0625f));
                            }
                            if (mlg.gearState.Equals(ModuleLandingGear.GearStates.DEPLOYING) || (mlg.gearState.Equals(ModuleLandingGear.GearStates.RETRACTING)))
                            {
                                //Inverted G - in transit
                                GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 10 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.3184f, 0.8776f, 0.0461f, 0.0625f));
                            }
                        }
                        if (pm.moduleName.Equals("ModuleLight"))
                        {
                            ModuleLight ml = (ModuleLight)pm;
                            if (ml.isOn)
                            {
                                //Lights
                                GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 60 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.3789f, 0.9362f, 0.0461f, 0.0625f));
                            }
                        }
                    }
                }
            }
            //Target Info
            if (drawTargetInfo)
            {
                ITargetable tar = FlightGlobals.fetch.VesselTarget;
                if (tar != null)
                {
                    GUI.DrawTextureWithTexCoords(new Rect(0f, 275f*Scale, 145f*Scale, 270f*Scale), Resources.HUD_extras, new Rect(0f, 0.2917f, 0.1416f, 0.3516f));
                    Vector3d aPos = v.ReferenceTransform.position;  //Control source's position
                    Vector3d tPos = tar.GetOrbit().pos;             //Rough distance
                    Transform self = FlightGlobals.ActiveVessel.ReferenceTransform;
                    Transform tartrans = FlightGlobals.fetch.VesselTarget.GetTransform();
                    double distance = 0;
                    if (FlightGlobals.fetch.VesselTarget is ModuleDockingNode)  //Use more precise distance
                    {
                        ModuleDockingNode targetDockingPort = FlightGlobals.fetch.VesselTarget as ModuleDockingNode;
                        tartrans = targetDockingPort.controlTransform;
                        tPos = targetDockingPort.controlTransform.position;
                    }
                    Vector3 OwnshipToTarget = tartrans.position - self.position;
                    distance = Vector3d.Distance(tPos, aPos) + 0.5;
                    res Result = getClosestApproach();                          //Calculates closest approach
                    drawDigits(200, 280, distance, false, true, true);                //Target Distance
                    drawDigits(200, 310, tar.GetOrbit().ApA, false, true, true);      //Target Apoapsis
                    drawDigits(200, 340, tar.GetOrbit().PeA, false, true, true);      //Target Periapsis
                    drawDigits(200, 370, tar.GetOrbit().altitude, false, true, true); //Target altitude
                    float closureV = Vector3.Dot(FlightGlobals.ship_tgtVelocity, OwnshipToTarget.normalized);
                    drawDigits2(200, 400, closureV);                //Closure speed
                    drawDigits(200, 430, Result.dist, false, true, true); //Closest Approach
                    drawTime(160, 485, Result.time);    //TT Closest Approach
                    int rinc = (int)(tar.GetOrbit().inclination - v.GetOrbit().inclination);
                    drawDigits(200, 520, rinc, true, false, false);    //Relative inclination
                    //Draw my relative velocity vectors
                    Vector3 rVel = new Vector3();
                    rVel.x = AngleAroundNormal(FlightGlobals.ship_tgtVelocity, OwnshipToTarget, self.forward);
                    rVel.y = AngleAroundNormal(FlightGlobals.ship_tgtVelocity, OwnshipToTarget, self.right);
                    float VelX = rVel.x * 10.8f;      //10.8 pix/degree, so multiply by 10.8
                    float VelY = rVel.y * 10.8f;      //These are the offsets, so modify to absolute position, times scale
                    VelX = (485 + VelX) * Scale;      //Center is 512, 384
                    VelY = (363 - VelY) * Scale;
                    //The HUD is 28 degrees wide by 60 degrees high, so +/-14 and +/-30
                    if ((Math.Abs(rVel.x) < 14) && (Math.Abs(rVel.y) < 30))
                    {
                        Rect tvvBox = new Rect(0.0918f, 0.8841f, 0.0518f, 0.0547f);
                        Rect tvvPos = new Rect(VelX, VelY, 53f * Scale, 42f * Scale);
                        GUI.DrawTextureWithTexCoords(tvvPos, Resources.HUD_extras, tvvBox);
                    }
                    if ((Math.Abs(rVel.x) > 166) && (Math.Abs(rVel.y) > 150))
                    {
                        if (rVel.x < 0) //-166 to -180, so add 180
                            VelX = (rVel.x + 180) * 10.8f;
                        else            //subtract 180 - do I need to be multiplying by -1?
                            VelX = (rVel.x - 180) * 10.8f;
                        VelX = (496 + VelX) * Scale;
                        if (rVel.y < 0)
                            VelY = (rVel.y + 180) * 10.8f;
                        else
                            VelY = (rVel.y - 180) * 10.8f;
                        VelY = (368 - VelY) * Scale;
                        Rect ivvBox = new Rect(0.1553f, 0.8854f, 0.0313f, 0.0417f);
                        Rect ivvPos = new Rect(VelX, VelY, 32f * Scale, 32f * Scale);
                        GUI.DrawTextureWithTexCoords(ivvPos, Resources.HUD_extras, ivvBox);
                    }
                }
            }
            //Maneuver Node Info
            if (drawNodeInfo)
            {
                ManeuverNode myNode = null;
                if (v.patchedConicSolver != null)
                {
                    if (v.patchedConicSolver.maneuverNodes != null)
                    {
                        if (v.patchedConicSolver.maneuverNodes.Count > 0)
                        {
                            GUI.DrawTextureWithTexCoords(new Rect(0f, 566f*Scale, 112f*Scale, 160f*Scale), Resources.HUD_extras, new Rect(0f, 0.056f, 0.1094f, 0.2083f));
                            myNode = v.patchedConicSolver.maneuverNodes.ToArray()[0];
                            Vector2 nodeVec;
                            nodeVec.x = AngleAroundNormal(FlightGlobals.ship_tgtVelocity, myNode.GetBurnVector(v.orbit), v.ReferenceTransform.forward);
                            nodeVec.y = AngleAroundNormal(FlightGlobals.ship_tgtVelocity, myNode.GetBurnVector(v.orbit), v.ReferenceTransform.right);
                            //GUILayout.Label("Node x: " + Math.Round(nodeVec.x) + " y: " + Math.Round(nodeVec.y));
                            double deltaV = myNode.DeltaV.magnitude;                                       //The burn's ΔV
                            double deltaVRem = myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit).magnitude;   //Remaining ΔV in the burn
                            NavBallBurnVector bv = FlightUIController.fetch.GetComponentsInChildren<NavBallBurnVector>()[0];
                            double bt2 = 0;                             //Start at 0 time until the game crunches a value.
                            String ihtfs = bv.ebtText.text;
                            if (ihtfs.Contains("N/A"))
                                bt2 = 0;    //Insurance
                            else if (ihtfs.Length == 0)
                                bt2 = 0;    //Insurance
                            else
                            {
                                ihtfs = ihtfs.Substring(ihtfs.IndexOf(':')+1);    //Strip the front half off
                                String[] splits = ihtfs.Split(' ');
                                foreach (String str in splits)
                                {
                                    if (str.Contains("h"))
                                        bt2 = int.Parse(str.Remove(str.IndexOf('h')))*3600;
                                    if (str.Contains("m"))
                                        bt2 += int.Parse(str.Remove(str.IndexOf('m'))) * 60;
                                    if (str.Contains("s"))
                                        bt2 += int.Parse(str.Remove(str.IndexOf('s')));
                                }
                            }
                            //Draw values
                            drawDigits(200, 570, deltaV, false, false, false);
                            drawDigits(200, 605, deltaVRem, false, false, false);
                            drawTime(160, 660, bt2);        //Draw KSP's burn time
                            //If we are past the node time, time until burn is 0
                            if (Planetarium.GetUniversalTime() < myNode.UT)
                                drawTime(160, 716, (myNode.UT - Planetarium.GetUniversalTime()) - (bt2 / 2));    //draw actual time to KSP's burn start
                            else
                                drawTime(160, 716, 0);    //Use 0 as time to burn if past node
                        }
                    }
                }
            }
            //Display intake air gauge
            float air = getIntakeAir(v);
            if (air >= 0)
            {
                //GUILayout.Label("Intake Air: " + Math.Round(air, 2));
                //GUILayout.Label("Air Flow: " + Math.Round(getAirFlow(v), 2));
                //if (flameOut(v)) GUILayout.Label("Flameout!");
            }
            //Display "Orbital" for orbital mode - upper right
            if (inOrbit(v))
            {
                GUI.DrawTextureWithTexCoords(new Rect(920f*Scale, 10f*Scale, 98f*Scale, 24f*Scale), Resources.HUD_extras, new Rect(0.8984f, 0.9558f, 0.0957f, 0.0313f));
            }
        }

        //Draws the pitch ladder and roll pointer
        //I don't like how the turning pitch ladder isn't constrained within the original rectangle.
        //Eventually, draw various vectors and targets as well
        private static void drawPitchLadder(Vessel v)
        {
            //I'm pretty sure the navball stuff came from MechJeb too.
            NavBall ball = FlightUIController.fetch.GetComponentInChildren<NavBall>();
            Quaternion vesselRot = Quaternion.Inverse(ball.relativeGymbal);
            float pitch = (vesselRot.eulerAngles.x > 180) ? (360 - vesselRot.eulerAngles.x) : -vesselRot.eulerAngles.x; //Is this an if/then assignment?
            float roll = (vesselRot.eulerAngles.z > 180) ? (360 - vesselRot.eulerAngles.z) : -vesselRot.eulerAngles.z;  //I think this converts from 0-360 to +/-180
            float yaw = vesselRot.eulerAngles.y;    //Heading
            //Velocity Vector - More MechJeb code
            /*
            Vector3d up = (v.findWorldCenterOfMass() - v.mainBody.position).normalized;
            Vector3d forward = v.GetTransform().up;
            Vector3d north = Vector3d.Exclude(up, (v.mainBody.position + v.mainBody.transform.up * (float)v.mainBody.Radius) - v.findWorldCenterOfMass()).normalized;
            Vector3d east = v.mainBody.getRFrmVel(v.findWorldCenterOfMass()).normalized;
            double velocityHeading = 180 / Math.PI * Math.Atan2(Vector3d.Dot(v.srf_velocity, east), Vector3d.Dot(v.srf_velocity, north));   //Works great.
            double velocityPitch = (180 / Math.PI * Math.Atan2(Vector3d.Dot(v.srf_velocity, up), Vector3d.Dot(v.srf_velocity, forward)));   //so close!
            double heading_diff = velocityHeading-yaw;
            while (heading_diff > 180) heading_diff -= 180;
            while (heading_diff < -180) heading_diff += 180;
            double pitch_diff = pitch - velocityPitch;
            while (pitch_diff > 90) pitch_diff -= 90;
            while (pitch_diff < -90) pitch_diff += 90;
            //Draw a vector
            double VelX = heading_diff * 10.8;      //10.8 pix/degree, so multiply by 10.8
            double VelY = pitch_diff * 10.8;      //These are the offsets, so modify to absolute position, times scale
            VelX = (485 + VelX) * Scale;      //Center is 512, 384
            VelY = (363 + VelY) * Scale;
            Rect vvBox = new Rect(0.0918f, 0.8841f, 0.0518f, 0.0547f);
            Rect vvPos = new Rect((float) VelX, (float) VelY, 53f * Scale, 42f * Scale);
            GUI.DrawTextureWithTexCoords(vvPos, Resources.HUD_extras, vvBox);
            //double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, attitudeGetReferenceRotation(reference) * direction));
            
            //double flightPathAngle = 180 / Math.PI * Math.Atan2(v.verticalSpeed, v.heightFromSurface);
            //GUILayout.Label("FPV A: " + Math.Round(velocityPitch) + " H: " + Math.Round(velocityHeading));
            */
            //Draw vertical mode if pitch is greater than 70
            if (pitch > 70)
            {
                //Rotate by yaw, then move down by pitch
                Vector2 pivot = new Vector2(513 * Scale, 385 * Scale);
                GUIUtility.RotateAroundPivot(yaw, pivot);
                //70 is 647, 90 is 447, so 20 deg/200 pix = 10 pix / deg, or 0.0111% per deg
                float bottom = 0.3333f - (pitch * 0.0111f);
                //GUI.DrawTextureWithTexCoords(new Rect(362 * Scale, 232 * Scale, 300 * Scale, 300 * Scale), Resources.HUD_vert, new Rect(0.3333f, bottom, 0.3333f, 0.3333f));
                GUI.matrix = Matrix4x4.identity;
            }
            //Roll scale
            if (Math.Abs(roll) > 35)  //Draw full arc only for moderate bank angles
            {
                GUI.DrawTextureWithTexCoords(new Rect(422*Scale, 77*Scale, 181*Scale, 78*Scale), Resources.HUD_extras, new Rect(0.2256f, 0.7031f, 0.1786f, 0.1016f));
            }
            if (Math.Abs(roll) > 85)  //Draw bottom half only for extreme angles
            {
                GUI.DrawTextureWithTexCoords(new Rect(417 * Scale, 149 * Scale, 193 * Scale, 106 * Scale), Resources.HUD_extras, new Rect(0.2207f, 0.5547f, 0.1885f, 0.1380f));
            }
            //Draw roll pointer
            Vector2 pivotPoint = new Vector2(512 * Scale, 154 * Scale);    //Center of the arc  513, 154 original
            GUIUtility.RotateAroundPivot((float)(roll), pivotPoint);
            //Draw the arrow
            GUI.DrawTexture(new Rect(0f, 0f, 1024f*Scale, 768f*Scale), Resources.HUD_roll_ptr); //Reinstated
            //GUI.DrawTextureWithTexCoords(new Rect(428*Scale, 65*Scale, 170*Scale, 176*Scale), Resources.HUD_extras, new Rect(0.4180f, 0.6862f, 0.1660f, 0.2292f));
            GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
            //Draw pitch ladder, unless in vertical ascent mode
            if (true) //(pitch <= 70)   //change back for vertical ascent mode
            {
                //Rotate for pitch ladder, if selected
                if (rotatePitch)
                {
                    pivotPoint = new Vector2(513 * Scale, 385 * Scale); //Center of HUD
                    GUIUtility.RotateAroundPivot((float)(-1 * roll), pivotPoint);
                }
                //Each degree is 10.83 pixels
                //1300 is the center, but we start 325 pixels above that, at 975 in order to get 30* above and below our pitch
                float top = 975 - (float)(pitch * -10.833333);
                top /= 2600;   //Convert to a percentage
                GUI.DrawTextureWithTexCoords(new Rect(362 * Scale, 59 * Scale, 300 * Scale, 650 * Scale), Resources.HUD_ladder, new Rect(0f, top, 1f, 0.25f));
                GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
            }
            //In Orbital mode, display pitch in terms of FPV.  Roll might still be towards surface?
        }

        //Draws the heading tape, current heading, and heading bug to the HUD
        private static void drawHeadingTape(Vessel v)
        {
            //Taken from MechJeb via KSP forums
            double vesselHeading1;
            Vector3d CoM, MoI, up;
            Quaternion rotationSurface, rotationVesselSurface;
            Vessel vessel = FlightGlobals.ActiveVessel;
            CoM = vessel.findWorldCenterOfMass();
            MoI = vessel.findLocalMOI(CoM);
            up = (CoM - vessel.mainBody.position).normalized;
            Vector3d north = Vector3.Exclude(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized;
            rotationSurface = Quaternion.LookRotation(north, up);
            rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.transform.rotation) * rotationSurface);
            vesselHeading1 = rotationVesselSurface.eulerAngles.y;
            //End MechJeb code
            float offset = 0;
            vesselHeading1 *= 0.00238;          //Conver into percentage left
            offset += (float)vesselHeading1;    //Move left that percentage
            //Draw just the part we need
            GUI.DrawTextureWithTexCoords(new Rect(261 * Scale, 718 * Scale, 500 * Scale, 50 * Scale), Resources.HUD_compass, new Rect(offset, 0f, 0.1429f, 1f));
            //In Orbital mode, display heading relative to FPV
        }

        //Draws the airspeed tape, current airspeed, and ground speed to the HUD
        private static void drawAirspeedTape(Vessel v)
        {
            //Auto switchin between surface and orbital velocities
           double vel = 0;
           if (inOrbit(v))
               vel = v.obt_velocity.magnitude; //Orbital speeds
           else
           {
               vel = v.srf_velocity.magnitude; //Surface speeds
               drawDigits(295, 610, v.horizontalSrfSpeed, false, false, true);  //Ground speed doesn't matter IN SPAAAAACE!
           }
            float offset = 0;
            if (vel < 200)
            {
                //13.7 pix/m/s, start at 0, displaying 0.16722%
                offset = (float) ((200-vel) * 13.7);      //Start this many pixels from the top
                offset = offset / 3289;             //Convert to a percentage of the texture
                GUI.DrawTextureWithTexCoords(new Rect(182*Scale, 59*Scale, 150*Scale, 550*Scale), Resources.HUD_speed_tape1, new Rect(0f, offset, 1f, 0.167f));   //0.16722
            }
            else if (vel < 1000)
            {
                //2.74 pix/m/s, start at 200, displaying 0.2%
                offset = (float)(1000 - vel)/1000;
                GUI.DrawTextureWithTexCoords(new Rect(182*Scale, 59*Scale, 150*Scale, 550*Scale), Resources.HUD_speed_tape2, new Rect(0f, offset, 1f, 0.2f));
            }
            else if (vel < 3000)
            {
                offset = (float)(1 - (((vel - 600) * 1.37)/3290));
                GUI.DrawTextureWithTexCoords(new Rect(182*Scale, 59*Scale, 150*Scale, 550*Scale), Resources.HUD_speed_tape3, new Rect(0f, offset, 1f, 0.16722f));
            }
            else if (vel < 10000)
            {
                offset = 1 - (float)(((vel-1000)*.274)/2475);
                GUI.DrawTextureWithTexCoords(new Rect(182*Scale, 59*Scale, 150*Scale, 550*Scale), Resources.HUD_speed_tape4, new Rect(0f, offset, 1f, 0.2214f));
            }
            else
            {
                drawDigits(302, 325, vel, false, true, true);     //Just draw the speed at this point.
            }
            //Draw the digital readout
            if (drawSpdAlt)
            {
                //Draw the exact airspeed
                drawDigits(460, 335, vel, false, false, true);                            //TAS
            }
        }

        //Draws the altitude tape, current altitude, VVI, and radar altitude to the HUD
        private static void drawAltitudeTape(Vessel v)
        {
            float alt =(float) v.mainBody.GetAltitude(v.CoM);
            if (alt < 2000)
            {
                float offset = 1-((2400-alt)*1.37f)/3297;
                GUI.DrawTextureWithTexCoords(new Rect(721 * Scale, 59 * Scale, 150 * Scale, 550 * Scale), Resources.HUD_alt_tape1, new Rect(0f, offset, 1f, 0.1662f));
            }
            else if (alt < 15000)
            {
                float offset = 1 - ((17000 - alt) * .274f) / 4119;
                GUI.DrawTextureWithTexCoords(new Rect(721 * Scale, 59 * Scale, 150 * Scale, 550 * Scale), Resources.HUD_alt_tape2, new Rect(0f, offset, 1f, 0.133f));
            }
            else if (alt < 40000)
            {
                float offset = (alt - 15000) / 29000;
                GUI.DrawTextureWithTexCoords(new Rect(721 * Scale, 59 * Scale, 150 * Scale, 550 * Scale), Resources.HUD_alt_tape3, new Rect(0f, offset, 1f, 0.1376f));
            }
            else if (alt < 150000)
            {
                float offset = (alt - 39675) / 130331;
                GUI.DrawTextureWithTexCoords(new Rect(721 * Scale, 59 * Scale, 150 * Scale, 550 * Scale), Resources.HUD_alt_tape4, new Rect(0.0025f, offset, 1f, 0.1535f));
            }
            else
            {
                drawDigits(800, 325, v.mainBody.GetAltitude(v.CoM), false, true, true);     //Just draw the altitude at this point.
            }
            //Draw the interior readouts, of wanted
            if (drawSpdAlt)
            {
                drawDigits(615, 335, v.mainBody.GetAltitude(v.CoM), false, true, true);   //MSL
                drawDigits(615, 355, v.verticalSpeed, true, false, false);                 //VVI
            }
            if (!inOrbit(v))
            {
                double terrain = v.terrainAltitude;
                if (terrain < 0) terrain = 0;   //Fixes RA over the ocean.
                long ra = (long)(v.mainBody.GetAltitude(v.CoM) - terrain);
                drawDigits(830, 610, ra, false, true, true);                                  //RA
            }
        }

        //Checks the vessel to see if ground impact is immenant, and displays warnings if necessary
        private static void GPWS(Vessel v)
        {
            //Don't do anything if GPWS is off.
            if (!useGPWS) return;
            bool warning = false;
            double terrain = v.terrainAltitude;
            if (terrain < 0) terrain = 0;   //Fixes RA over the ocean.
            long ra = (long)(v.mainBody.GetAltitude(v.CoM) - terrain);
            double vvi = -1*v.verticalSpeed;
            if (v.Landed)
                maxAlt = v.altitude;  //Reset max altitude upon landing/takeoff
            else if (ra > 3)
            {
                //Mode 1: Excessive Descent Rate
                //M1A: "SINKRATE"
                if (ra < 762)
                {
                    double trigger = 48.1 * vvi - 245.3;
                    if (trigger > ra)
                    {
                        warning = true;
                        //GUILayout.Label("M1A - Sinkrate! Trigger: " + Math.Round(trigger));
                        //SINK RATE
                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 183 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.9531f, 0.1787f, 0.0391f));
                    }
                }
                //M1B: "PULL UP"
                if (!warning && (ra < 762))
                {
                    //alt = 23.9vvi - 121.9
                    double trigger = 23.9 * vvi - 121.9;
                    if (trigger > ra)
                    {
                        warning = true;
                        //GUILayout.Label("M1B - PULL UP! Trigger: " + Math.Round(trigger));
                        //PULL UP
                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 144 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.9089f, 0.1406f, 0.0391f)); 
                    }
                }
                //M2: "TERRAIN PULL UP"
                if (!warning && (ra < 457))
                {
                    if (vvi > 30.6)
                    {
                        warning = true;
                        //GUILayout.Label("M2 - PULL UP! VVI > 30.6");
                        //PULL UP
                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 144 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.9089f, 0.1406f, 0.0391f)); 
                    }
                    else
                    {
                        //alt = 19.9*vvi - 152.2
                        double trigger = 19.9 * vvi - 152.2;
                        if (trigger > ra)
                        {
                            warning = true;
                            //GUILayout.Label("M2: - PULL UP! Trigger: " + Math.Round(trigger));
                            //PULL UP
                            GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 144 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.9089f, 0.1406f, 0.0391f)); 
                        }
                    }
                }
                //M3: "DON'T SINK"
                if (v.altitude > maxAlt) maxAlt = v.altitude;
                if (!warning && (ra < 457.2) && (maxAlt - v.altitude > 45.7))
                {
                    warning = true;
                    //GUILayout.Label("M3: DON'T SINK!");
                    //DON'T SINK
                    GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 205 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.8646f, 0.2002f, 0.0391f)); 
                }
                else if (!warning)
                {
                    double trigger = 10 * vvi;
                    if (trigger > ra)
                    {
                        warning = true;
                        //GUILayout.Label("M3: DON'T SINK! Trigger: " + Math.Round(trigger));
                        //DON'T SINK
                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 205 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.8646f, 0.2002f, 0.0391f)); 
                    }
                }
                //M4: "TOO LOW GEAR"
                if (!warning && (ra < 304.8) && (vvi  > -1))
                {
                    bool tlg = false;
                    foreach (Part P in v.parts)
                    {
                        foreach (PartModule pm in P.Modules)
                        {
                            if (pm.moduleName.Equals("ModuleLandingGear"))
                            {
                                ModuleLandingGear mlg = (ModuleLandingGear)pm;
                                if (!mlg.gearState.Equals(ModuleLandingGear.GearStates.DEPLOYED))
                                {
                                    if (v.srf_velocity.magnitude > 97.7)
                                    {
                                        warning = true;
                                        //GUILayout.Label("M4: TOO LOW TERRAIN");
                                        tlg = true;
                                        //TOO LOW TERRAIN
                                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 170 * Scale, 64 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.7708f, 0.1660f, 0.0833f)); 
                                    }
                                    else if ((ra < 152.4) && (v.srf_velocity.magnitude < 97.7))
                                    {
                                        warning = true;
                                        tlg = true;
                                        //GUILayout.Label("M4: TOO LOW GEAR");
                                        //TOO LOW GEAR
                                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 170 * Scale, 64 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.6745f, 0.1660f, 0.0833f)); 
                                    }
                                }
                            }
                            if (tlg) break; //Don't need to check the rest of the modules  
                        }
                        if (tlg) break; //Do'nt need to theck the rest of the parts
                    }
                }
                //M6: "BANK ANGLE"
                if (!warning && (ra < 1000) && (ra > 5))
                {
                    NavBall ball = FlightUIController.fetch.GetComponentInChildren<NavBall>();
                    Quaternion vesselRot = Quaternion.Inverse(ball.relativeGymbal);
                    float roll = (vesselRot.eulerAngles.z > 180) ? (360 - vesselRot.eulerAngles.z) : -vesselRot.eulerAngles.z;
                    if ((ra < 10) && (roll > 10))
                        //GUILayout.Label("M6: BANK ANGLE");
                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 217 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.6276f, 0.2119f, 0.0391f)); 
                    else if (ra < 45)
                    {
                        if (roll > ra * 1.2)
                            //GUILayout.Label("M6: BANK ANGLE");
                            GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 217 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.6276f, 0.2119f, 0.0391f)); 
                    }
                    else if (roll > 40)
                        //GUILayout.Label("M6: BANK ANGLE");
                        GUI.DrawTextureWithTexCoords(new Rect(450 * Scale, 285 * Scale, 217 * Scale, 30 * Scale), Resources.HUD_extras, new Rect(0.7012f, 0.6276f, 0.2119f, 0.0391f)); 
                }
            }
        }

        //Returns the amount of airflow being genertaed by by all intakes
        //Returns -1 if there are no active intakes
        private static float getAirFlow(Vessel v)
        {
            float air = 0;
            bool anyActive = false;
            foreach (Part P in v.parts)
            {
                foreach (PartModule PM in P.Modules)
                {
                    if (PM.moduleName.Equals("ModuleResourceIntake"))
                    {
                        ModuleResourceIntake ri = PM as ModuleResourceIntake;
                        if (ri.intakeEnabled)
                        {
                            anyActive = true;
                            air += ri.airFlow;
                        }
                    }
                }
            }
            if (!anyActive) return -1;
            return air;
        }

        //Returns the intake air stored in all parts in the vessel, replicating the display
        private static float getIntakeAir(Vessel v)
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
            return (float) air;
        }

        //Returns true if any engines are flamed out in the vessel
        private static bool flameOut(Vessel v)
        {
            foreach (Part P in v.parts)
            {
                AtmosphericEngine ae = P as AtmosphericEngine;
                if (ae != null && ae.State == PartStates.DEAD)
                    return true;
            }
            return false;
        }

        //Returns true if the engine is close to flaming out
        private static bool nearFlameOut(Vessel v, float intakeair)
        {
            foreach (Part P in v.parts)
            {
                AtmosphericEngine ae = P as AtmosphericEngine;
                if (ae != null)
                    print("Lower Limit: " + Math.Round(ae.lowerAirflowLimit, 2));
            }

            return false;
        }

        //Returns true if v is considered "in orbit" for purposes of displays.
        //For mainbodies with atmosphere, this is out of the atmosphere
        //For bodies without, it is speeds over 1000 m/s
        private static bool inOrbit(Vessel v)
        {
            CelestialBody B = v.mainBody;
            if (B.atmosphere)
            {
                if (v.altitude > B.maxAtmosphereAltitude)
                    return true;
                else return false;
            }
            else
            {
                if (v.srf_velocity.magnitude > 1000) return true;
            }
            return false;
        }

        private struct res { public double dist; public double time; }

        private static res getClosestApproach()
        {
            //Crunch closest distance
            //I cunck my orbit into 100 pieces, and find between which two chuncks the minimum occurs...
            Orbit O = FlightGlobals.ActiveVessel.GetOrbit();
            Orbit TO = FlightGlobals.fetch.VesselTarget.GetOrbit();
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
            res Result = new res();
            Result.dist = bestDist;
            Result.time = bestTime;
            return Result;
        }

        
        //This code is heavily based on FAR by Ferram4
        //Though just figuring out that there's a FlightGlobals.getExternalTemperature() helps a lot!
        private static float GetMachNumber(Vessel v)
        {
            Vector3 velocity = v.srf_velocity;  //I think I want to use this velocity for planes flying around in the atmosphere, no?
            float MachNumber = 0;    
            CelestialBody body = FlightGlobals.currentMainBody;
            float temp = FlightGlobals.getExternalTemperature((float)v.altitude, body);
            temp += 273.15f;                            //Convert to Kelvin
            //if (body.GetName() == "Jool")
            //    temp += FARAeroUtil.JoolTempOffset;   //I guess Jool has some weird stuff going on...I could just look up the tempoffset he has
            if (temp < 0.1f)
                temp = 0.1f;
            float soundspeed = temp * 401.8f;              //Calculation for speed of sound in ideal gas using air constants of gamma = 1.4 and R = 287 kJ/kg*K
            soundspeed = Mathf.Sqrt(soundspeed);

            MachNumber = velocity.magnitude / soundspeed;

            if (MachNumber < 0)
                MachNumber = 0;


            return MachNumber;
        }

        //Draws a formatted 5 digit number
        //If sign is true, draws - for negative numbers.  If false, draws 0.
        //If magnitude is true, draws m, K, or M postfix as appropriate
        private static void drawDigits(float right, float top, double value, bool Sign, bool Magnitude, bool RollOnes)
        {
            float char_width = Resources.HUD_chars.width;   //was orbit_chars
            float char_height = Resources.HUD_chars.height / 5f;
            float output = (float)Math.Abs(value);
            bool negative = false;
            if ((value < 1) && Sign) negative = true;
            //Below 100,000 m, display raw value and 'm'
            if (Magnitude && (Math.Abs(value) < 100000))
            {
                output = (float)Math.Abs(value);
                GUI.DrawTextureWithTexCoords(new Rect((right - char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.HUD_chars, new Rect(0f, 0f, 1f, 0.2f));
            }
            //Below 100,000 k, display kilometers and 'K'
            else if (Magnitude && (Math.Abs(value) < 100000000))
            {
                output = (float)(Math.Abs(value) / 1000f);
                GUI.DrawTextureWithTexCoords(new Rect((right - char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.HUD_chars, new Rect(0f, 0.2f, 1f, 0.2f));
            }
            else if (Magnitude) //Display megameters and 'M'
            {
                output = (float)(Math.Abs(value) / 1000000f);
                GUI.DrawTextureWithTexCoords(new Rect((right - char_width) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.HUD_chars, new Rect(0f, 0.4f, 1f, 0.2f));
            }
            int first_digit = 5;
            //Now draw each digit of our output, starting from the right
            for (int i = 5; i > 0; i--)
            {
                float y = 0;
                int x = 0;
                int divisor = (int)Math.Pow(10f, (i - 1));
                if ((i == 1)&&RollOnes) //Smooth rolling for last digit
                {
                    x = (int)(output / divisor);
                    x = x % 10;
                    y = ((output/divisor) * 0.091f);
                }
                else
                {
                    x = (int)(output / divisor);
                    x = x % 10;
                    y = ((float)x * 0.091f);
                }
                if ((i == first_digit) && (x == 0) && (i > 1)) { first_digit--; continue; }
                //y = ((float)x * 0.091f);
                if (Magnitude)
                    GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * (i + 1))) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
                else
                    GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * i)) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
            }
            
            //Draw sign, if present
            if (negative && Magnitude)
            {
                GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * (first_digit+2))) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.HUD_chars, new Rect(0f, 0.6f, 1f, 0.2f));
            }
            if (negative && !Magnitude)
            {
                GUI.DrawTextureWithTexCoords(new Rect((right - (char_width * (first_digit+1))) * Scale, top * Scale, char_width * Scale, char_height * Scale), Resources.HUD_chars, new Rect(0f, 0.6f, 1f, 0.2f));
            }
        }

        //Draws a formatted 3 digit number with 1 decimal place and sign 
        private static void drawDigits2(float right, float top, double value)
        {
            float width = Resources.HUD_digits.width;
            float height = Resources.HUD_digits.height / 11;
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
            int dec = (int)(val * 10) % 10;
            float y = ((float)dec * 0.091f);
            //print("Decimal: " + dec);
            GUI.DrawTextureWithTexCoords(new Rect((right - width) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
            //Now draw each digit of our output, starting from the right
            for (int i = 1; i < 4; i++)
            {
                if ((i == 3) && (negative)) //if we're on the last digit, print the - if ncessary
                {
                    GUI.DrawTextureWithTexCoords(new Rect((right - (width * 4) - 8) * Scale * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_chars, new Rect(0f, 0.6f, 1f, 0.2f));
                    //GUI.DrawTexture(new Rect((right - (width * 4) - 8) * Scale, top * Scale, width * Scale, height * Scale), Resources.minus);
                    break;  //don't draw the last digit
                }
                int divisor = (int)Mathf.Pow(10f, i - 1);
                int x = (int)(val / divisor);
                x = x % 10;
                y = ((float)x * 0.091f);
                GUI.DrawTextureWithTexCoords(new Rect((right - (width * (i + 1)) - 8) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
            }
        }

        //Draws a time in the format HHH : MM : SS
        private static void drawTime(float right, float top, double time)
        {
            double seconds = time;
            double minutes = 0;
            double hours = 0;
            float width = Resources.HUD_digits.width;
            float height = Resources.HUD_digits.height / 11;
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
            GUI.DrawTextureWithTexCoords(new Rect((right - width) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)seconds / 10;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (2 * width)) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
            //Draw colon
            GUI.DrawTextureWithTexCoords(new Rect((right - (2*width)-15) * Scale, (top-4)*Scale, width*Scale, height*Scale), Resources.HUD_chars, new Rect(0f, 0.8f, 1f, 0.2f));
            //draw minutes
            x = (int)minutes % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (3f * width) - 10) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)minutes / 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (4f * width) - 10) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits6, new Rect(0f, (float)(x * 0.143), 1f, 0.143f));
            //Draw colon
            GUI.DrawTextureWithTexCoords(new Rect((right - (4 * width) - 25) * Scale, (top-4) * Scale, width * Scale, height * Scale), Resources.HUD_chars, new Rect(0f, 0.8f, 1f, 0.2f));
            //draw hours
            x = (int)hours % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (5 * width) - 20) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)hours / 10;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (6 * width) - 20) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
            x = (int)hours / 100;
            x = x % 10;
            GUI.DrawTextureWithTexCoords(new Rect((right - (7 * width) - 20) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, (float)(x * 0.091), 1f, 0.091f));
        }

        //Toggles visability of this gauge
        public static void toggle()
        {
            isMinimized = !isMinimized;
            print("Hud gauge toggled.");
        }

        public static void load(PluginConfiguration config)
        {
            windowPosition = config.GetValue<Rect>("HUDPosition", new Rect(0f, 0f, 1024f, 768f));
            isMinimized = config.GetValue<bool>("HUDMinimized", true);
            Scale = (float)config.GetValue<double>("HUDScale", 0.65);
            rotatePitch = config.GetValue<bool>("HUDrotatePitch", true);
            drawOrbital = config.GetValue<bool>("HUDdrawOrbit", true);
            drawSpdAlt = config.GetValue<bool>("HUDdrawSpdAlt", true);
            drawTargetInfo = config.GetValue<bool>("HUDdrawTarget", true);
            drawNodeInfo = config.GetValue<bool>("HUDdrawNode", true);
            drawThrottle = config.GetValue<bool>("HUDdrawThrottle", true);
            drawIndicators = config.GetValue<bool>("HUDdrawIndicators", true);
            useGPWS = config.GetValue<bool>("HUDuseGPWS", true);
            ivaOnly = config.GetValue<bool>("HUDivaOnly", false);
            Red = config.GetValue<int>("HUDred", 0);
            Green = config.GetValue<int>("HUDgreen", 255);
            Blue = config.GetValue<int>("HUDblue", 0);
        }

        public static void save(PluginConfiguration config)
        {
            config.SetValue("HUDPosition", windowPosition);
            config.SetValue("HUDMinimized", isMinimized);
            config.SetValue("HUDScale", (double)Scale);
            config.SetValue("HUDrotatePitch", rotatePitch);
            config.SetValue("HUDdrawOrbit", drawOrbital);
            config.SetValue("HUDdrawSpdAlt", drawSpdAlt);
            config.SetValue("HUDdrawTarget", drawTargetInfo);
            config.SetValue("HUDdrawNode", drawNodeInfo);
            config.SetValue("HUDdrawThrottle", drawThrottle);
            config.SetValue("HUDdrawIndicators", drawIndicators);
            config.SetValue("HUDuseGPWS", useGPWS);
            config.SetValue("HUDivaOnly", ivaOnly);
            config.SetValue("HUDred", Red);
            config.SetValue("HUDgreen", Green);
            config.SetValue("HUDblue", Blue);
            }
    }
}
