using System;
using KSP;
using UnityEngine;
using KSP.IO;
using System.Collections.Generic;

namespace SteamGauges
{
    public struct GaugeButton
    {
        public Rect position;
        public Rect permPosition;
        public Rect onTexture;
        public Rect offTexture;
        public bool active;
    }
    
    //The Gauge class is a prototype for all the individual gauge classes.  It defines general gauge behavior and includes universal items.
    abstract class Gauge : MonoBehaviour
    {
        public Rect windowPosition;                                     //The position for our little window (left, top, width, height)
        protected Rect lastPosition;                                    //Used so I don't over-save
        public bool isMinimized;                                        //Is the window currently minimized?
        protected bool isEnabled;                                         //Is this gauge currently enabled?
        protected  float Scale;                                         //Scales the gauge to different sizes
        protected int windowID;                                         //So each gauge is treated separately
        protected int base_width;                                       //Gauge width before scaling
        protected int base_height;                                      //Gauge height before scaling
        protected static SteamGauges home;                                     //So I don't need statics
        protected GaugeButton[] buttons;                                //An array for this gauge's buttons, if present.
        protected bool useDrawButtons = true;                                  //Set to false to override built-in button drawing
        protected Texture2D texture;                                    //The main texture atlas for this gauge

        private static Dictionary<String, Texture2D> texture_cache = new Dictionary<String, Texture2D>();

        public float getScale()
        {
            return Scale;
        }

        public void setScale(float v)
        {
            if (v < 0.12f) v = 0.12f;
            if (v > 1) v = 1;
            Scale = v;
        }

        //Simple initialization code
        public bool Initialize(SteamGauges sg, int id, String texture_name, bool enable,int tex_w=800, int tex_h=814, int w=400, int h=407)
        {
            try
            {
                if (!texture_cache.TryGetValue(texture_name, out this.texture))
                {
                    Byte[] array;
                    array = KSP.IO.File.ReadAllBytes<SteamGauges>(texture_name);
                    //Integral texture loading
                    this.texture = new Texture2D(tex_w, tex_h);
                    texture_cache.Add(texture_name, this.texture);
                    texture.LoadImage(array);
                }
                isEnabled = enable;
                base_width = w;
                base_height = h;
                home = sg;
                windowID = id;
                windowPosition = new Rect(300, 300, base_width, base_height);
                lastPosition = windowPosition;
                sg.AddToPostDrawQueue(OnDraw);
            }
            catch
            {
                Debug.LogError("Initialization error, probably with "+texture_name);
                return false;
            }
            return true;
        }

        //What to do when we are drawn
        public void OnDraw()
        {
            if (this.isVisible() && this.isEnabled)
            {
                //Window scaling
                windowPosition.width = base_width * Scale;
                windowPosition.height = base_height * Scale;
                //Button scaling
                if (buttons != null)
                {
                    for (int i = 0; i < buttons.Length;i++)
                    {
                        buttons[i].position = new Rect(buttons[i].permPosition.xMin * Scale, buttons[i].permPosition.yMin * Scale, buttons[i].permPosition.width * Scale, buttons[i].permPosition.height * Scale);
                    }
                }
                //Check window off screen
                if ((windowPosition.xMin + windowPosition.width) < 20) windowPosition.xMin = 20 - windowPosition.width; //left limit
                if (windowPosition.yMin + windowPosition.height < 20) windowPosition.yMin = 20 - windowPosition.height; //top limit
                if (windowPosition.xMin > Screen.width - 20) windowPosition.xMin = Screen.width - 20;   //right limit
                if (windowPosition.yMin > Screen.height - 20) windowPosition.yMin = Screen.height - 20; //bottom limit
                windowPosition = GUI.Window(windowID, windowPosition, OnWindow, "", SteamGauges._labelStyle); //labelStyle makes my window invisible, which is nice
            }
        }

        //This method is replaced by gauges to see if it should be drawn
        //It should check for isMinimized, as well as any gauge-specific
        //conditions (in atmosphere, maneuver node, etc
        protected abstract bool isVisible();

        //basically, the layout function, but also adds dragability
        protected void OnWindow(int WindowID)
        {
            //Alpha blending
            Color tmpColor = GUI.color;
            GUI.color = new Color(1, 1, 1, SteamGauges.Alpha);
            
            //Checks for any buttons that may be on the gauge.
            bool save = GaugeButtons();

            //Performs the actions and drawing specific to this gauge.
            GaugeActions();
            //Draws any buttons that may be on the gauge
            DrawGaugeButtons();

            //Reset Alpha blending
            GUI.color = tmpColor;

            //Make it dragable
            if (!SteamGauges.windowLock)
                GUI.DragWindow();

            //Save check so we only save after draging
            if (save || windowPosition.x != lastPosition.x || windowPosition.y != lastPosition.y)
            {
                lastPosition = windowPosition;
                home.SaveMe();
            }
        }

        //Checks to see if any buttons (if they exist) have been pressed
        private bool GaugeButtons()
        {
            bool force_save = false;
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length;i++)
                {
                    if (Event.current.type == EventType.MouseUp && buttons[i].position.Contains(Event.current.mousePosition))
                    {
                        buttons[i].active = !buttons[i].active;
                        //home.SaveMe();
                        force_save = true;
                    }
                }
            }
            return force_save;
        }

        private void DrawGaugeButtons()
        {
            if (buttons != null && useDrawButtons)
            {
                if (Event.current.type != EventType.repaint)
                    return;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].active)
                        GUI.DrawTextureWithTexCoords(buttons[i].position, texture, buttons[i].onTexture);
                    else
                        GUI.DrawTextureWithTexCoords(buttons[i].position, texture, buttons[i].offTexture);
                }
            }
        }

        protected abstract void GaugeActions();

        //Toggles visability of this gauge
        public void toggle()
        {
            this.isMinimized = !this.isMinimized;
        }

        //Loads values from the config file
        public abstract void load(PluginConfiguration config);

        //Saves values to the config file
        public abstract void save(PluginConfiguration config);

    }
}
