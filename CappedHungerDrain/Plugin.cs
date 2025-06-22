using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CappedHungerDrain;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private static ConfigEntry<float> _maxHunger;
    private static ConfigEntry<bool> _isRounded;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loading...");
        
        
        // Load the settings.
        _maxHunger = Config.Bind("Settings", "MaxHunger", 0.33f, "Maximum hunger level the player can starve to. Default is 0.33 (33% of the bar).");
        _isRounded = Config.Bind("Settings", "IsRounded", true, "If true, the hunger level will be rounded to the nearest 0.025. If false, it will be set to the exact value.");
        
        // Apply the patches.
        Harmony.CreateAndPatchAll(typeof(Plugin), MyPluginInfo.PLUGIN_GUID);
        
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded...");
    }
    
    [HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")] // Unfortunate hard-code because private method.
    [HarmonyPostfix]
    public static void Postfix_CharacterAffliction_AddAffliction(CharacterAfflictions __instance)
    {
        // Note: I'm pretty sure the action bar adds up to 1, meaning afflictions are measured in % of the bar occupied.
        // E.g. When hunger is 50% of the bar, it's "0.5".
        
        // Read the player's hunger value.
        float hunger = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger);
        
        // Check if we don't need to do anything.
        if (hunger < _maxHunger.Value) return;
        
        // Calculate the amount to subtract based on the maximum hunger level.
        float hungerToSubtract = __instance.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger) - _maxHunger.Value;
        
        // Check if rounding is enabled.
        if (_isRounded.Value)
        {
            hungerToSubtract = (float) Math.Round(hungerToSubtract / 0.025) * 0.025f;
        }
            
        // Reduce the player's hunger to the maximum hunger level.
        __instance.SubtractStatus(CharacterAfflictions.STATUSTYPE.Hunger, hungerToSubtract);
    }
}