/**************************************************************************
 * 
 * This is the core class for the SteamGauges mod for Kerbal Space Program.
 * It is  licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License
 * and based on the game produced and under copywrite to Squad.
 * 
 * Thanks to everyone on the KSP forums who offered up support, ideas,
 * and bugs to fix.
 * Special thanks to a.g. for all his help with the HUD.
 * Also thanks to several mods and their authors for insights onto how KSP works
 * and how to get at all the bits I was after.  Especially: Kerbal Engineer Redux,
 * Kerbal Alarm Clock, and MechJeb.
 * 
 * Aaron Port (Trueborn) May 2015
 * ************************************************************************/

using UnityEngine;                  //KSP is built on the Unity engine, so, we use that a lot
using KSP.IO;                       //KSP specific IO handling
using KSP.UI.Screens;
using System;


namespace SteamGauges
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]      //Don't show anything unless in flight.
    public class SteamGauges : MonoBehaviour
    {
        //This version of SteamGauges is compatible with KSP 1.2 - for use in CompatabilityChecker
        public static int CompatibleMajorVersion { get { return 1; } }
        public static int CompatibleMinorVersion { get { return 2; } }
        public static int CompatibleRevisionVersion { get { return 0; } }
        public static String VersionString { get { return "1.7.2"; } }

        public static bool debug = false;                                            //If this is true, prints debug info to the console
        private static Rect _windowPosition;                                        //The position for the options window (left, top, width, height)
        public static GUIStyle _windowStyle, _labelStyle, _boldStyle, _buttonStyle, _IconStyle, _toggleStyle;         //Styles for the window and label
        private bool _hasInitStyles = false;                                        //Only initialize once
        private static bool isMinimized = true;                                     //Is the window currently minimized?
        private static bool advMinimized = true;                                    //Advanced settings window
        private bool _allToolbar;                                                   //If true, replaces the main window with a bunch of toolbar buttons
        public static bool windowLock;                                              //Are the windows locked in position, or dragable?
        private static Rect _advwindowPosition;                                     //Advanced settings window
        public static bool drawBezels { get; private set; }                         //Draw the square bezels around gauges?
        public static float Alpha {get; private set;}                               //The global alpha blend value for all windows
        public static Color Red = new Color(1, 0, 0);                               //Red, yo!
        private static string[] buttonNames = { "Orbital Info Off", "Throttle Off", "Indicators Off", "Target Info Off", "Node Info Off", "Rotate Pitch Off", "Central Speed/Alt Off" , "GPWS Off", "Center HUD", "Oribal Mode Off", "Warnings Off", "Use CAS"};
        //Gauge variables
        public bool enableAirGauge, enableElectricalGauge, enableFuelGauge, enableHUDGauge, enableCompass, enableNodeGauge, enableOrbitGauge, enableRadarAltimeter, enableRZGauge, enableNavGauge, enableTempGauge;
        private AirGauge airGauge;                                                  
        private ElectricalGauge electricalGauge;
        private FuelGauge fuelGauge;
        private HudGauge hudGauge;
        private MagneticCompass compassGauge;
        private NodeGauge nodeGauge;
        internal OrbitGauge orbitGauge;
        private RadarAltimeter radarAltimeter;
        private RendezvousGauge rzGauge;
        private NavGauge navGauge;
        private TempGauge tempGauge;
        
        //Global calculation class
        public static SteamShip vShip;

        //Blizzy's toolbar buttons
        private static IButton[] buttons = new IButton[12];
        private static ApplicationLauncherButton[] appButtons = new ApplicationLauncherButton[12];

        private Callback preDrawCallbacks;
        private Callback postDrawCallbacks;
        
        //Runs (once?) on object loading
        public void Awake()
        {
            //Don't display the window in the editor
            if (FlightGlobals.fetch != null)
            {
                //Initialize styles, if not already done
                if (!_hasInitStyles) initStyles();

                if (debug) Debug.Log("(SG) SteamGauges is loading textures...");
                //Loads textures
                Resources.loadAssets();

                //This needs to get drawn from here on out
                AddToPostDrawQueue(OnDraw);

                if (debug) Debug.Log("(SG) Loading config values...");
                PluginConfiguration config = PluginConfiguration.CreateForType<SteamGauges>();
                config.load();
                LoadMe(config);
                if (debug) Debug.Log("(SG) Initializing individual gauges...");
                //Initialize individual gauges
                radarAltimeter = new RadarAltimeter();
                radarAltimeter.Initialize(this, 8903, "rad_alt.png", enableRadarAltimeter);
                compassGauge = new MagneticCompass();
                compassGauge.Initialize(this, 8904, "magnetic_compass.png", enableCompass, 1134, 574);
                electricalGauge = new ElectricalGauge();
                electricalGauge.Initialize(this, 8905, "ammeter_volmeter.png", enableElectricalGauge);
                fuelGauge = new FuelGauge();
                fuelGauge.Initialize(this, 8906, "fuel_gauge.png", enableFuelGauge);
                orbitGauge = new OrbitGauge();
                orbitGauge.Initialize(this, 8907, "orbit_gauge.png", enableOrbitGauge);
                rzGauge = new RendezvousGauge();
                rzGauge.Initialize(this, 8908, "RZ_gauge.png", enableRZGauge, 1200, 1200, 1200);
                nodeGauge = new NodeGauge();
                nodeGauge.Initialize(this, 8909, "node_gauge.png", enableNodeGauge);
                hudGauge = new HudGauge();
                hudGauge.Initialize(this, 8910, enableHUDGauge);
                airGauge = new AirGauge();
                airGauge.Initialize(this, 8911, "air_gauge.png", enableAirGauge);
                navGauge = new NavGauge();
                navGauge.Initialize(this, 8912, "nav_gauge.png", enableNavGauge);
                tempGauge = new TempGauge();
                tempGauge.Initialize(this, 8913, "temp_gauge.png", enableTempGauge);
                if (debug) Debug.Log("(SG) Loading gauge settings...");
                LoadThem(config);
            }
            //Blizzy's toolbar buton setup
            if (ToolbarManager.ToolbarAvailable)
            {
                InitializeButtons();
            }
            else
            {
                GameEvents.onGUIApplicationLauncherReady.Add(InitializeButtons);
            }
            if (debug) Debug.Log("(SG) SteamGauges initialization comlete.");
        }

        private void InitializeButtons()
        {
            if (debug) Debug.Log("(SG) Initializing SteamGauges/Toolbar Integration");
            Callback onClickSteamButton = () =>
            {
                if (_allToolbar)
                {
                    if (debug) Debug.Log("(SG) SteamGauges settings toggled.");
                    advMinimized = !advMinimized;
                }
                else
                {
                    if (debug) Debug.Log("(SG) SteamGauges menu toggled.");
                    isMinimized = !isMinimized;
                }
                SaveMe();
            };

            if (ToolbarManager.ToolbarAvailable)
            {
                IButton steam_button = ToolbarManager.Instance.add("SteamGauges", "steamgauges0");
                steam_button.TexturePath = "SteamGauges/sgi";
                steam_button.ToolTip = "SteamGauges Menu";
                steam_button.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                steam_button.OnClick += (e) =>
                {
                    onClickSteamButton();
                };
                buttons[0] = steam_button;
            }
            else if (ApplicationLauncher.Ready)
            {
                ApplicationLauncherButton steam_button = ApplicationLauncher.Instance.AddModApplication(
                        onClickSteamButton,
                        onClickSteamButton,
                        () => { },
                        () => { },
                        () => { },
                        () => { },
                        ApplicationLauncher.AppScenes.FLIGHT,
                        (Texture)GameDatabase.Instance.GetTexture("SteamGauges/sgi", false));
                appButtons[0] = steam_button;
            }

            AddButton(airGauge, enableAirGauge, 1);
            AddButton(electricalGauge, enableElectricalGauge, 2);
            AddButton(fuelGauge, enableFuelGauge, 3);
            AddButton(hudGauge, enableHUDGauge, 4);
            AddButton(compassGauge, enableCompass, 5);
            AddButton(nodeGauge, enableNodeGauge, 6);
            AddButton(orbitGauge, enableOrbitGauge, 7);
            AddButton(radarAltimeter, enableRadarAltimeter, 8);
            AddButton(rzGauge, enableRZGauge, 9);
            AddButton(navGauge, enableNavGauge, 10);
            AddButton(tempGauge, enableTempGauge, 11);
        }

        private void AddButton(Gauge gauge, bool enable, int index)
        {
            if (ToolbarManager.ToolbarAvailable)
            {
                AddToolbarButton(gauge, enable, index);
            }
            else if (ApplicationLauncher.Ready)
            {
                AddAppLaunchButton(gauge, enable, index);
            }
        }

        private void AddToolbarButton(Gauge gauge, bool enable, int index)
        {
            if (enable)
            {
                IButton btn = ToolbarManager.Instance.add("SteamGauges", String.Format("steamgauges{0}", index));
                btn.TexturePath = String.Format("SteamGauges/{0}", gauge.getTextureName());
                if (gauge.isMinimized)
                    btn.ToolTip = String.Format("{0} On", gauge.getTooltipName());
                else
                    btn.ToolTip = String.Format("{0} Off", gauge.getTooltipName());
                btn.Visible = _allToolbar;
                btn.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                btn.OnClick += (e) =>
                {
                    if (gauge.isMinimized)
                        btn.ToolTip = String.Format("{0} On", gauge.getTooltipName());
                    else
                        btn.ToolTip = String.Format("{0} Off", gauge.getTooltipName());
                    gauge.toggle();
                    SaveMe();
                };
                buttons[index] = btn;
            }
        }

        private void AddAppLaunchButton(Gauge gauge, bool enable, int index)
        {
            if (enable)
            {
                Callback onClick = () =>
                {
                    gauge.toggle();
                    SaveMe();
                };
                ApplicationLauncherButton button = ApplicationLauncher.Instance.AddModApplication(
                        onClick,
                        onClick,
                        () => { },
                        () => { },
                        () => { },
                        () => { },
                        ApplicationLauncher.AppScenes.FLIGHT,
                        (Texture)GameDatabase.Instance.GetTexture(String.Format("SteamGauges/{0}", gauge.getTextureName()), false));
                appButtons[index] = button;
            }
        }

        //Clean up buttons
        private void OnDestroy()
        {
            foreach (IButton btn in buttons)
            {
                if (btn != null)
                {
                    btn.Destroy();
                }
            }
            foreach (ApplicationLauncherButton btn in appButtons)
            {
                if (btn != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(btn);
                }
            }
        }

        //Save persistant data to the config file
        public void SaveMe()               
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<SteamGauges>();
            //Save main info
            config.SetValue("WindowPosition", _windowPosition);
            config.SetValue("WindowMinimized", isMinimized);
            config.SetValue("GlobalAlpha", (double) Alpha);
            config.SetValue("AdvancedMinimized", advMinimized);
            config.SetValue("AdvancedPosition", _advwindowPosition);
            config.SetValue("DrawBezels", drawBezels);
            config.SetValue("WindowLock", windowLock);
            config.SetValue("AllToolbar", _allToolbar);
            config.SetValue("EnableAirGauge", enableAirGauge);
            config.SetValue("EnableElecGauge", enableElectricalGauge);
            config.SetValue("EnableFuelGauge", enableFuelGauge);
            config.SetValue("EnableCompass", enableCompass);
            config.SetValue("EnableHUD", enableHUDGauge);
            config.SetValue("EnableNodeGauge", enableNodeGauge);
            config.SetValue("EnableOrbitGauge", enableOrbitGauge);
            config.SetValue("EnableRadarAltimeter", enableRadarAltimeter);
            config.SetValue("EnableRZGauge", enableRZGauge);
            config.SetValue("EnableNavGauge", enableNavGauge);
            config.SetValue("EnableTempGAuge", enableTempGauge);
            //Save individual gauges
            radarAltimeter.save(config);
            compassGauge.save(config);
            electricalGauge.save(config);
            fuelGauge.save(config);
            orbitGauge.save(config);
            rzGauge.save(config);
            nodeGauge.save(config);
            hudGauge.save(config);
            airGauge.save(config);
            navGauge.save(config);
            tempGauge.save(config);
            config.save();
        }

        //Load persistant data from the config file
        public void LoadMe(PluginConfiguration config)             
        {
            //Load main info
            _windowPosition = config.GetValue<Rect>("WindowPosition", new Rect(200, 200, 250f, 200f));
            _windowPosition.width = 10f;    //Make it small, so it can be resized larger
            _windowPosition.height = 10f;
            isMinimized = config.GetValue<bool>("WindowMinimized", true);
            Alpha = (float) config.GetValue<double>("GlobalAlpha", 1);   //No ability to save floats, so I save as a double
            advMinimized = config.GetValue<bool>("AdvancedMinimized", true);
            _advwindowPosition = config.GetValue<Rect>("AdvancedPosition", new Rect(200, 200, 300f, 300f));
            _advwindowPosition.width = 10f; //Make it small, so it can be resised larger
            _advwindowPosition.height = 10f;
            drawBezels = config.GetValue<bool>("DrawBezels", true);
            windowLock = config.GetValue<bool>("WindowLock", false);
            _allToolbar = config.GetValue<bool>("AllToolbar", false);
            enableAirGauge = config.GetValue<bool>("EnableAirGauge", true);
            enableCompass = config.GetValue<bool>("EnableCompass", true);
            enableElectricalGauge = config.GetValue<bool>("EnableElecGauge", true);
            enableFuelGauge = config.GetValue<bool>("EnableFuelGauge", true);
            enableHUDGauge = config.GetValue<bool>("EnableHUD", true);
            enableNodeGauge = config.GetValue<bool>("EnableNodeGauge", true);
            enableOrbitGauge = config.GetValue<bool>("EnableOrbitGauge", true);
            enableRadarAltimeter = config.GetValue<bool>("EnableRadarAltimeter", true);
            enableRZGauge = config.GetValue<bool>("EnableRZGauge", true);
            enableNavGauge = config.GetValue<bool>("EnableNaveGauge", true);
            enableTempGauge = config.GetValue<bool>("EnableTempGauge", true);
        }

        // Replace RenderingManager.AddToPreDrawQueue
        public void AddToPreDrawQueue(Callback drawFunction)
        {
            if (preDrawCallbacks == null)
            {
                preDrawCallbacks = drawFunction;
            }
            else
            {
                preDrawCallbacks += drawFunction;
            }
        }

        // Replace RenderingManager.AddToPostDrawQueue
        public void AddToPostDrawQueue(Callback drawFunction)
        {
            if (postDrawCallbacks == null)
            {
                postDrawCallbacks = drawFunction;
            }
            else
            {
                postDrawCallbacks += drawFunction;
            }
        }

        //Loads each gauge's configuration
        private void LoadThem(PluginConfiguration config)
        {
            radarAltimeter.load(config);
            compassGauge.load(config);
            electricalGauge.load(config);
            fuelGauge.load(config);
            orbitGauge.load(config);
            rzGauge.load(config);
            nodeGauge.load(config);
            hudGauge.load(config);
            airGauge.load(config);
            navGauge.load(config);
            tempGauge.load(config);
        }

        //What to do when we are drawn
        private void OnDraw()
        {
            //SteamShip updating
            SteamShip.update();
            
            //Alpha blending
            Color tmpColor = GUI.color;
            GUI.color = new Color(1, 1, 1, Alpha);

            //Draw the main window, if not minimized
            if (!isMinimized)
            {
                //Check window off screen
                if ((_windowPosition.xMin + _windowPosition.width) < 20) _windowPosition.xMin = 20 - _windowPosition.width; //left limit
                if (_windowPosition.yMin + _windowPosition.height < 20) _windowPosition.yMin = 20 - _windowPosition.height; //top limit
                if (_windowPosition.xMin > Screen.width - 20) _windowPosition.xMin = Screen.width - 20;   //right limit
                if (_windowPosition.yMin > Screen.height - 20) _windowPosition.yMin = Screen.height - 20; //bottom limit
                String title = "SteamGauges " + VersionString;
                if (!CompatibilityChecker.IsCompatible())
                    title = title + " Incompatible Version!";
                _windowPosition = GUILayout.Window(8901, _windowPosition, OnWindow, title, _windowStyle);
            }
            //Draw the advanced window, if not minimized
            if (!advMinimized)
            {   
                //Check window off screen
                if ((_advwindowPosition.xMin + _advwindowPosition.width) < 20) _advwindowPosition.xMin = 20 - _advwindowPosition.width; //left limit
                if (_advwindowPosition.yMin + _advwindowPosition.height < 20) _advwindowPosition.yMin = 20 - _advwindowPosition.height; //top limit
                if (_advwindowPosition.xMin > Screen.width - 20) _advwindowPosition.xMin = Screen.width - 20;   //right limit
                if (_advwindowPosition.yMin > Screen.height - 20) _advwindowPosition.yMin = Screen.height - 20; //bottom limit
                _advwindowPosition = GUILayout.Window(8902, _advwindowPosition, OnAdvanced, "SteamGauges"+VersionString+" Settings", _windowStyle);
            }

            //Reset alpha so we don't blend out stuff unintentionally
            GUI.color = tmpColor;
        }

        //Draw the advanced settings window
        private void OnAdvanced(int WindowID)
        {
            //Radar Altimeter Settings
            GUILayout.Label("Radar Altimeter Settings", _boldStyle, GUILayout.Width(500));  //Hopefully set the width of the window betterer
            GUILayout.BeginHorizontal();
            GUILayout.Label("Altitude Lights: G, Y, R", GUILayout.Width(200));
            radarAltimeter.setGreen(int.Parse(GUILayout.TextField(radarAltimeter.getGreenLight().ToString())));
            radarAltimeter.setYellow(int.Parse(GUILayout.TextField(radarAltimeter.getYellowLight().ToString())));
            radarAltimeter.setRed(int.Parse(GUILayout.TextField(radarAltimeter.getRedLight().ToString())));
            GUILayout.Label("Calibration (meters):", GUILayout.Width(200));
            radarAltimeter.calibration = int.Parse(GUILayout.TextField(radarAltimeter.calibration.ToString(), GUILayout.Width(100)));
            GUILayout.Label("Cutoff Alt: ", GUILayout.Width(150));
            radarAltimeter.contact_alt = int.Parse(GUILayout.TextField(radarAltimeter.contact_alt.ToString(), GUILayout.Width(100)));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale", GUILayout.Width(50));   //It would be nice to make this dynamic
            radarAltimeter.setScale(GUILayout.HorizontalSlider(radarAltimeter.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            int s = (int) (radarAltimeter.getScale() * 100);    //0.5 to 50
            GUILayout.Label(s.ToString()+'%', GUILayout.Width(50));    //Show, but don't allow editing of scale value
            GUILayout.EndHorizontal();
            //Compass/Electrical/Temp
            GUILayout.BeginHorizontal();
            GUILayout.Label("Magnetic Compass Settings", _boldStyle, GUILayout.Width(300)); 
            GUILayout.Label("Electrical Gauge Settings", _boldStyle, GUILayout.Width(300));
            GUILayout.Label("Temperature Gauge Settings", _boldStyle, GUILayout.Width(300));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale", GUILayout.Width(50));
            s = (int)(compassGauge.getScale() * 100);
            compassGauge.setScale(GUILayout.HorizontalSlider(compassGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            GUILayout.Label(s.ToString()+'%', GUILayout.Width(50));    //Show, but don't allow editing of scale value
            GUILayout.Label("Scale", GUILayout.Width(50));
            electricalGauge.setScale(GUILayout.HorizontalSlider(electricalGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(electricalGauge.getScale() * 100);
            GUILayout.Label(s.ToString() + '%', GUILayout.Width(50));    //Show, but don't allow editing of scale value
            GUILayout.Label("Scale", GUILayout.Width(50));
            tempGauge.setScale(GUILayout.HorizontalSlider(tempGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(tempGauge.getScale() * 100);
            GUILayout.Label(s.ToString() + '%', GUILayout.Width(50));    //Show, but don't allow editing of scale value
            GUILayout.EndHorizontal();
            //Fuel Gauge
            GUILayout.Label("Fuel Gauge Settings", _boldStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fuel Lights: R, Y, G", GUILayout.Width(200));
            fuelGauge.setFuelRed(float.Parse(GUILayout.TextField(fuelGauge.getFuelRed().ToString())));
            fuelGauge.setFuelYellow(float.Parse(GUILayout.TextField(fuelGauge.getFuelYellow().ToString())));
            fuelGauge.setFuelGreen(float.Parse(GUILayout.TextField(fuelGauge.getFuelGreen().ToString())));
            GUILayout.Label("Mono Lights: R, Y, G", GUILayout.Width(200));
            fuelGauge.setMonoRed(float.Parse(GUILayout.TextField(fuelGauge.getMonoRed().ToString())));
            fuelGauge.setMonoYellow(float.Parse(GUILayout.TextField(fuelGauge.getMonoYellow().ToString())));
            fuelGauge.setMonoGreen(float.Parse(GUILayout.TextField(fuelGauge.getMonoGreen().ToString())));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale", GUILayout.Width(50));
            fuelGauge.setScale(GUILayout.HorizontalSlider(fuelGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(fuelGauge.getScale() * 100);
            GUILayout.Label(s.ToString()+'%', GUILayout.Width(50));    //Show, but don't allow editing of scale value
            GUILayout.EndHorizontal();
            //Orbital Gauge
            GUILayout.Label("Orbital Gauge Settings", _boldStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Safe Altitude", GUILayout.Width(200));
            orbitGauge.setGreenAlt(double.Parse(GUILayout.TextField(orbitGauge.getGreenAlt().ToString(), GUILayout.Width(100))));
            GUILayout.Label("Circularization Threshold", GUILayout.Width(200));
            orbitGauge.setCircleThresh(double.Parse(GUILayout.TextField(orbitGauge.getCircleThresh().ToString(), GUILayout.Width(100))));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Burn Window", GUILayout.Width(200));
            orbitGauge.setBurnWindow(int.Parse(GUILayout.TextField(orbitGauge.getBurnWindow().ToString(), GUILayout.Width(100))));
            GUILayout.Label("Scale", GUILayout.Width(50));
            orbitGauge.setScale(GUILayout.HorizontalSlider(orbitGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(orbitGauge.getScale() * 100);
            GUILayout.Label(s.ToString()+'%', GUILayout.Width(50));    //Show, but don't allow editing of scale value
            String text = "Negative Pe";
            if (!orbitGauge.showNegativePe) text = "Zero Pe";
            if (GUILayout.Button(text, _buttonStyle, GUILayout.Width(100))) orbitGauge.showNegativePe = !orbitGauge.showNegativePe;
            GUILayout.EndHorizontal();
            //RZ Gauge
            GUILayout.Label("Rendezvous Gauge Settings", _boldStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Distance Lights: R, Y, G", GUILayout.Width(200));
            rzGauge.setRed(int.Parse(GUILayout.TextField(rzGauge.getRed().ToString())));
            rzGauge.setYellow (int.Parse(GUILayout.TextField(rzGauge.getYellow().ToString())));
            rzGauge.setGreen(int.Parse(GUILayout.TextField(rzGauge.getGreen().ToString())));
            GUILayout.Label("Scale", GUILayout.Width(50));
            rzGauge.setScale(GUILayout.HorizontalSlider(rzGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int) (rzGauge.getScale() * 100);
            GUILayout.Label(s.ToString()+'%', GUILayout.Width(50));    //Show, but don't allow editing of scale value
            GUILayout.EndHorizontal();
            //Node Gauge
            GUILayout.Label("Maneuver Node Gauge Settings", _boldStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Throttle Delay");
            nodeGauge.setThrottleDelay(int.Parse(GUILayout.TextField(nodeGauge.getThrottleDelay().ToString(), GUILayout.Width(100))));
            nodeGauge.setUseCalcBurn(GUILayout.Toggle(nodeGauge.getUseCalcBurn(), "Use Calculated Burn Time", _toggleStyle, GUILayout.Width(300)));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale", GUILayout.Width(50));
            nodeGauge.setScale(GUILayout.HorizontalSlider(nodeGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(nodeGauge.getScale() * 100);
            GUILayout.Label(s.ToString()+'%', GUILayout.Width(50));
            GUILayout.EndHorizontal();
            //Air Gauge/Nav Gauge
            GUILayout.BeginHorizontal();
            GUILayout.Label("Air Gauge Settings", _boldStyle, GUILayout.Width(400));
            GUILayout.Label("Nav Gauge Settings", _boldStyle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale", GUILayout.Width(50));
            airGauge.setScale(GUILayout.HorizontalSlider(airGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(airGauge.getScale() * 100);
            GUILayout.Label(s.ToString() + '%', GUILayout.Width(35));
            GUILayout.Label("Stall AoA", GUILayout.Width(100));
            airGauge.criticalAOA = (int.Parse(GUILayout.TextField(airGauge.criticalAOA.ToString(),GUILayout.Width(25))));
            GUILayout.Label("Scale", GUILayout.Width(50));
            navGauge.setScale(GUILayout.HorizontalSlider(navGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(navGauge.getScale() * 100);
            GUILayout.Label(s.ToString() + '%', GUILayout.Width(35));
            GUILayout.EndHorizontal();
            //HUD Gauge
            GUILayout.Label("HUD Settings", _boldStyle);
            //HUD color
            GUILayout.BeginHorizontal();
            Color tmp = GUI.color;
            GUI.color = new Color((float)hudGauge.Red / 255f, (float) hudGauge.Green / 255f, (float) hudGauge.Blue / 255f);
            GUILayout.Label("HUD Color: ", GUILayout.Width(75), GUILayout.Height(40));
            GUI.color = tmp;
            GUILayout.Label("     Red", GUILayout.Width(50), GUILayout.Height(40));
            hudGauge.Red = (int)GUILayout.HorizontalSlider(hudGauge.Red, 0, 255, GUILayout.Width(100), GUILayout.Height(40));
            GUILayout.Label("   Green", GUILayout.Width(50), GUILayout.Height(40));
            hudGauge.Green = (int)GUILayout.HorizontalSlider(hudGauge.Green, 0, 255, GUILayout.Width(100), GUILayout.Height(40));
            GUILayout.Label("     Blue", GUILayout.Width(50), GUILayout.Height(40));
            hudGauge.Blue = (int)GUILayout.HorizontalSlider(hudGauge.Blue, 0, 255, GUILayout.Width(100), GUILayout.Height(40));
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Use High Contrast Color in HUD"))
                hudGauge.color_center = !hudGauge.color_center;
            //HUD Inverse color
            GUILayout.BeginHorizontal();
            tmp = GUI.color;
            if (hudGauge.color_center)
                GUI.color = new Color((float)hudGauge.cRed / 255f, (float)hudGauge.cGreen / 255f, (float)hudGauge.cBlue / 255f);
            else
                GUI.enabled = false;
            GUILayout.Label("HUD Contrast Color: ", GUILayout.Width(75), GUILayout.Height(40));
            GUI.color = tmp;
            GUILayout.Label("     Red", GUILayout.Width(50), GUILayout.Height(40));
            hudGauge.cRed = (int)GUILayout.HorizontalSlider(hudGauge.cRed, 0, 255, GUILayout.Width(100), GUILayout.Height(40));
            GUILayout.Label("   Green", GUILayout.Width(50), GUILayout.Height(40));
            hudGauge.cGreen = (int)GUILayout.HorizontalSlider(hudGauge.cGreen, 0, 255, GUILayout.Width(100), GUILayout.Height(40));
            GUILayout.Label("     Blue", GUILayout.Width(50), GUILayout.Height(40));
            hudGauge.cBlue = (int)GUILayout.HorizontalSlider(hudGauge.cBlue, 0, 255, GUILayout.Width(100), GUILayout.Height(40));
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            int selected = -1;
            selected = GUILayout.SelectionGrid(selected, buttonNames, 4, _buttonStyle, GUILayout.Width(600));
            switch (selected)
            {
                case 0:
                    //Orbital toggle
                    hudGauge.drawOrbital = !hudGauge.drawOrbital;
                    break;
                case 1:
                    //Throttle toggle
                    hudGauge.drawThrottle = !hudGauge.drawThrottle;
                    break;
                case 2:
                    //Indicator Toggle
                    hudGauge.drawIndicators = !hudGauge.drawIndicators;
                    break;
                case 3:
                    //Target Info
                    hudGauge.drawTargetInfo = !hudGauge.drawTargetInfo;
                    break;
                case 4:
                    //Node Info
                    hudGauge.drawNodeInfo = !hudGauge.drawNodeInfo;
                    break;
                case 5:
                    //Rotate Pitch Ladder
                    hudGauge.rotatePitch = !hudGauge.rotatePitch;
                    break;
                case 6:
                    //Draw speed/alt
                    hudGauge.drawSpdAlt = !hudGauge.drawSpdAlt;
                    break;
                case 7:
                    //GPWS
                    hudGauge.useGPWS = !hudGauge.useGPWS;
                    break;
                case 8:
                    //Center HUD
                    hudGauge.centerWindow();
                    break;
                case 9:
                    //Toggle Oribtal mode
                    hudGauge.orbitalMode = !hudGauge.orbitalMode;
                    break;
                case 10:
                    //Toggle Warning flags
                    hudGauge.drawWarnings = !hudGauge.drawWarnings;
                    break;
                case 11:
                    //Toggle EAS/TAS
                    hudGauge.useEAS = !hudGauge.useEAS;
                    break;
                default:
                    break;
            }
            if (hudGauge.drawOrbital) buttonNames[0] = "Orbital Info Off"; else buttonNames[0] = "Orbital Info On";
            if (hudGauge.drawThrottle) buttonNames[1] = "Throttle Off"; else buttonNames[1] = "Throttle On";
            if (hudGauge.drawIndicators) buttonNames[2] = "Indicators Off"; else buttonNames[2] = "Indicators On";
            if (hudGauge.drawTargetInfo) buttonNames[3] = "Target Info Off"; else buttonNames[3] = "Target Info On";
            if (hudGauge.drawNodeInfo) buttonNames[4] = "Node Info Off"; else buttonNames[4] = "Node Info On";
            if (hudGauge.rotatePitch) buttonNames[5] = "Rotate Pitch Off"; else buttonNames[5] = "Rotate Pitch On";
            if (hudGauge.drawSpdAlt) buttonNames[6] = "Central Spd/Alt Off"; else buttonNames[6] = "Central Spd/Alt On";
            if (hudGauge.useGPWS) buttonNames[7] = "GPWS Off"; else buttonNames[7] = "GPWS On";
            //Center HUD botton doesn't change when you click on it.
            if (hudGauge.orbitalMode) buttonNames[9] = "Oribital Mode Off"; else buttonNames[9] = "Orbital Mode On";
            if (hudGauge.drawWarnings) buttonNames[10] = "Warnings Off"; else buttonNames[10] = "Warnings On";
            buttonNames[11] = hudGauge.useEAS ? "Use TAS" : "Use EAS";
            GUILayout.BeginHorizontal();
            //IVA Only
            hudGauge.ivaOnly = GUILayout.Toggle(hudGauge.ivaOnly, "IVA Only", _toggleStyle, GUILayout.Width(150));
            //Mouse Input
            hudGauge.mouseInput = GUILayout.Toggle(hudGauge.mouseInput, "Mouse Input", _toggleStyle, GUILayout.Width(150));
            GUI.enabled = hudGauge.mouseInput;
            hudGauge.planeMode = GUILayout.Toggle(hudGauge.planeMode, hudGauge.planeMode ? "Plane" : "Rocket", _buttonStyle, GUILayout.Width(150));
            GUI.enabled = true; //hmm
            GUILayout.Label("HUD Key:");
            String key = "";
            foreach (KeyCode code in hudGauge.hudKeys)
                key = key + code.ToString()+" ";
            GUILayout.TextArea(key);
            /*if (GUILayout.Button("Assign Key", _buttonStyle, GUILayout.Width(100)))
            {
                //This is terrible right now, you pretty much have to hold down the key, then press the button.
                if (Input.anyKey)
                {
                    key = Input.inputString;
                    if (Event.current.shift)
                        key = key + " shift";
                    if (Event.current.alt)
                        key = key + " alt";
                    if (Event.current.control)
                        key = key + " ctrl";
                    Debug.Log("Found keys: " + key);
                }
            } */
            GUILayout.EndHorizontal();
            //Scale
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale", GUILayout.Width(50));
            hudGauge.setScale(GUILayout.HorizontalSlider(hudGauge.getScale(), 0.12f, 1f, GUILayout.Width(150)));
            s = (int)(hudGauge.getScale() * 100);
            GUILayout.Label(s.ToString()+'%', GUILayout.Width(50));
            GUILayout.EndHorizontal();
            if (_allToolbar)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Transparencey:", _labelStyle, GUILayout.Width(130));
                Alpha = GUILayout.HorizontalSlider(Alpha, 0.1f, 1f, GUILayout.Width(130));
                //Update red for alpha
                Red = new Color(1, 0, 0, Alpha);
                GUILayout.Label("HUD opacity:", _labelStyle, GUILayout.Width(130));
                hudGauge.Alpha = (int)GUILayout.HorizontalSlider(hudGauge.Alpha, 1f, 255f, GUILayout.Width(130));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                drawBezels = GUILayout.Toggle(drawBezels, "Draw Large Bezels", _toggleStyle, GUILayout.Width(250));
                windowLock = GUILayout.Toggle(windowLock, "Lock Gauge Posistions", _toggleStyle, GUILayout.Width(250));
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", _buttonStyle)) advMinimized = true;
            String capt = "Use Toolbar for gauge toggle";
            if (_allToolbar == true) capt = "Use window for gauge toggle";
            if (GUILayout.Button(capt, _buttonStyle)) toolbarToggle();
            GUILayout.EndHorizontal();
            if (GUI.changed)  SaveMe();
            //Make it dragable
            GUI.DragWindow();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint || Event.current.isMouse)
            {
                if (preDrawCallbacks != null)
                {
                    preDrawCallbacks();
                }
            }
            if (postDrawCallbacks != null)
            {
                postDrawCallbacks();
            }
        }

        //This toggles the visibility of the extra toolbar buttons, based on the user selected mode.
        private void toolbarToggle()
        {
            _allToolbar = !_allToolbar;
            if (_allToolbar)
                isMinimized = true;
            for (int i = 1; i < buttons.Length; i++)    // skip first button
            {
                IButton btn = buttons[i];
                if (btn != null)
                {
                    btn.Visible = _allToolbar;
                }
            }
        }

        private bool WindowToggle(bool cur_state, System.String name, int width)
        {
            return cur_state != GUILayout.Toggle(cur_state, name, _buttonStyle, GUILayout.Width(width));
        }

        //basically, the layout function, but also adds dragability
        private void OnWindow(int WindowID)
        {
            //Directions
            GUILayout.Label("Please select the desired gauges:", _labelStyle);
            //Buttons for each of the gauges
            GUILayout.BeginHorizontal();                            //Arrange buttons side-by-side
            if (WindowToggle(!radarAltimeter.isMinimized, "Radar Altimeter", 130))
            {
                radarAltimeter.toggle();
                SaveMe();
            }
            if (WindowToggle(!compassGauge.isMinimized, "Magnetic Compass", 130))
            {
                compassGauge.toggle();
                SaveMe();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (WindowToggle(!electricalGauge.isMinimized, "Electrical Gauge", 130))
            {
                electricalGauge.toggle();
                SaveMe();
            }
            if (WindowToggle(!fuelGauge.isMinimized, "Fuel Gauge", 130))
            {
                fuelGauge.toggle();
                SaveMe();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (WindowToggle(!orbitGauge.isMinimized, "Orbital Gauge", 130))
            {
                orbitGauge.toggle();
                SaveMe();
            }
            if (WindowToggle(!rzGauge.isMinimized, "Rendezvous Gauge", 130))
            {
                rzGauge.toggle();
                SaveMe();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (WindowToggle(!nodeGauge.isMinimized, "Node Gauge", 130))
            {
                nodeGauge.toggle();
                SaveMe();
            }
            bool miniHud = hudGauge.mouseInput && hudGauge.mouseInputOnly;
            if (WindowToggle(!hudGauge.isMinimized, miniHud ? "HUD (Mini)" : "HUD", 130))
            {
                hudGauge.toggle();
                SaveMe();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (WindowToggle(!airGauge.isMinimized, "Air Gauge", 130))
            {
                airGauge.toggle();
                SaveMe();
            }
            if (hudGauge.mouseInput && WindowToggle(hudGauge.planeMode, hudGauge.planeMode ? "Mouse: Plane" : "Mouse: Rocket", 130))
            {
                hudGauge.planeMode = !hudGauge.planeMode;
                SaveMe();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (WindowToggle(!navGauge.isMinimized, "Nav Guage", 130))
            {
                navGauge.toggle();
                SaveMe();
            }
            GUILayout.EndHorizontal();
            //Alpha transparency control
            GUILayout.BeginHorizontal();
            GUILayout.Label("Transparencey:", _labelStyle, GUILayout.Width(130));
            Alpha = GUILayout.HorizontalSlider(Alpha, 0.1f, 1f, GUILayout.Width(130));
            //Update red for alpha
            Red = new Color(1, 0, 0, Alpha);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("HUD opacity:", _labelStyle, GUILayout.Width(130));
            hudGauge.Alpha = (int)GUILayout.HorizontalSlider(hudGauge.Alpha, 1f, 255f, GUILayout.Width(130));
            GUILayout.EndHorizontal();
            drawBezels = GUILayout.Toggle(drawBezels, "Draw Large Bezels", _toggleStyle);
            windowLock = GUILayout.Toggle(windowLock, "Lock Gauge Posistions", _toggleStyle);
            GUILayout.BeginHorizontal();
            if (WindowToggle(!advMinimized, "Advanced Settings", 130))
            {
                advMinimized = !advMinimized;
                SaveMe();
            }
            if (WindowToggle(isMinimized, "Close", 130))
            {
                isMinimized = !isMinimized;
                SaveMe();
            }
            GUILayout.EndHorizontal();
            //make it dragable
            GUI.DragWindow(); 
        }

        //Initialize window and label styles, which will eventually go away
        private void initStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.stretchHeight = true;
            _windowStyle.stretchWidth = true;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _labelStyle.stretchWidth = true;
            _toggleStyle = new GUIStyle(HighLogic.Skin.toggle);     //This...doesn't do as much as I would like
            _boldStyle = new GUIStyle(HighLogic.Skin.label);
            _boldStyle.fontStyle = FontStyle.Bold;
            _boldStyle.fontSize = 16;                               //Larger, but not really very bold
            _boldStyle.stretchWidth = true;
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _hasInitStyles = true;
            _IconStyle = new GUIStyle();                            //I guess this is just an empty style...

        }
    }
}
