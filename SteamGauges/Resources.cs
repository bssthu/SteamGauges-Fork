using System;
using UnityEngine;
using KSP;

namespace SteamGauges
{
    public static class Resources
    {
        private static bool loaded = false; //Thanks to a.g. for pointing out that I was reloading these each sceen change.

        //private static int width = 400;
        //private static int height = 407;
        //The textures I'll be using       
        //Radar Altimeter      
        //public static Texture2D rad_alt_atlas = new Texture2D(465, 673);
        //Fuel/MonoPro Gauge
        //public static Texture2D fuel_atlas = new Texture2D(800, 700);
        //Electrical Gauge
        //public static Texture2D elec_atlas = new Texture2D(800, 814);
        //Magnetic Compass
        //public static Texture2D compass_atlas = new Texture2D(1134, 574);
        //Orbital Information
        //public static Texture2D orbit_atlas = new Texture2D(800, 814);
        public static Texture2D digits = new Texture2D(20, 330);
        public static Texture2D digits6 = new Texture2D(20, 210);
        public static Texture2D orbit_chars = new Texture2D(20, 90);
        //Rendezvous Information
        //public static Texture2D RZ_atlas = new Texture2D(1200, 1200);
        public static Texture2D minus = new Texture2D(20, 28);
        //Maneuver Node Information
        //public static Texture2D node_atlas = new Texture2D(800, 814);
        //public static Texture2D air_atlas = new Texture2D(800, 814);
        //HUD
        //public static Texture2D HUD_bg = new Texture2D(1024, 768);
        //public static Texture2D HUD_roll_ptr = new Texture2D(1024, 768);
        public static Texture2D HUD_digits = new Texture2D(20, 330);
        public static Texture2D HUD_digits6 = new Texture2D(20, 210);
        public static Texture2D HUD_chars = new Texture2D(20, 150);
        public static Texture2D HUD_compass = new Texture2D(3500, 110); 
        public static Texture2D HUD_ladder = new Texture2D(300, 2600);
        public static Material HUD_ladder_mat = null;
        public static Texture2D HUD_vert = new Texture2D(900, 900);
        public static Material HUD_vert_mat = null;
        public static Texture2D HUD_vertd = new Texture2D(900, 900);
        public static Material HUD_vertd_mat = null;
        public static Texture2D HUD_speed_tape1 = new Texture2D(150, 3289);
        public static Texture2D HUD_speed_tape2 = new Texture2D(150, 2740);
        public static Texture2D HUD_speed_tape3 = new Texture2D(150, 3290);
        public static Texture2D HUD_speed_tape4 = new Texture2D(150, 2475);
        public static Texture2D HUD_alt_tape1 = new Texture2D(150, 3297);
        public static Texture2D HUD_alt_tape2 = new Texture2D(150, 4119);
        public static Texture2D HUD_alt_tape3 = new Texture2D(150, 3297);
        public static Texture2D HUD_alt_tape4 = new Texture2D(150, 3571);
        public static Texture2D HUD_extras = new Texture2D(1024, 768);

        //Actually load the textures from files
        public static void loadAssets()
        {
            if (loaded) return;     //Don't releoad assets
            Byte[] arrBytes;
            //Radar Altimeter
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("rad_alt.png");
            //rad_alt_atlas.LoadImage(arrBytes);
            //Fuel/MonoPro Gauge
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("fuel_gauge.png");
            //fuel_atlas.LoadImage(arrBytes);
            //Electrical Gauge
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("ammeter_volmeter.png");
            //elec_atlas.LoadImage(arrBytes);
            //Magnetic Compass
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("magnetic_compass.png");
            //compass_atlas.LoadImage(arrBytes);
            //Orbital Gauge
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("orbit_gauge.png");
            //orbit_atlas.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("digits.png");
            digits.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("digits6.png");
            digits6.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("orbit_chars.png");
            orbit_chars.LoadImage(arrBytes);
            //Rendesvous Gauge
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("RZ_gauge.png");
            //RZ_atlas.LoadImage(arrBytes);
            //Minus sign for digits
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("minus.png");
            minus.LoadImage(arrBytes);
            //Maneuver Node
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("node_gauge.png");
            //node_atlas.LoadImage(arrBytes);
            //Air Gauge
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("air_gauge.png");
            //air_atlas.LoadImage(arrBytes);
            //HUD
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_static.png");
            //HUD_bg.LoadImage(arrBytes);
            //arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_roll_pointer.png");
            //HUD_roll_ptr.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_digits.png");
            HUD_digits.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_digits6.png");
            HUD_digits6.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_chars.png");
            HUD_chars.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_compass.png");
            HUD_compass.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_ladder.png");
            HUD_ladder.LoadImage(arrBytes);
            HUD_ladder.wrapMode = TextureWrapMode.Clamp;
            HUD_ladder_mat = new Material(Shader.Find("Hidden/Internal-GUITexture"));
            HUD_ladder_mat.mainTexture = Resources.HUD_ladder;
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_vert.png");
            HUD_vert.LoadImage(arrBytes);
            HUD_vert.wrapMode = TextureWrapMode.Clamp;
            HUD_vert_mat = new Material(Shader.Find("Hidden/Internal-GUITexture"));
            HUD_vert_mat.mainTexture = Resources.HUD_vert;
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_vertd.png");
            HUD_vertd.LoadImage(arrBytes);
            HUD_vertd.wrapMode = TextureWrapMode.Clamp;
            HUD_vertd_mat = new Material(Shader.Find("Hidden/Internal-GUITexture"));
            HUD_vertd_mat.mainTexture = Resources.HUD_vertd;
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_speed_tape1.png");
            HUD_speed_tape1.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_speed_tape2.png");
            HUD_speed_tape2.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_speed_tape3.png");
            HUD_speed_tape3.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_speed_tape4.png");
            HUD_speed_tape4.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_alt_tape1.png");
            HUD_alt_tape1.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_alt_tape2.png");
            HUD_alt_tape2.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_alt_tape3.png");
            HUD_alt_tape3.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_alt_tape4.png");
            HUD_alt_tape4.LoadImage(arrBytes);
            arrBytes = KSP.IO.File.ReadAllBytes<SteamGauges>("hud_extras.png");
            HUD_extras.LoadImage(arrBytes);
            loaded = true;
        }

    }
}
