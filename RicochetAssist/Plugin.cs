using System;
using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using System.IO;
using System.Reflection;

namespace RicochetAssist
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency(PluginConfiguratorController.PLUGIN_GUID)]
    public class Plugin : BaseUnityPlugin
    {
        Harmony harm;
        public void Start()
        {
            Debug.Log("We did dang dun it reddit");
            harm = new Harmony(PluginInfo.GUID);
            harm.PatchAll(typeof(RicochetMorty));
            ConfigManager.Setup();
        }
    }
    public static class ConfigManager
    {
        public static PluginConfigurator config;
        public static BoolField AimAssistEnabled;
        public static BoolField VanillaAimAssistEnabled;
        public static FloatField RicochetFOV;
        public static BoolField TargetCoins;
        
        public static FloatField RicochetTimer;
        public static IntField SharpshooterExtraRicochet;
        public static BoolField RailcannonRicochetEnable;
        public static IntField RailcannonExtraRicochet;

        public static void Setup()
        {
            config = PluginConfigurator.Create(PluginInfo.Name, PluginInfo.GUID);

            new ConfigHeader(config.rootPanel, "<color=red>Does not confict with modded aim assist</color>", 15);
            VanillaAimAssistEnabled = new BoolField(config.rootPanel, "Enable Vanilla Aim Assist", "bool.vanillaaimassist", false);
            AimAssistEnabled = new BoolField(config.rootPanel, "Enable Aim Assist", "bool.aimassist", true);
            RicochetFOV = new FloatField(config.rootPanel, "Ricochet FOV", "float.ricochetfov", 180, 0, 360);
            TargetCoins = new BoolField(config.rootPanel, "Target Coins", "bool.targetcoins", true);
            new ConfigHeader(config.rootPanel, "<color=red>At 0, shots will ricochet while paused!</color>", 15);
            RicochetTimer = new FloatField(config.rootPanel, "Ricochet Timer", "float.ricochettimer", 0.1f, 0, 100);
            SharpshooterExtraRicochet = new IntField(config.rootPanel, "Extra Sharpshooter Ricochet", "int.sharpshooterextraricochet", 5, 0, 100000000);
            RailcannonRicochetEnable = new BoolField(config.rootPanel, "Enable Railcannon Ricochet", "bool.railcannonricochet", false);
            RailcannonExtraRicochet = new IntField(config.rootPanel, "Extra Railcannon Ricochet", "int.railcannonextraricochet", 5, 0, 100000000);

            VanillaAimAssistEnabled.onValueChange += (e) =>
            {
                RicochetMorty.shouldVanillaAimAssist = e.value;
            };

            AimAssistEnabled.onValueChange += (e) =>
            {
                RicochetFOV.hidden = !e.value;
                TargetCoins.hidden = !e.value;
                RicochetMorty.shouldAimAssist = e.value;
            };

            RicochetFOV.onValueChange += (e) =>
            {
                RicochetMorty.ricFOV = e.value;
            };

            TargetCoins.onValueChange += (e) =>
            {
                RicochetMorty.shouldTargetCoin = e.value;
            };

            RicochetTimer.onValueChange += (e) =>
            {
                RicochetMorty.ricTimer = e.value;
            };

            SharpshooterExtraRicochet.onValueChange += (e) =>
            {
                RicochetMorty.ricBounceAmount = e.value;
            };

            RailcannonRicochetEnable.onValueChange += (e) =>
            {
                RailcannonExtraRicochet.hidden = !e.value;
                RicochetMorty.shouldRailBounce = e.value;
            };

            RailcannonExtraRicochet.onValueChange += (e) =>
            {
                RicochetMorty.railBounceAmount = e.value;
            };

            VanillaAimAssistEnabled.TriggerValueChangeEvent();
            AimAssistEnabled.TriggerValueChangeEvent();
            RicochetFOV.TriggerValueChangeEvent();
            TargetCoins.TriggerValueChangeEvent();
            RicochetTimer.TriggerValueChangeEvent();
            SharpshooterExtraRicochet.TriggerValueChangeEvent();
            RailcannonRicochetEnable.TriggerValueChangeEvent();
            RailcannonExtraRicochet.TriggerValueChangeEvent();

            string workingDirectory = Utils.ModDir();
            string iconFilePath = Path.Combine(Path.Combine(workingDirectory, "Data"), "icon.png");
            ConfigManager.config.SetIconWithURL("file://" + iconFilePath);
        }
    }
    public static class Utils
    {
        public static bool WithinFOV(Vector3 main, Vector3 target, float fov)
        {
            float angle = Mathf.Acos(Vector3.Dot(main.normalized, target.normalized));
            float fovInRad = ((fov / 2) % 360) * Mathf.Deg2Rad;
            return (angle <= fovInRad || angle >= 2 * Mathf.PI - fovInRad);
        }
        public static string ModDir()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
