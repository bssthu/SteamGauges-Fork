using UnityEngine;
using KSP;
using KSP.IO;
using KSP.UI.Screens.Flight;
using ModuleWheels;
using System;

namespace SteamGauges
{
    class HudGauge : Gauge
    {
        public bool rotatePitch;                                         //If true, rotates the pitch ladder w/aircraft roll
        public bool drawSpdAlt;                                          //If true, draws the exact speend and altitude and VVI values in the center of the HUD
        public bool drawOrbital;                                         //If true, draws additional information around the HUD, like AP, PE, etc
        public bool drawThrottle;                                        //If true, draws a throttle in the lower right corner of the HUD
        public bool drawTargetInfo;                                      //If true, and a target is selected, draws target info
        public bool drawNodeInfo;                                        //If true, and a node exists, draws node info
        public bool drawIndicators;                                      //If true, draws indicators for SAS, RCS, Gear, Lights, and Brakes
        public bool drawWarnings;                                        //If true, draws warnings for low fuel, mono, or electrical states.
        public bool useGPWS;                                             //If true, annunciates GPWS warnings in the HUD
        public bool useEAS;                                              //If true, displays EAS in the HUD instead of TAS
        public bool ivaOnly;                                             //If true, only displays the HUD in IVA views
        public bool mouseInput;                                          //If true, dragging the mouse over the hud can be used for fine control
        public bool mouseInputOnly;                                      //If true, force reduced mode.
        public bool planeMode;                                           //If true, x direction controls roll; otherwise yaw
        public bool orbitalMode;                                         //If true, draws HUD relative to orbital velocity in orbit.
        public int Red;                                                  //Red component of the HUD's color
        public int Green;                                                //Green component of the HUD's color
        public int Blue;                                                 //Blue component of the HUD's color
        public int Alpha;                                                //HUD transparency components
        public int cRed;                                                 //Red component of the center HUD data
        public int cGreen;                                               //Green ...
        public int cBlue;                                                //Blue ...
        public bool color_center;                                        //Draw central information with unique colors?
        private double maxAlt = 0;                                       //maximum altitude, for GPWS computations
        private float _minGs;                                            //The minimum value for the G meter to display
        private float _minMach;                                          //The minimum value for the mach meter to display
        private string required_technology;                              //The technology required for the HUD to display
        public KeyCode[] hudKeys;
        private bool mouseInputActive = false, mouseInputFlip = false;
        private Vector3 mouse_center;
        private Vessel mouseVessel;
        private static VesselAutopilot.VesselSAS mouseSAS;
        
        private NavBall ball;

        //This isn't ever called becuase I'm hiding the onDraw method, but it needs to be in the class none the less.
        protected override void GaugeActions()
        {
            
        }

        public void Initialize(SteamGauges sg, int id, bool enable)
        {
            home = sg;
            windowID = id;
            isEnabled = enable;
            lastPosition = windowPosition;
            sg.AddToPreDrawQueue(OnPreDraw);
            sg.AddToPostDrawQueue(OnDraw);
        }

        //return signed angle in relation to normal's 2d plane
        //From NavyFish's docking alignment
        /*private float AngleAroundNormal(Vector3 a, Vector3 b, Vector3 up)
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
        }*/

        //Centers a window in the screen
        public void centerWindow()
        {
            windowPosition.x = Mathf.Floor((Screen.width - windowPosition.width) / 2);
            windowPosition.y = Mathf.Floor((Screen.height - windowPosition.height) / 2);
        }

        // If true, the hud is completely disabled
        public bool isDisabledNow()
        {
            return isMinimized;
        }

        // If false, the hud is disabled or in reduced mode when with mouse input
        public bool isCorrectCamera()
        {
            // If in map view, use reduced mode or disable
            if (MapView.MapIsEnabled)
                return false;

            //This is trying to make it so you can't use the HUD unless Electronics is researched.
            if (ResearchAndDevelopment.GetTechnologyState(required_technology) == RDTech.State.Unavailable)
                return false;

            // If alpha is set to zero, use reduced mode or disable
            if (Alpha < 10)
                return false;

            if (!ivaOnly)
                return true;

            var mode = CameraManager.Instance.currentCameraMode;
            if (mode == CameraManager.CameraMode.IVA ||
                mode == CameraManager.CameraMode.Internal)
                return true;

            // A heuristic to detect Hullcam VDS mode: it clears target, but sets parent
            if (mode == CameraManager.CameraMode.Flight &&
                FlightCamera.fetch != null &&
                FlightCamera.fetch.Target == null &&
                FlightCamera.fetch.transform.parent != null)
                return true;

            return false;
        }

        new public void toggle()
        {
            // Cycle through non-minimized mouse-only mode if applicable
            if (isMinimized)
            {
                isMinimized = false;
                if (isCorrectCamera())
                    mouseInputOnly = false;
            }
            else if (mouseInput && !mouseInputOnly && isCorrectCamera())
                mouseInputOnly = true;
            else
                isMinimized = true;
        }

        //Because this class overrides OnDraw, this won't get called.
        protected override bool isVisible()
        {
            return true;
        }

        private void UpdateWindowPosition()
        {
            //Window scaling
            windowPosition.width = Resources.HUD_extras.width * Scale;
            windowPosition.height = Resources.HUD_extras.height * Scale;
            //Check window off screen
            if ((windowPosition.xMin + windowPosition.width) < 20) windowPosition.xMin = 20 - windowPosition.width; //left limit
            if (windowPosition.yMin + windowPosition.height < 20) windowPosition.yMin = 20 - windowPosition.height; //top limit
            if (windowPosition.xMin > Screen.width - 20) windowPosition.xMin = Screen.width - 20;   //right limit
            if (windowPosition.yMin > Screen.height - 20) windowPosition.yMin = Screen.height - 20; //bottom limit
        }

        // Rendering pass below main gui
        public void OnPreDraw()
        {
            //Check for the hud key to be pressed
            keyInput();

            bool off = isDisabledNow();

            if (off || !mouseInput || (mouseInputActive && FlightGlobals.ActiveVessel != mouseVessel))
                StopMouseInput();

            if (off)
                return;

            // When mouse input is on, draw a rudimentary interface even when otherwise off
            bool camera_ok = !mouseInputOnly && isCorrectCamera();
            bool force_show = mouseInput && !camera_ok;

            if (!SteamGauges.windowLock && !force_show)
                return;

            if (camera_ok || force_show)
            {
                UpdateWindowPosition();

                // When locked, skip the window and draw controls below all UI
                GUI.BeginGroup(windowPosition);

                if (force_show)
                    OnMiniWindow(-1);
                else
                    OnWindow(-1);

                GUI.EndGroup();
            }
        }

        //What to do when we are drawn
        new public void OnDraw()
        {
            //don't draw if minimized or locked and thus already done in OnPreDraw
            if (isDisabledNow() || SteamGauges.windowLock || !isEnabled)
                return;

            if (!mouseInputOnly && isCorrectCamera())
            {
                UpdateWindowPosition();

                windowPosition = GUI.Window(windowID, windowPosition, OnWindow, "", SteamGauges._labelStyle); //labelStyle makes my window invisible, which is nice
            }
        }

        //Keyboard input for HUD toggle
        private void keyInput()
        {
            //If no keys are pressed, we're done
            if (!Input.anyKeyDown)
                return;
            //look for the hud keys
            foreach (KeyCode key in hudKeys)
                if (!Input.GetKey(key))
                    return;
            toggle();
        }

        // Fine control by dragging mouse. Idea inspired by HydroTech Mouse Drive, but:
        // 1) Using cursor movement over an area that is already a focus of attention.
        // 2) Intended most of all for fine control, especially around neutral position.
        //    For rough large-scale movement keyboard works quite well for the most part.
        // 3) Active only with mouse button down to make the active/inactive state intuitively
        //    obvious. For long-term attitude hold there is trim, SAS and MechJeb.

        private void FlyByWireCallback(FlightCtrlState state)
        {
            if (!mouseInputActive)
                return;

            // Compute mouse position here just in case GUI pipeline lags somehow.
            Vector3 delta = Input.mousePosition - mouse_center;

            if (mouseSAS != null)
            {
                // Don't trim SAS in on-rails warp
                if (TimeWarp.CurrentRate != 1f && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
                    return;

                // SAS eats up all non-user input, so instead we adjust its heading lock
                float pitch = 0f, roll = 0f, yaw = 0f;
                ApplyInput(ref pitch, ref roll, ref yaw, delta.x, delta.y);

                // Convert user input to rate of turn based change (20 deg/sec max)
                float scale = TimeWarp.fixedDeltaTime * 20f;
                Quaternion rot = Quaternion.Euler(new Vector3(pitch, roll, yaw) * -scale);

                mouseSAS.LockHeading(mouseSAS.lockedHeading * rot, true);
            }
            else
                ApplyInput(ref state.pitch, ref state.roll, ref state.yaw, delta.x, delta.y);
        }

        private void ApplyInput(ref float pitch, ref float roll, ref float yaw, float dx, float dy)
        {
            ApplyAxis(ref pitch, dy);

            if (planeMode != mouseInputFlip)
                ApplyAxis(ref roll, dx);
            else
                ApplyAxis(ref yaw, dx);
        }

        private void ApplyAxis(ref float result, float input)
        {
            // These values control sensitivity and shouldn't be tied to gui scaling
            float effect = ScaleInput(15f, 250f, input);

            result = Mathf.Clamp(result + effect, -1f, 1f);
        }

        private float ScaleInput(float dead_zone, float range_size, float val)
        {
            // Clamp to dead zone and sensitivity range
            float diff = Mathf.Clamp((Mathf.Abs(val) - dead_zone) / range_size, 0f, 1f);
            // Nonlinear scaling to allow higher precision control near zero
            return Mathf.Pow(diff, 1.5f) * Mathf.Sign(val);
        }

        private FlightInputCallback AddHookBefore(FlightInputCallback a, FlightInputCallback b)
        {
            // Broken due to https://bugzilla.xamarin.com/show_bug.cgi?id=12536:
            //return a + b;

            if (b != null)
            {
                foreach (var bv in b.GetInvocationList())
                    a += (FlightInputCallback)bv;
            }
            return a;
        }

        private void UpdateMouseInput(Rect hotspot)
        {
            if (!mouseInputActive)
            {
                mouseVessel = FlightGlobals.ActiveVessel;
                if (mouseVessel == null) return;

                mouseInputActive = true;
                mouseInputFlip = false;

                // This is more logical, but it annoyingly complains about obsolete API
                //FlightInputHandler.OnFlyByWire += FlyByWireCallback;

                // So instead install the callback first in the list for the target vessel.
                mouseVessel.OnFlyByWire = AddHookBefore(FlyByWireCallback, mouseVessel.OnFlyByWire);

                // SAS eats up all non-user input, so instead we change its heading lock
                mouseSAS = mouseVessel.ActionGroups[KSPActionGroup.SAS] ? mouseVessel.Autopilot.SAS : null;
            }

            mouse_center = GUIUtility.GUIToScreenPoint(hotspot.center);
        }

        private void ApplyTrim(Rect hotspot, Vector2 mouse)
        {
            if (!mouseInputActive || mouseSAS != null)
                return;

            Vector2 delta = mouse - hotspot.center;
            FlightCtrlState state = FlightInputHandler.state;
            ApplyInput(ref state.pitchTrim, ref state.rollTrim, ref state.yawTrim, delta.x, -delta.y);
        }

        private void StopMouseInput()
        {
            if (!mouseInputActive)
                return;

            mouseSAS = null;

            mouseVessel.OnFlyByWire -= FlyByWireCallback;
            mouseInputActive = false;
            mouseVessel = null;
        }

        private int inputHash = "HUDMouseInput".GetHashCode();

        private void HandleMouseInput(Rect hotspot)
        {
            // Handle events like a button does. This ensures control
            // doesn't activate due to clicks on windows above this one.
            // see http://answers.unity3d.com/questions/226861/how-to-write-onmousedown-condition-for-gui-button.html
            int controlID = GUIUtility.GetControlID(inputHash, FocusType.Passive, hotspot);
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0 && hotspot.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                        UpdateMouseInput(hotspot);
                        // If Control held down at time of press, invert the Plane/Rocket choice
                        mouseInputFlip = Input.GetKey(KeyCode.RightControl);
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        // If Alt held down at time of release, apply the input to trim
                        if (GameSettings.MODIFIER_KEY.GetKey())
                            ApplyTrim(hotspot, Event.current.mousePosition);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        Event.current.Use();
                        UpdateMouseInput(hotspot);
                    }
                    break;

                default:
                    if (mouseInputActive)
                        UpdateMouseInput(hotspot);
                    break;
            }

            if (GUIUtility.hotControl != controlID)
                StopMouseInput();
        }

        //basically, the layout function, but also adds dragability
        new private void OnWindow(int WindowID)
        {
            // Only do all this stuff when requested to actually paint something
            if (Event.current.type == EventType.repaint)
                DrawDisplay(false);

            HandleInput(WindowID, 150f, 100f);
        }

        private void OnMiniWindow(int WindowID)
        {
            // Only do all this stuff when requested to actually paint something
            if (Event.current.type == EventType.repaint)
                DrawDisplay(true);

            // Only initiate mouse input in the central dead zone
            HandleInput(WindowID, 15f / Scale, 15f / Scale);
        }

        private void DrawDisplay(bool reduced)
        {
            //Alpha blending
            Color tmpColor = GUI.color;
            // Use half alpha, but not less than 0.25 for reduced mode
            float AdjAlpha = reduced ? Mathf.Max(0.25f, (float)Alpha / 255f / 2f) : (float)Alpha / 255f;
            //Special thanks to a.g. from the KSP forums for his help with scaling the textures, and helping allow the coloration to work
            GUI.color = new Color((float)Red / 255f, (float)Green / 255f, (float)Blue / 255f, AdjAlpha);

            // Draw a mark in the middle to show sensitive area AKA mouse input dead zone
            if (reduced || mouseInputActive)
                DrawTile(512 - 24f / Scale, 384 - 24f / Scale, Resources.HUD_extras, new Rect(109f, 162f, 49f, 49f), 1f / Scale);

            if (reduced)
            {
                if (mouseInputActive)
                {
                    //Draw a subset of display elements
                    Vessel v = FlightGlobals.ActiveVessel;
                    if (v != null)
                    {
                        drawPitchLadder(v, false);
                        drawHeadingTape(v);
                        drawExtraInfo(v, false);
                    }
                }
            }
            else
            {
                //Draw the static elements (background)
                //GUI.DrawTexture(new Rect(0f, 0f, Resources.HUD_bg.width * Scale, Resources.HUD_bg.height * Scale), Resources.HUD_bg);

                //Draw the dynamic elements
                Vessel v = FlightGlobals.ActiveVessel;
                if (v != null)
                {
                    drawPitchLadder(v, true);
                    drawHeadingTape(v);
                    drawAirspeedTape(v);
                    drawAltitudeTape(v);
                    drawExtraInfo(v, true);
                    GPWS(v);
                }
            }

            GUI.color = tmpColor;   //reset Alpha blend
        }

        private void HandleInput(int WindowID, float mousew, float mouseh)
        {
            //Make it dragable
            if (!SteamGauges.windowLock && WindowID >= 0 )
            {
                GUI.DragWindow();
                // Keep below all others at all times
                GUI.BringWindowToBack(WindowID);
            }
            // Otherwise do mouse input over the ladder area.
            else if (mouseInput)
                HandleMouseInput(new Rect((512 - mousew) * Scale, (384 - mouseh) * Scale, mousew * 2 * Scale, mouseh * 2 * Scale));

            //Save check so we only save after draging
            if (windowPosition.x != lastPosition.x || windowPosition.y != lastPosition.y)
            {
                lastPosition = windowPosition;
                home.SaveMe();
            }
        }

        //Draws additional info around the HUD, like Ap, Pe, etc
        private void drawExtraInfo(Vessel v, bool full)
        {
            if (full)
            {
                //Draw static elements
                GUI.DrawTextureWithTexCoords(new Rect(301 * Scale, 318 * Scale, 62 * Scale, 34 * Scale), Resources.HUD_extras, new Rect(0.6084f, 0.9518f, 0.0605f, 0.0443f));    //Airspeed Pointer
                GUI.DrawTextureWithTexCoords(new Rect(661 * Scale, 318 * Scale, 62 * Scale, 34 * Scale), Resources.HUD_extras, new Rect(0.6084f, 0.9089f, 0.0605f, 0.0443f));    //Altitude Pointer
                Color gc = GUI.color;
                Color inverse = new Color((float)cRed / 255f, (float)cGreen / 255f, (float)cBlue / 255f, gc.a);
                if (color_center)
                    GUI.color = inverse;
                GUI.DrawTextureWithTexCoords(new Rect(463 * Scale, 381 * Scale, 98 * Scale, 33 * Scale), Resources.HUD_extras, new Rect(0.4912f, 0.9479f, 0.0957f, 0.0430f));    //Waterline
                GUI.color = gc;
            }
             GUI.DrawTextureWithTexCoords(new Rect(504 * Scale, 706 * Scale, 16 * Scale, 19 * Scale), Resources.HUD_extras, new Rect(0.2812f, 0.8958f, 0.0186f, 0.0247f));    //Heading Pointer
            //G meter only drawn outiside "normal" limits, but otherwise gets drawn all the time
            if (full && v.geeForce > _minGs)
            {
                //Draw the "G"
                GUI.DrawTextureWithTexCoords(new Rect(840f * Scale, 640f * Scale, 44f * Scale, 26f * Scale), Resources.HUD_extras, new Rect(0.0830f, 0.9558f, 0.0430f, 0.0339f));
                //Draw the value
                drawDigits(830, 640, Math.Round(v.geeForce, 2), true, false, false);
            }
            //Orbital information
            if (full && (drawOrbital) && (v.altitude > 10000) && (v.orbit.ApA > 20000))    //Only display orbital info once we're on our way
            {
                //Draw static labels
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, 80f*Scale, 275f*Scale), Resources.HUD_extras, new Rect(0f, 0.6419f, 0.07813f, 0.3581f));
                //Apoapsis
                double val = v.orbit.ApA;
                if (v.orbit.eccentricity > 1.0) val = 0;
                drawDigits(165, 65, val, false, true, true);
                drawTime(165, 150, v.orbit.timeToAp);
                //Periapsis, time
                val = v.orbit.PeA;
                if (!home.orbitGauge.showNegativePe && val < 0) val = 0;                
                drawDigits(165, 100, val, true, true, true); //Periapsis
                drawTime(165, 205, v.orbit.timeToPe);   //Time to Pe                
                //Inclination
                drawDigits(165, 240, v.orbit.inclination, false, false, false);
            }
            //Throttle, unless idle
            if (full && drawThrottle && FlightInputHandler.state.mainThrottle > 0)
            {
                //Draw static scale
                GUI.DrawTextureWithTexCoords(new Rect(932f*Scale, 354f*Scale, 92f*Scale, 370f*Scale), Resources.HUD_extras, new Rect(0.9102f, 0.0443f, 0.0898f, 0.4818f));
                float vert = (float)FlightInputHandler.state.mainThrottle * 100 * 3.24f; //Throttle *3.24
                vert = 690 - vert;  //Move up from bottom of scale
                //Throttle arrow
                GUI.DrawTextureWithTexCoords(new Rect(988 * Scale, vert * Scale, 16 * Scale, 19 * Scale), Resources.HUD_extras, new Rect(0.3027f, 0.9727f, 0.0156f, 0.0247f));
            }
            //Draw dynamic TWR, cause that's kinda cool
            if (SteamShip.MaxTWR > 0)
            {
                GUI.DrawTextureWithTexCoords(new Rect(960 * Scale, 745 * Scale, 67 * Scale, 20 * Scale), Resources.HUD_extras, new Rect(0.4844f, 0.8971f, 0.0654f, 0.0260f));   //TWR
                GUI.DrawTextureWithTexCoords(new Rect(855 * Scale, 745 * Scale, 14 * Scale, 20 * Scale), Resources.HUD_extras, new Rect(0.5576f, 0.8971f, 0.0137f, 0.0260f));   // '/'
                drawDigits2x2(850, 740, SteamShip.CurrentTWR);
                drawDigits2x2(950, 740, SteamShip.MaxTWR);
            }
            //EVA fuel gauge, if in EVA
            if (full && v.vesselType == VesselType.EVA)
            {
                //Use the same scale as the throttle (different label though!)
                GUI.DrawTextureWithTexCoords(new Rect(948f * Scale, 321f * Scale, 76f * Scale, 390f * Scale), Resources.HUD_extras, new Rect(0.9258f, 0.0755f, 0.0742f, 0.5078f));
                float vert = (float)(SteamShip.EVAFuelPercent * 100 * 3.24); //Throttle *3.24
                vert = 700 - vert;  //Move up from bottom of scale
                //use the fuel arrow
                GUI.DrawTextureWithTexCoords(new Rect(1004 * Scale, vert * Scale, 16 * Scale, 19 * Scale), Resources.HUD_extras, new Rect(0.3027f, 0.9727f, 0.0156f, 0.0247f));
            }
            if (full && drawIndicators)
            {
                //SAS flag
                if (v.ActionGroups[KSPActionGroup.SAS]) GUI.DrawTextureWithTexCoords(new Rect(720 * Scale, 11 * Scale, 88 * Scale, 33 * Scale), Resources.HUD_extras, new Rect(0.2168f, 0.9440f, 0.0859f, 0.0430f));
                //RCS flag
                if (v.ActionGroups[KSPActionGroup.RCS]) GUI.DrawTextureWithTexCoords(new Rect(255 * Scale, 11 * Scale, 88 * Scale, 34 * Scale), Resources.HUD_extras, new Rect(0.1270f, 0.9440f, 0.0859f, 0.0443f));
                //Brakes Flag
                if (v.ActionGroups[KSPActionGroup.Brakes]) GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 110 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.4307f, 0.9349f, 0.0461f, 0.0625f));
                //Lights Flag
                if (v.ActionGroups[KSPActionGroup.Light]) GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 60 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.3789f, 0.9362f, 0.0461f, 0.0625f));
                int gear_state = SteamShip.GearState;
                //0 = up, 1 = extending, 2 = retracting, 3 = down
                switch (SteamShip.GearState)
                {
                    case 1:
                    case 2:
                        GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 10 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.3184f, 0.8776f, 0.0461f, 0.0625f));    //Inverted G
                        break;
                    case 3:
                        GUI.DrawTextureWithTexCoords(new Rect(970 * Scale, 10 * Scale, 48 * Scale, 48 * Scale), Resources.HUD_extras, new Rect(0.3193f, 0.9375f, 0.0461f, 0.0625f));    //G
                        break;
                    default:
                        break;
                }
            }

            // Target velocity correction
            ITargetable tar = FlightGlobals.fetch.VesselTarget;
            Vector3 tgt_velocity = FlightGlobals.ship_tgtVelocity;

            if (tar != null && tar.GetVessel() != null)
            {
                // Otherwise it seems to be equal to orbital velocity when the target
                // vessel isn't loaded (i.e. more than 2km away), which makes no sense.
                // I consider this as a bug in the stock nav-ball functionality.
                Vessel vessel = tar.GetVessel();
                if (vessel.LandedOrSplashed)
                {

                    tgt_velocity = v.GetSrfVelocity();
                    //tgt_velocity = Vector3.zero;
                    if (vessel.loaded)
                        tgt_velocity -= tar.GetSrfVelocity();
                }
            }

            //Target Info
            if (drawTargetInfo)
            {
                if (tar != null)
                {
                    Orbit orbit = tar.GetOrbit();
                    /*Transform self = FlightGlobals.ActiveVessel.ReferenceTransform;
                    Transform tartrans = FlightGlobals.fetch.VesselTarget.GetTransform();
                    Vector3d aPos = self.position;  //Control source's position
                    Vector3d tPos = tartrans.position; //Rough distance

                    double distance = 0;
                    if (FlightGlobals.fetch.VesselTarget is ModuleDockingNode)  //Use more precise distance
                    {
                        ModuleDockingNode targetDockingPort = FlightGlobals.fetch.VesselTarget as ModuleDockingNode;
                        tartrans = targetDockingPort.controlTransform;
                        tPos = targetDockingPort.controlTransform.position;
                    }

                    
                    distance = Vector3d.Distance(tPos, aPos);
                    */
                    Vector3 OwnshipToTarget = SteamShip.TargetPos - v.ReferenceTransform.position;
                    double distance = SteamShip.TargetDist;

                    if (full)
                    {
                        GUI.DrawTextureWithTexCoords(new Rect(0f, 275f * Scale, 145f * Scale, 270f * Scale), Resources.HUD_extras, new Rect(0f, 0.2917f, 0.1416f, 0.3516f));

                        drawDigits(200, 280, distance, false, true, true);                //Target Distance
                        float closureV = Vector3.Dot(tgt_velocity, OwnshipToTarget.normalized);
                        drawDigits2(200, 400, closureV);                //Closure speed
                    }

                    Vessel tgt_vessel = tar.GetVessel();

                    // Orbit does not make any sense for a landed vessel, especially when
                    // it is not loaded. The usable values are geographical lat/lon/alt,
                    // so compute and display info for great-circle paths along the surface.
                    if (full && tgt_vessel != null && tgt_vessel.LandedOrSplashed)
                    {
                        drawDigits(200, 370, tgt_vessel.altitude, false, true, true);    //Target altitude

                        if (tgt_vessel.mainBody == FlightGlobals.ActiveVessel.mainBody)
                        {
                            // Great-circle course to the target
                            Vector2d dist = SurfaceBearing(FlightGlobals.ActiveVessel, tgt_vessel);
                            double bearing = (dist.y + 360.0) % 360.0;
                            drawDigits(200, 310, bearing, false, false, true);     // Surface bearing
                            drawDigits(200, 340, dist.x, false, true, true);       // Surface distance

                            // Horizontal component of surface speed and prograde heading
                            Quaternion surf_rot = SurfaceRotationAt(FlightGlobals.ActiveVessel);
                            Vector3 srf_vel = Quaternion.Inverse(surf_rot) * v.GetSrfVelocity();
                            //Vector3 srf_vel = Quaternion.Inverse(surf_rot) * tgt_vessel.GetSrfVelocity();
                            //double hspeed = Vector3.Exclude(Vector3.up, srf_vel).magnitude;   //obsolete
                            double hspeed = Vector3.ProjectOnPlane(Vector3.up, srf_vel).magnitude;

                            // Surface distance to great circle defined by current prograde heading
                            if (hspeed > 0.05)
                            {
                                double vheading = SteamShip.AngleAroundNormal(Vector3.forward, srf_vel, Vector3.up);
                                Vector2d appr = SurfaceApproach(tgt_vessel.mainBody, vheading, bearing, dist.x);
                                double rinc = (bearing - vheading + 540.0) % 360.0 - 180.0;
                                appr.x *= Math.Sign(rinc);
                                drawDigits(200, 430, appr.x, true, true, true);        //Closest Approach
                                drawTime(160, 485, appr.y / hspeed);                     //TT Closest Approach
                                drawDigits(200, 520, -rinc, true, false, true);        //Relative inclination
                            }
                        }
                    }
                    else if (full && orbit != null)
                    {
                        drawDigits(200, 310, orbit.ApA, false, true, true);         //Target Apoapsis
                        drawDigits(200, 340, orbit.PeA, false, true, true);         //Target Periapsis
                        drawDigits(200, 370, orbit.altitude, false, true, true);    //Target altitude
                        drawDigits(200, 430, SteamShip.ClosestApproach, false, true, true);       //Closest Approach
                        drawTime(160, 485, SteamShip.ClosestTime);                  //TT Closest Approach
                        int rinc = (int)(orbit.inclination - v.GetOrbit().inclination);
                        drawDigits(200, 520, rinc, true, false, false);             //Relative inclination
                    }

                    //Draw target / inverse target marker
                    DrawVector(
                        OwnshipToTarget, Resources.HUD_extras,
                        new Rect(168f, 100f, 52f, 52f),  //Target box
                        new Rect(108f, 100f, 52f, 52f)   //Opposite target box
                    );
                }
            }

            //Draw SAS heading lock orientation when mouse control is engaged
           if (v.ActionGroups[KSPActionGroup.SAS] && mouseSAS != null)
                DrawSASHeading(v);

            //Which velocity vector are we interested in?
            Vector3 speed = Vector3.zero;
            switch (FlightGlobals.speedDisplayMode)
            {
                case FlightGlobals.SpeedDisplayModes.Orbit:
                    speed = FlightGlobals.ship_obtVelocity;
                    break;

                case FlightGlobals.SpeedDisplayModes.Target:
                    speed = tgt_velocity;
                    break;

                default:
                    //speed = FlightGlobals.ship_srfVelocity;   //ship_srfVelocity broken in 0.23
                    speed = v.GetSrfVelocity();
                    break;
            }

            //Actually draw the velocity vector
            if (speed.magnitude >= 0.5f && !(SteamShip.InOrbit && orbitalMode))
            {
                DrawVector(
                    speed, Resources.HUD_extras,
                    new Rect(94f, 47f, 53f, 53f),
                    new Rect(159f, 56f, 32f, 32f)
                );
            }

            //Maneuver Node Info
            if (mouseInputActive || drawNodeInfo)
            {
                ManeuverNode myNode = null;
                if (v.patchedConicSolver != null)
                {
                    if (v.patchedConicSolver.maneuverNodes != null)
                    {
                        if (v.patchedConicSolver.maneuverNodes.Count > 0)
                        {
                            myNode = v.patchedConicSolver.maneuverNodes.ToArray()[0];
                            double deltaV = myNode.DeltaV.magnitude;                                       //The burn's ΔV
                            double deltaVRem = myNode.GetBurnVector(FlightGlobals.ActiveVessel.orbit).magnitude;   //Remaining ΔV in the burn
                            NavBallBurnVector bv = UnityEngine.Object.FindObjectsOfType<NavBallBurnVector>()[0];
                            //Thanks to a.g. for finding this vector in MechJeb's code
                            Vector3 fwd = v.patchedConicSolver.maneuverNodes[0].GetBurnVector(v.orbit);
                            //Draw the forward (or anti) burn vector
                            DrawVector(fwd, Resources.HUD_extras, new Rect(198f, 49f, 30f, 47f), new Rect(239f, 50f, 30f, 47f));

                            if (full)
                            {
                                GUI.DrawTextureWithTexCoords(new Rect(0f, 566f * Scale, 112f * Scale, 160f * Scale), Resources.HUD_extras, new Rect(0f, 0.056f, 0.1094f, 0.2083f));

                                double bt = bv.estimatedBurnTime;                             //Start at 0 time until the game crunches a value.
                                if (double.IsInfinity(bt) || double.IsNaN(bt)) bt = 0; //Assume its good if not infinity or NaN
                                if (NodeGauge.useCalculatedBurn) bt = SteamShip.BurnTime;   //Use the same setting from the node gauge
                                //Draw values
                                drawDigits(200, 570, deltaV, false, false, false);
                                drawDigits(200, 605, deltaVRem, false, false, false);
                                drawTime(160, 660, bt);        //Draw KSP's burn time
                                //If we are past the node time, time until burn is 0
                                if (Planetarium.GetUniversalTime() < myNode.UT)
                                    drawTime(160, 716, (myNode.UT - Planetarium.GetUniversalTime()) - (bt / 2));    //draw actual time to KSP's burn start
                                else
                                    drawTime(160, 716, 0);    //Use 0 as time to burn if past node
                            }
                        }
                    }
                }
            }

            //Warning Info
            if (full && drawWarnings)
            {
                if (SteamShip.ElecMax >0 && SteamShip.ChargePercent < 0.1)
                {
                    GUI.DrawTextureWithTexCoords(new Rect(860f * Scale, 170f * Scale, 154f * Scale, 17f * Scale), Resources.HUD_extras, new Rect(0.7041f, 0.5924f, 0.1504f, 0.0221f));
                }
                //Fuel Warning
                if (SteamShip.FuelMax > 0 && SteamShip.FuelPercent < 0.1)
                {
                    GUI.DrawTextureWithTexCoords(new Rect(860f * Scale, 190f * Scale, 131f * Scale, 17f * Scale), Resources.HUD_extras, new Rect(0.7041f, 0.5664f, 0.1279f, 0.0221f));
                }
                //Monopropellent Warning
                if (SteamShip.MonoMax > 0 && SteamShip.MonoPercent < 0.1)
                {
                    GUI.DrawTextureWithTexCoords(new Rect(860f * Scale, 210f * Scale, 131f * Scale, 17f * Scale), Resources.HUD_extras, new Rect(0.7041f, 0.5391f, 0.1279f, 0.0221f));
                }

                //Air intake/overheat info
                double airReq = SteamShip.AirReq;
                double airIn = SteamShip.AirAvail;
                if ((airReq > 0))// && v.mainBody.atmosphere && (v.altitude < v.mainBody.maxAtmosphereAltitude))
                {
                    if (airIn < (1.1 * airReq))
                        GUI.DrawTextureWithTexCoords(new Rect(860f * Scale, 230f * Scale, 131f * Scale, 17f * Scale), Resources.HUD_extras, new Rect(0.7041f, 0.5130f, 0.1279f, 0.0221f));
                }
                //Engine Overtemp warning
                if (SteamShip.MaxPartTemp > 0.8)
                    GUI.DrawTextureWithTexCoords(new Rect(860 * Scale, 250 * Scale, 131 * Scale, 17 * Scale), Resources.HUD_extras, new Rect(0.7041f, 0.4844f, 0.1279f, 0.0221f));
            }
            //Display "Orbital" for orbital mode - upper right
            if (SteamShip.InOrbit && orbitalMode)
            {
                GUI.DrawTextureWithTexCoords(new Rect(920f*Scale, 10f*Scale, 98f*Scale, 24f*Scale), Resources.HUD_extras, new Rect(0.8984f, 0.9558f, 0.0957f, 0.0313f));
            }

        }

        private Vector2d SurfaceBearing(Vessel vfrom, Vessel vto)
        {
            // http://www.movable-type.co.uk/scripts/latlong.html
            var R = vto.mainBody.Radius;
            var dLat = (vto.latitude - vfrom.latitude) * Math.PI / 180.0;
            var dLon = (vto.longitude - vfrom.longitude) * Math.PI / 180.0;
            var lat1 = vfrom.latitude * Math.PI / 180.0;
            var lat2 = vto.latitude * Math.PI / 180.0;

            // Distance
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;

            // Bearing
            var y = Math.Sin(dLon) * Math.Cos(lat2);
            var x = Math.Cos(lat1) * Math.Sin(lat2) -
                    Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
            var brng = Math.Atan2(y, x) * 180.0 / Math.PI;

            return new Vector2d(d, brng);
        }

        private Vector2d SurfaceApproach(CelestialBody body, double heading, double bearing, double dist)
        {
            // http://www.movable-type.co.uk/scripts/latlong.html
            var R = body.Radius;
            var brng12 = heading * Math.PI / 180.0;
            var brng13 = bearing * Math.PI / 180.0;

            // Closest approach
            var dXt = Math.Asin(Math.Sin(dist / R) * Math.Sin(brng13 - brng12)) * R;
            // Distance to closest approach
            var dAt = Math.Acos(Math.Cos(dist / R) / Math.Cos(dXt / R)) * R;

            return new Vector2d(dXt, dAt);
        }

        private void DrawSASHeading(Vessel vessel)
        {
            
            //var SAS = vessel.vesselSAS;
            var SAS = vessel.Autopilot.SAS;
            if (SAS == null)
                return;

            var vrot = vessel.ReferenceTransform.rotation;
            var euler = (Quaternion.Inverse(vrot) * SAS.lockedHeading).eulerAngles;
            var turn = new Vector3(-euler.z, -euler.x, -euler.y);

            // The code expects the mark to be symmetric, so a negative bias has to be used
            DrawAngleVector(turn, Resources.HUD_extras, new Rect(504, -17, 96, 56), new Rect(), 0.7f);
        }

        //Starts drawing a relative vecotor on the HUD
        private void DrawVector(Vector3 dir, Texture2D atlas, Rect tvv, Rect ivv)
        {
            Transform self = FlightGlobals.ActiveVessel.ReferenceTransform;
            //Determine the number of degrees the FPV differs from vessel orientation
            Vector3 rVel = new Vector3();
            rVel.x = SteamShip.AngleAroundNormal(dir, self.up, self.forward);
            rVel.y = SteamShip.AngleAroundNormal(dir, self.up, self.right);
            DrawAngleVector(rVel, atlas, tvv, ivv);
        }

        //Actually draws the requested vector onto the HUD
        private void DrawAngleVector(Vector3 rVel, Texture2D atlas, Rect tvv, Rect ivv, float resize = 1.0f)
        {
            //Convert to +/- 180 instead of 0-365
            rVel.x = (rVel.x + 540f) % 360f - 180f;
            rVel.y = (rVel.y + 540f) % 360f - 180f;

            // Detect retrograde mode and flip angles
            Rect xvv = tvv;
            if (Math.Abs(rVel.x) > 90f || Math.Abs(rVel.y) > 90f)
            {
                xvv = ivv;
                rVel.x = (rVel.x + 360f) % 360f - 180f;
                rVel.y = (rVel.y + 360f) % 360f - 180f;
            }

            if (xvv.width <= 0f || xvv.height <= 0f)
                return;

            // Clamp to the edges of the view
            float factor = 1.0f;
            if (Mathf.Abs(rVel.x) > 14)
                factor = Mathf.Min(factor, 14f / Mathf.Abs(rVel.x));
            if (Mathf.Abs(rVel.y) > 30)
                factor = Mathf.Min(factor, 30f / Mathf.Abs(rVel.y));
            rVel *= factor;

            factor = Mathf.Max(0.5f, factor) * resize;
            float VelX = rVel.x * 10.8f;      //10.8 pix/degree, so multiply by 10.8
            float VelY = rVel.y * 10.8f;      //These are the offsets, so modify to absolute position, times scale
            float PosX = (512 - xvv.width * factor * 0.5f + VelX);      //Center is 512, 384
            float PosY = (384 - xvv.height * factor * 0.5f - VelY);

            // Roll by the z value
            if (rVel.z != 0.0f)
            {
                Matrix4x4 cur = GUI.matrix;
                Vector2 pivotPoint = new Vector2((512 + VelX) * Scale, (384 - VelY) * Scale);
                GUIUtility.RotateAroundPivot(rVel.z, pivotPoint);
                DrawTile(PosX, PosY, atlas, xvv, factor);
                GUI.matrix = cur;
            }
            else
                DrawTile(PosX, PosY, atlas, xvv, factor);
        }

        private void DrawTile(float x, float y, Texture2D atlas, Rect xvv, float resize = 1.0f)
        {
            Rect xvvBox = new Rect(
                xvv.x / atlas.width, (atlas.height - xvv.y - xvv.height) / atlas.height,
                xvv.width / atlas.width, xvv.height / atlas.height
            );
            Rect xvvPos = new Rect(x * Scale, y * Scale, xvv.width * Scale * resize, xvv.height * Scale * resize);
            GUI.DrawTextureWithTexCoords(xvvPos, atlas, xvvBox);
        }

        //Draws the pitch ladder or vertical ascent circle, and roll scale
        private void drawPitchLadder(Vessel v, bool full)
        {
            //I'm pretty sure the navball stuff came from MechJeb too.
            if (ball == null)
            {
                ball = UnityEngine.Object.FindObjectOfType<NavBall>();
            }
            Quaternion vesselRot = Quaternion.Inverse(ball.relativeGymbal);
            float pitch = (vesselRot.eulerAngles.x > 180) ? (360 - vesselRot.eulerAngles.x) : -vesselRot.eulerAngles.x; 
            float roll = (vesselRot.eulerAngles.z > 180) ? (360 - vesselRot.eulerAngles.z) : -vesselRot.eulerAngles.z;
            float yaw = vesselRot.eulerAngles.y;    //Heading

            //Display relative pitch in orbital mode
            if (SteamShip.InOrbit && orbitalMode)
            {
                pitch = -1*SteamShip.AngleAroundNormal(v.GetObtVelocity(), v.ReferenceTransform.up, v.ReferenceTransform.right);
                if (pitch > 90)
                {
                    pitch -= 90f;
                }
                if (pitch < -90)
                {
                    pitch += 90f;
                }
                Transform pro = ball.progradeVector;
                //Debug.Log("Pos: " + pro.position.x + ", " + pro.position.y + ", " + pro.position.z);
            }

            //Roll scale
            if (Math.Abs(pitch) < 70 && (full || planeMode != mouseInputFlip))    //Don't draw in partial rocket or vertical modes
            {
                GUI.DrawTextureWithTexCoords(new Rect(456 * Scale, 48 * Scale, 112 * Scale, 36 * Scale), Resources.HUD_extras, new Rect(0.2588f, 0.8177f, 0.1094f, 0.0469f)); //Draw the small arc all the time
                if (Math.Abs(roll) > 35)  //Draw full arc only for moderate bank angles
                {
                    GUI.DrawTextureWithTexCoords(new Rect(422 * Scale, 77 * Scale, 181 * Scale, 78 * Scale), Resources.HUD_extras, new Rect(0.2256f, 0.7031f, 0.1786f, 0.1016f));
                }
                if (Math.Abs(roll) > 85)  //Draw bottom half only for extreme angles
                {
                    GUI.DrawTextureWithTexCoords(new Rect(417 * Scale, 149 * Scale, 193 * Scale, 106 * Scale), Resources.HUD_extras, new Rect(0.2207f, 0.5547f, 0.1885f, 0.1380f));
                }
                //Draw roll pointer
                Vector2 pivotPoint = new Vector2(512 * Scale, 154 * Scale);    //Center of the arc
                GUIUtility.RotateAroundPivot((float)(roll), pivotPoint);
                //Draw the arrow
                DrawTile(505, 66, Resources.HUD_extras, new Rect(272, 83, 17, 16));
                //DrawTile(505, 66, Resources.HUD_roll_ptr, new Rect(505, 66, 17, 13));
                GUI.matrix = Matrix4x4.identity;    //Reset rotation matrix
                // Second roll arrow for near-zero bank angles
                if (Math.Abs(roll) <= 3)
                {
                    GUIUtility.RotateAroundPivot((float)(roll * 10), pivotPoint);
                    DrawTile(505, 66, Resources.HUD_extras, new Rect(272, 83, 17, 16), 0.6f);
                    //DrawTile(508, 70, Resources.HUD_roll_ptr, new Rect(505, 66, 17, 13), 0.6f);
                    GUI.matrix = Matrix4x4.identity;
                }
            }
            //Draw pitch ladder, unless in vertical ascent mode
            if (Event.current.type == EventType.repaint)
            {
                float xsize = 150, ysize = 325;
                Rect area = new Rect(362 * Scale, 59 * Scale, xsize * 2 * Scale, ysize * 2 * Scale);

                // Since it seems impossible to properly clip a rotated texture rect,
                // use a hack: draw the ladder as a straight rect, but transform the
                // texture coordinates.
                Matrix4x4 mat;
                //float polar_pitch = SteamShip.PolarPitch;
                //float polar_roll = SteamShip.PolarRoll;
                //float polar_yaw = SteamShip.PolarYaw;
                if (pitch > 70f && !(orbitalMode && SteamShip.InOrbit))    //Vertical Ascent
                {
                    // Work around yaw/roll gimbal lock at the pole by using a different transform.
                    Quaternion polarRot = Quaternion.Euler(90, 0, 0) * vesselRot;
                    float polar_yaw = (polarRot.eulerAngles.y + 180f) % 360f - 180f;
                    //print("Polar Yaw: " + Math.Round(polar_yaw));
                    float polar_pitch = (polarRot.eulerAngles.x + 180f) % 360f - 180f;
                    //print("Polar Pitch: " + Math.Round(polar_pitch));
                    float polar_roll = polarRot.eulerAngles.z;
                    
                    mat = Matrix4x4.Scale(
                        new Vector3(1 / 900f, 1 / 900f, 1.0f)
                    ) * Matrix4x4.TRS(
                        // 70 is 647, 90 is 447, so 20 deg/200 pix = 10 pix / deg, or 0.0111% per deg
                        // The spherical distortion error with angles < 20deg is less than 1 percent.
                        // I have no idea why the angles have these signs, maybe I mix up clockwise vs ccw.
                        new Vector3(450 + polar_yaw * 10, 450 - polar_pitch * 10, 0),
                        // The rotatePitch option is meaningless when pointing straight up.
                        Quaternion.Euler(0.0f, 0.0f, polar_roll),
                        // Correct scaling to match the pixels per degree expected by markers
                        Vector3.one * (10 / 10.8f)
                    );
                    Resources.HUD_vert_mat.SetPass(0);
                }
                else if (pitch < -70f && !(orbitalMode && SteamShip.InOrbit))  //Vertical Descent
                {
                    // Work around yaw/roll gimbal lock at the pole by using a different transform.
                    Quaternion polarRot = Quaternion.Euler(-90, 0, 0) * vesselRot;  //Bug 3 patch
                    float polar_yaw = (polarRot.eulerAngles.y + 180f) % 360f - 180f;
                    //print("Polar Yaw: " + Math.Round(polar_yaw));
                    float polar_pitch = (polarRot.eulerAngles.x + 180f) % 360f - 180f;
                    //print("Polar Pitch: " + Math.Round(polar_pitch));
                    float polar_roll = polarRot.eulerAngles.z;

                    mat = Matrix4x4.Scale(
                        new Vector3(1 / 900f, 1 / 900f, 1.0f)
                    ) * Matrix4x4.TRS(
                        // 70 is 647, 90 is 447, so 20 deg/200 pix = 10 pix / deg, or 0.0111% per deg
                        // The spherical distortion error with angles < 20deg is less than 1 percent.
                        // I have no idea why the angles have these signs, maybe I mix up clockwise vs ccw.
                        new Vector3(450 + polar_yaw * 10, 450 - polar_pitch * 10, 0),
                        // The rotatePitch option is meaningless when pointing straight up.
                        Quaternion.Euler(0.0f, 0.0f, polar_roll),
                        // Correct scaling to match the pixels per degree expected by markers
                        Vector3.one * (10 / 10.8f)
                    );
                    Resources.HUD_vertd_mat.SetPass(0);
                }
                else //Regular ladder
                {
                    mat = Matrix4x4.Scale(
                        // Scale to normalize
                        new Vector3(1 / 300f, 1 / 2600f, 1.0f)
                    ) * Matrix4x4.TRS(
                        //Each degree is 10.83 pixels
                        new Vector3(150f, 1300f - (float)(pitch * -10.833333), 0.0f),
                        //Rotate for pitch ladder, if selected
                        Quaternion.Euler(0.0f, 0.0f, (rotatePitch || !full) ? -roll : 0.0f),
                        Vector3.one
                    );

                    Resources.HUD_ladder_mat.SetPass(0);
                }

                // Direct drawing calls to get full control over texture coords
                GL.Begin(GL.QUADS);
                GL.Color(GUI.color * 0.5f); // the shader appears to multiply by 2 for some reason
                GL.TexCoord(mat.MultiplyPoint3x4(new Vector3(-xsize, ysize, 0)));
                GL.Vertex3(area.xMin, area.yMin, 0.1f);
                GL.TexCoord(mat.MultiplyPoint3x4(new Vector3(xsize, ysize, 0)));
                GL.Vertex3(area.xMax, area.yMin, 0.1f);
                GL.TexCoord(mat.MultiplyPoint3x4(new Vector3(xsize, -ysize, 0)));
                GL.Vertex3(area.xMax, area.yMax, 0.1f);
                GL.TexCoord(mat.MultiplyPoint3x4(new Vector3(-xsize, -ysize, 0)));
                GL.Vertex3(area.xMin, area.yMax, 0.1f);
                GL.End();
            }
        }
        private Quaternion SurfaceRotationAt(Vessel vessel)
        {
            //Taken from MechJeb via KSP forums
            Vector3d CoM, up;
            CoM = vessel.findWorldCenterOfMass();
            up = (CoM - vessel.mainBody.position).normalized;
            //Vector3d north = Vector3.Exclude(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized; //obsolete
            Vector3d north = Vector3.ProjectOnPlane(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized;
            return Quaternion.LookRotation(north, up);
        }

        //Draws the heading tape, current heading, and heading bug to the HUD
        private void drawHeadingTape(Vessel v)
        {
            if (SteamShip.InOrbit && orbitalMode) //In orbital mode, draw heading in terms of FPV, or yaw left and right of orbital velocity
            {
                //Transform self = v.ReferenceTransform;
                float relHeading = SteamShip.AngleAroundNormal(FlightGlobals.ActiveVessel.GetObtVelocity(), v.ReferenceTransform.up, v.ReferenceTransform.forward);
                //float relOther = SteamShip.AngleAroundNormal(FlightGlobals.ActiveVessel.GetObtVelocity(), self.up, self.right);
                relHeading = (relHeading + 540f) % 360f - 180f; //This converts from 0-365 to +/- 180, except I think it already is
                GUI.DrawTextureWithTexCoords(new Rect(261 * Scale, 718 * Scale, 500 * Scale, 50 * Scale), Resources.HUD_compass, new Rect(0.42855f+(relHeading * -0.00238f), 0f, 0.1429f, 0.4545f));
            }
            else
            {
                //MechJeb code for vessel heading
                /*Vector3d CoM, MoI, up;
                Quaternion rotationSurface, rotationVesselSurface;
                Vessel vessel = FlightGlobals.ActiveVessel;
                CoM = vessel.findWorldCenterOfMass();
                MoI = vessel.findLocalMOI(CoM);
                up = (CoM - vessel.mainBody.position).normalized;
                Vector3d north = Vector3.Exclude(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized;
                rotationSurface = Quaternion.LookRotation(north, up);
                rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.transform.rotation) * rotationSurface);
                double vesselHeading = rotationVesselSurface.eulerAngles.y;*/
                double vesselHeading = SteamShip.Heading;
                float offset = 0;
                vesselHeading *= 0.00238;          //Conver into percentage left
                offset += (float)vesselHeading;    //Move left that percentage
                //Draw just the part we need
                GUI.DrawTextureWithTexCoords(new Rect(261 * Scale, 718 * Scale, 500 * Scale, 50 * Scale), Resources.HUD_compass, new Rect(offset, 0.5454f, 0.1429f, 0.4546f));
            }
        }

        //Draws the airspeed tape, current airspeed, and ground speed to the HUD
        private void drawAirspeedTape(Vessel v)
        {
           //double mach, tas, easCoeff;
           //AirGauge.getAirspeedInfo(v, out mach, out tas, out easCoeff);
           double mach = SteamShip.Mach;
           double tas = SteamShip.TAS;
           double easCoeff= SteamShip.EASCoef;
           float terminal = AirGauge.getTerm(v);
           
           //Auto switching between surface and orbital velocities
           double vel = 0;
           if (SteamShip.InOrbit)
               vel = v.obt_velocity.magnitude; //Orbital speeds
           else
           {
               vel = tas;
               if (useEAS)
               {
                   vel *= easCoeff;
                   terminal *= (float)easCoeff;
               }
               GUI.DrawTextureWithTexCoords(new Rect(300*Scale,615*Scale,38*Scale,21*Scale), Resources.HUD_extras, new Rect(0.3809f, 0.8971f, 0.0371f, 0.0273f));  //GS static text
               drawDigits(295, 610, v.horizontalSrfSpeed, false, false, true);  //Ground speed doesn't matter IN SPAAAAACE!
           }
           //Draw Mach number, if relevant
           if (v.mainBody.atmosphere && v.altitude < v.mainBody.atmosphereDepth)
           {
               if (mach > _minMach)
               {
                   drawDigits2x2(310, 640, mach, true);
                   GUI.DrawTextureWithTexCoords(new Rect(315 * Scale, 640 * Scale, 20 * Scale, 30 * Scale), Resources.HUD_chars, new Rect(0f, 0.4f, 1f, 0.2f)); //Draw the 'M'
               }
           }
            float offset = 0;
            if (vel < 200)
            {
                if (terminal < 200) //display Vt down to bottom of the ladder
                {
                    float vert = (float) (terminal-vel);
                    vert = 275f + (vert*13.7f);  //distance from top
                    GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                }
                else if (terminal < 300)    //Scale change
                {
                    float vert = (float)(terminal - vel);
                    if (Math.Abs(vert) < 100)
                    {
                        vert = 275f + (vert * 2.74f);
                        GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                    }
                }
                //13.7 pix/m/s, start at 0, displaying 0.16722%
                offset = (float) ((200-vel) * 13.7);      //Start this many pixels from the top
                offset = offset / 3289;             //Convert to a percentage of the texture
                GUI.DrawTextureWithTexCoords(new Rect(182*Scale, 59*Scale, 150*Scale, 550*Scale), Resources.HUD_speed_tape1, new Rect(0f, offset, 1f, 0.167f));   //0.16722
            }
            else if (vel < 1000)
            {
                //Draw previous scale terminal velocity marker
                if (terminal < 200)
                {
                    float vert = (float)(terminal - vel);
                    if (Math.Abs(vert) < 20)
                    {
                        vert = 275f + (vert * 13.7f);
                        GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                    }
                }   //Draw current scale terminal velocity marker
                else if (terminal < 1000)
                {
                    float vert = (float)(terminal - vel);
                    if (Math.Abs(vert) < 100)
                    {
                        vert = 275f + (vert * 2.74f);
                        GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                    }
                }   //Draw next scale terminal velocity marker
                else if (terminal < 1200)
                {
                    float vert = (float)(terminal - vel);
                    if (Math.Abs(vert)<200)
                    {
                        vert = 275f + (vert * 1.37f);
                        GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                    }
                }
                //2.74 pix/m/s, start at 200, displaying 0.2%
                offset = (float)(1000 - vel)/1000;
                GUI.DrawTextureWithTexCoords(new Rect(182*Scale, 59*Scale, 150*Scale, 550*Scale), Resources.HUD_speed_tape2, new Rect(0f, offset, 1f, 0.2f));
            }
            else if (vel < 3000)
            {
                float vert = (float)(terminal - vel);
                //Draw terminal velocity marker in previous scale
                if (terminal < 1000)
                {
                   if (Math.Abs(vert) < 100)
                   {
                       vert = 275f + (vert * 2.74f);
                       GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                   }
                }   //Draw terminal velocity marker in current scale
                else if (terminal < 3000)
                {
                    if (Math.Abs(vert) < 200)
                    {
                        vert = 275f + (vert * 1.37f);
                        GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                    }
                }   //Draw terminal velocity marker in next scale
                else if (terminal < 4000)
                {
                    if (Math.Abs(vert) < 1000)
                    {
                        vert = 275f + (vert * 0.274f);
                        GUI.DrawTextureWithTexCoords(new Rect(257f * Scale, vert * Scale, 43f * Scale, 19f * Scale), Resources.HUD_extras, new Rect(0.6279f, 0.8646f, 0.0420f, 0.0247f));
                    }
                }
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
                Color gc = GUI.color;
                Color inverse = new Color((float)cRed / 255f, (float)cGreen / 255f, (float)cBlue / 255f, gc.a);
                if (color_center)
                    GUI.color = inverse;
                drawDigits(460, 335, vel, false, false, true);                            //TAS or EAS
                GUI.color = gc;
            }
            
        }

        //Draws the altitude tape, current altitude, VVI, and radar altitude to the HUD
        private void drawAltitudeTape(Vessel v)
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
                Color gc = GUI.color;
                Color inverse = new Color((float)cRed / 255f, (float)cGreen / 255f, (float)cBlue / 255f, gc.a);
                if (color_center)
                    GUI.color = inverse;
                drawDigits(615, 335, alt, false, true, true);   //MSL
                drawDigits(615, 355, v.verticalSpeed, true, false, false);                 //VVI
                GUI.color = gc;
            }
            //Draw the VVI gauge
            //digital readout
            drawDigits(940, 520, v.verticalSpeed, true, false, false);
            //static vvi box
            GUI.DrawTextureWithTexCoords(new Rect(890 * Scale, 250 * Scale, 38 * Scale, 264 * Scale), Resources.HUD_extras, new Rect(0.7061f, 0.1120f, 0.0371f, 0.34375f));
            //dynamic vvi box
            bool pos = true;
            int vs = (int)(v.verticalSpeed+0.5);
            if (v.verticalSpeed < 0)
            {
                vs *= -1;
                pos = false;
            }
            int height;
            if (vs <= 100)
            {
                //2 m/s per pixel
                height = vs / 2;
            }
            else if (vs <= 300)
            {
                //4 m/s per pixel
                height = vs - 100;
                height = height / 4;
                height += 50;
            }
            else if (vs <= 600)
            {
                //12 m/s per pixel
                height = vs - 300;
                height = height / 12;
                height += 100;
            }
            else //peg at top
            {
                height = 128;
            }
            if (pos)
            {
                if (height < 14)
                {
                    //Debug.Log("VS: "+vs+" H: "+height+" ("+(893*Scale)+", "+(368-height)*Scale+", "+(22*Scale)+", "+(height*Scale)+") (0.7422, "+(234f-height)/768f+", 0.0195, "+(height/768f)+")");
                    GUI.DrawTextureWithTexCoords(new Rect(893 * Scale, (381 - height) * Scale, 22 * Scale, height * Scale), Resources.HUD_extras, new Rect(0.7422f, (248f - height) / 768f, 0.0195f, 0.0182f));  //triangle growing from 0
                }
                else
                {
                    height -= 14;
                    GUI.DrawTextureWithTexCoords(new Rect(893 * Scale, (368 - height) * Scale, 22 * Scale, 14 * Scale), Resources.HUD_extras, new Rect(0.7422f, 0.3047f, 0.0195f, 0.0182f));  //triangle
                    GUI.DrawTextureWithTexCoords(new Rect(890 * Scale, (381 - height) * Scale, 28 * Scale, height * Scale), Resources.HUD_extras, new Rect(0.7451f, 0.2591f, 0.0156f, 0.0208f));    //box
                }
            }
            else
            {
                if (height < 14)
                    GUI.DrawTextureWithTexCoords(new Rect(893 * Scale, 381 * Scale, 22 * Scale, height * Scale), Resources.HUD_extras, new Rect(0.7441f, 0.2344f, 0.0195f, (height/768f)));  //triangle growing from -0
                else
                {
                    height -= 14;
                    GUI.DrawTextureWithTexCoords(new Rect(890 * Scale, 382 * Scale, 28 * Scale, height * Scale), Resources.HUD_extras, new Rect(0.7451f, 0.2591f, 0.0156f, 0.0208f));   //box
                    GUI.DrawTextureWithTexCoords(new Rect(893 * Scale, (381 + height) * Scale, 22 * Scale, 14 * Scale), Resources.HUD_extras, new Rect(0.7441f, 0.2344f, 0.0195f, 0.0182f));  //triangle
                }
            }

            //Draw radar altimeter, unless in orbit
            if (!SteamShip.InOrbit)
            {
                GUI.DrawTextureWithTexCoords(new Rect(838*Scale,615*Scale,40*Scale,21*Scale), Resources.HUD_extras, new Rect(0.4355f, 0.8958f, 0.0391f, 0.0273f));  //Static RA
                drawDigits(830, 610, SteamShip.RA, false, true, true);
            }
        }

        //Checks the vessel to see if ground impact is immenant, and displays warnings if necessary
        private void GPWS(Vessel v)
        {
            //Don't do anything if GPWS is off.
            if (!useGPWS) return;
            bool warning = false;
            long ra = SteamShip.RA;
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
                            if (pm is ModuleWheelDeployment)
                            {
                                ModuleWheelDeployment mlg = (ModuleWheelDeployment)pm;
                                if (!mlg.stateString.Equals("Deployed"))
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
                //Disable for vertical ascent mode
                if (!warning && (ra < 1000) && (ra > 5))
                {
                    if (ball == null)
                        ball = UnityEngine.Object.FindObjectOfType<NavBall>();
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

        //Returns true if any engines are flamed out in the vessel
        private bool flameOutOld(Vessel v)
        {
            foreach (Part P in v.parts)
            {
                foreach (PartModule PM in P.Modules)
                {
                    if (PM is ModuleEngines)
                    {
                        ModuleEngines ME = PM as ModuleEngines;
                        if (ME.flameout) return true;
                    }
                }
            }
            return false;
        }

        public double getMaxTempOld(Vessel v)
        {
            double hotesttemp = 0.0;
            foreach (Part P in v.parts)
            {
                foreach (PartModule PM in P.Modules)
                {
                    if (PM is ModuleEngines)
                    {
                        ModuleEngines ME = PM as ModuleEngines;
                        if (!ME.flameout)
                        {
                            double temp = P.temperature / P.maxTemp;
                            //print("Temp: " + Math.Round(P.temperature, 2) + "/" + P.maxTemp + " = " + Math.Round(temp * 100, 2) + "%");
                            if (temp > hotesttemp) hotesttemp = temp;
                        }
                    }
                }
            }
            return hotesttemp;
        }

        private struct res { public double dist; public double time; }

        private res getClosestApproachOld()
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

        //Draws a formatted 5 digit number
        //If sign is true, draws - for negative numbers.  If false, draws 0.
        //If magnitude is true, draws m, K, or M postfix as appropriate
        private void drawDigits(float right, float top, double value, bool Sign, bool Magnitude, bool RollOnes)
        {
            float char_width = Resources.HUD_chars.width;   //was orbit_chars
            float char_height = Resources.HUD_chars.height / 5f;
            float output = (float)Math.Abs(value);
            bool negative = false;
            if ((value < 0) && Sign) negative = true;
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
            int ioutput = Mathf.RoundToInt(output);
            //Now draw each digit of our output, starting from the right - a.g. patch - Bug 1
            for (int i = 5, divisor = 10000; i > 0; i--, divisor /= 10)
            {
                int x = (int)(ioutput / divisor) % 10;
                float y = x * 0.091f;
                if ((i == 1)&&RollOnes) { //Smooth rolling for last digit
                    y += (output - ioutput) * 0.091f;
                    if (y < 0.0f) y += 0.91f;
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

        //Draws a formatted 2 digit number with 2 decimal places, optionally rolling 100ths digit
        private void drawDigits2x2(float right, float top, double value, bool rolling = false)
        {
            float width = Resources.HUD_digits.width;
            float height = Resources.HUD_digits.height / 11;
            float val = 0;
            if (value > 99.99) val = 99.99f; else if (val < 0) val = 0f; else val = (float)value;
            //100ths
            float exact = (int)(val * 100);
            exact /= 100;
            //print("Val: "+val+" Exact: "+exact+" Diff: " +(val-exact));
            int dec = (int)(val * 100) % 10;
            float y = dec * 0.091f;
            if (rolling)
            {
                y += (val - exact) *9.1f;
                if (y < 0) y += 0.91f;
            }
            //print("100ths: "+dec+" y: "+y);
            GUI.DrawTextureWithTexCoords(new Rect((right - width) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
            //10ths
            dec = (int)(val * 10) % 10;
            y = ((float)dec * 0.091f);
            //print("10ths: "+dec+" y: "+y);
            GUI.DrawTextureWithTexCoords(new Rect((right - (width*2)) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
            //'.'
            GUI.DrawTextureWithTexCoords(new Rect((right - (width * 2) - 7) * Scale, (top+18) * Scale, 9 * Scale, 9 * Scale), Resources.HUD_extras, new Rect(0.1982f, 0.4362f, 0.0088f, 0.0177f));
            //1s
            dec = (int)val % 10;
            //print("1s: " + dec);
            y = dec * 0.091f;
            GUI.DrawTextureWithTexCoords(new Rect((right - (width * 3)-6) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
            //10s
            dec =(int) (val / 10) % 10;
            //print("10s: " + dec);
            y = dec * 0.091f;
            if (dec != 0)
                GUI.DrawTextureWithTexCoords(new Rect((right - (width * 4)-6) * Scale, top * Scale, width * Scale, height * Scale), Resources.HUD_digits, new Rect(0f, y, 1f, 0.091f));
        }

        //Draws a formatted 3 digit number with 1 decimal place and sign 
        private void drawDigits2(float right, float top, double value)
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
        private void drawTime(float right, float top, double time)
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

        //This loads the configuration values from the config file
        public override void load(PluginConfiguration config)
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
            drawWarnings = config.GetValue<bool>("HUDdrawWarnings", true);
            useGPWS = config.GetValue<bool>("HUDuseGPWS", true);
            ivaOnly = config.GetValue<bool>("HUDivaOnly", false);
            mouseInput = config.GetValue<bool>("HUDmouseInput", false);
            mouseInputOnly = config.GetValue<bool>("HUDmouseInputOnly", false);
            planeMode = config.GetValue<bool>("HUDplaneMode", false);
            orbitalMode = config.GetValue<bool>("HUDorbitalMode", true);
            useEAS = config.GetValue<bool>("HUDuseEAS", false);
            Red = config.GetValue<int>("HUDred", 0);
            Green = config.GetValue<int>("HUDgreen", 255);
            Blue = config.GetValue<int>("HUDblue", 0);
            Alpha = config.GetValue<int>("HUDalpha", 255);
            cRed = config.GetValue<int>("centerRed", 255 - Red);
            cGreen = config.GetValue<int>("centerGreen", 255 - Green);
            cBlue = config.GetValue<int>("centerBlue", 255 - Blue);
            color_center = config.GetValue<bool>("centerColor", false);
            _minMach =(float) config.GetValue<double>("HUDminMach", 0.5);
            _minGs = (float) config.GetValue<double>("HUDminG", 2);
            required_technology = config.GetValue<string>("HUDtechnology", "electronics");
            String s = config.GetValue<string>("HUDkey", "LeftAlt H");
            try
            {
                s = s.Trim();
                String[] keys = s.Split(' ');
                hudKeys = new KeyCode[keys.Length];
                for (int i = 0; i < hudKeys.Length;i++ )
                    hudKeys[i] = (KeyCode)Enum.Parse(typeof(KeyCode), keys[i]);   //Parse the string the the matching key
            }
            catch
            {
                Debug.Log("Couldn't read keys from config.");
                //Use default
                hudKeys = new KeyCode[2];
                hudKeys[0] = KeyCode.LeftAlt;
                hudKeys[1] = KeyCode.H; 
            }
            
        }

        //This saves the configuration values from the config file
        public override void save(PluginConfiguration config)
        {
            config.SetValue("HUDminMach", (double)_minMach);
            config.SetValue("HUDminG", (double)_minGs);
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
            config.SetValue("HUDdrawWarnings", drawWarnings);
            config.SetValue("HUDuseGPWS", useGPWS);
            config.SetValue("HUDivaOnly", ivaOnly);
            config.SetValue("HUDmouseInput", mouseInput);
            config.SetValue("HUDmouseInputOnly", mouseInputOnly);
            config.SetValue("HUDplaneMode", planeMode);
            config.SetValue("HUDorbitalMode", orbitalMode);
            config.SetValue("HUDuseEAS", useEAS);
            config.SetValue("HUDred", Red);
            config.SetValue("HUDgreen", Green);
            config.SetValue("HUDblue", Blue);
            config.SetValue("HUDalpha", Alpha);
            config.SetValue("centerRed", cRed);
            config.SetValue("centerGreen", cGreen);
            config.SetValue("centerBlue", cBlue);
            config.SetValue("centerColor", color_center);
            config.SetValue("HUDtechnology", required_technology);
            String key = "";
            foreach (KeyCode code in hudKeys)
                key = key + code.ToString()+" ";
            config.SetValue("HUDkey", key);
            }
    }
}
