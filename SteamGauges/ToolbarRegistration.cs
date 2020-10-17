
using UnityEngine;
using ToolbarControl_NS;

namespace SteamGauges
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ToolbarRegistration : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(SteamGauges.MODID, SteamGauges.MODNAME);
            for (int i = 1; i < 12; i++)
                ToolbarControl.RegisterMod(SteamGauges.MODID, SteamGauges.MODNAME + i);
        }
    }
}