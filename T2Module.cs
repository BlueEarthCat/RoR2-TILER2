﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Networking;
using static TILER2.MiscUtil;

namespace TILER2 {
    public abstract class T2Module<T>:T2Module where T : T2Module<T> {
        public static T instance {get;private set;}

        protected T2Module() {
            if(instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Module was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class T2Module : AutoConfigContainer {
        public const string LANG_PREFIX_DISABLED = "<color=#FF0000>[DISABLED]</color>";

        internal static void SetupModuleClass() {
            On.RoR2.Run.Start += On_RunStart;
        }

        private static void On_RunStart(On.RoR2.Run.orig_Start orig, Run self) {
            orig(self);
            if(!NetworkServer.active) return;
            var rngGenerator = new Xoroshiro128Plus(self.seed);
            foreach(var module in allModules)
                module.rng = new Xoroshiro128Plus(rngGenerator.nextUlong);
        }

        public static FilingDictionary<T2Module> allModules = new FilingDictionary<T2Module>();

        public bool enabled { get; protected internal set; } = true;

        public readonly string name;

        ///<summary>If true, Module.enabled will be registered as a config entry.</summary>
        public virtual bool managedEnable => false;
        ///<summary>If managedEnable is true, configDescription will be appended to the module's enable/disable config description.</summary>
        public virtual string configDescription => null;
        ///<summary>If managedEnable is true, enabledConfigFlags will be used for the resultant config entry.</summary>
        public virtual AutoConfigFlags enabledConfigFlags => AutoConfigFlags.PreventNetMismatch;
        ///<summary>If managedEnable is true, enabledConfigUpdateEventsFlags will be used for the resultant config entry.</summary>
        public virtual AutoUpdateEventFlags enabledConfigUpdateEventFlags => AutoUpdateEventFlags.InvalidateLanguage;

        protected readonly List<LanguageAPI.LanguageOverlay> languageOverlays = new List<LanguageAPI.LanguageOverlay>();
        protected readonly Dictionary<string, string> genericLanguageTokens = new Dictionary<string, string>();
        protected readonly Dictionary<string, Dictionary<string, string>> specificLanguageTokens = new Dictionary<string, Dictionary<string, string>>();
        public bool languageInstalled { get; private set; } = false;

        ///<summary>A server-only rng instance based on the current run's seed.</summary>
        public Xoroshiro128Plus rng { get; internal set; }

        ///<summary>Contains various information relating to the mod owning this module.</summary>
        public ModInfo modInfo {get; private set;}

        /// <summary>
        /// Implement to handle AutoItemConfig binding and other related actions. With standard base plugin setup, will be performed before SetupAttributes and SetupBehavior.
        /// </summary>
        public virtual void SetupConfig() {
            var moduleConfigName = $"Modules.{name}";
            if(managedEnable)
                Bind(typeof(T2Module).GetProperty(nameof(enabled)), modInfo.mainConfigFile, modInfo.displayName, moduleConfigName, new AutoConfigAttribute(
                    $"{((configDescription != null) ? (configDescription + "\n") : "")}Set to False to disable this module, and as much of its content as can be disabled after initial load. Doing so may cause changes in other modules as well.",
                    enabledConfigFlags), enabledConfigUpdateEventFlags != AutoUpdateEventFlags.None ? new AutoUpdateEventInfoAttribute(enabledConfigUpdateEventFlags) : null);
            BindAll(modInfo.mainConfigFile, modInfo.displayName, moduleConfigName);
            ConfigEntryChanged += (sender, args) => {
                if(args.target.boundProperty.Name == nameof(enabled)) {
                    if((bool)args.newValue == true) {
                        Install();
                    } else {
                        Uninstall();
                        if(languageInstalled) {
                            UninstallLanguage();
                            Language.CCLanguageReload(new ConCommandArgs());
                        }
                    }
                }
                if(enabled && args.flags.HasFlag(AutoUpdateEventFlags.InvalidateLanguage)) {
                    if(languageInstalled)
                        UninstallLanguage();
                    InstallLanguage();
                    Language.CCLanguageReload(new ConCommandArgs());
                }
            };
        }

        ///<summary>
        ///Implement to handle registration with RoR2 catalogs.
        ///</summary>
        public virtual void SetupAttributes() {}

        ///<summary>Third stage of setup. Should be used to apply permanent hooks and other similar things.</summary>
        public virtual void SetupBehavior() {}

        ///<summary>Fourth stage of setup. Will be performed after all catalogs have initialized.</summary>
        public virtual void SetupLate() {}

        ///<summary>Fifth stage of setup. Should be used to perform final, non-permanent hooks and changes.</summary>
        public virtual void Install() {}

        ///<summary>Should undo EVERY change made by Install.</summary>
        public virtual void Uninstall() {}

        ///<summary>Will be called once after initial language setup, and also if/when the module is installed after setup. Automatically loads tokens from the languageTokens dictionary.</summary>
        public virtual void InstallLanguage() {
            languageOverlays.Add(LanguageAPI.AddOverlay(genericLanguageTokens));
            languageOverlays.Add(LanguageAPI.AddOverlay(specificLanguageTokens));
            languageInstalled = true;
        }

        //Will be called if/when the module is uninstalled after setup.
        public virtual void UninstallLanguage() {
            foreach(var overlay in languageOverlays) {
                overlay.Remove();
            }
            languageOverlays.Clear();
            languageInstalled = false;
        }

        /// <summary>
        /// Call to scan your plugin's assembly for classes inheriting directly or indirectly from a specific subtype of Module, initialize all of them, and prepare a list for further setup.
        /// </summary>
        /// <returns>A FilingDictionary containing all instances that this method just initialized.</returns>
        public static FilingDictionary<T> InitAll<T>(ModInfo modInfo) where T:T2Module {
            return InitSome<T>(t => t.IsSubclassOf(typeof(T)), modInfo);
        }

        /// <summary>
        /// Call to scan your plugin's assembly for classes inheriting directly from a specific subtype of Module, initialize all of them, and prepare a list for further setup.
        /// Has special handling for the MyClass : ModuleOrModuleSubclass&lt;MyClass&gt; pattern.
        /// </summary>
        /// <returns>A FilingDictionary containing all instances that this method just initialized.</returns>
        public static FilingDictionary<T> InitDirect<T>(ModInfo modInfo) where T:T2Module {
            return InitSome<T>(t => (t.BaseType.IsGenericType
                ? (t.BaseType.GenericTypeArguments[0] == t && t.BaseType.BaseType == typeof(T))
                : t.BaseType == typeof(T)), modInfo);
        }

        public static FilingDictionary<T> InitSome<T>(Func<Type, bool> extraTypeChecks, ModInfo modInfo) where T:T2Module {
            var f = new FilingDictionary<T>();
            foreach(Type type in Assembly.GetCallingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && (extraTypeChecks?.Invoke(t) ?? true))) {
                var newModule = (T)Activator.CreateInstance(typeof(T));
                newModule.modInfo = modInfo;
                f.Add(newModule);

            }
            return f;
        }
        
        /// <summary>
        /// Call to scan your plugin's assembly for classes inheriting directly from Module, initialize all of them, and prepare a list for further setup.
        /// </summary>
        /// <returns>A FilingDictionary containing all instances that this method just initialized.</returns>
        public static FilingDictionary<T2Module> InitModules(ModInfo modInfo) {
            return InitDirect<T2Module>(modInfo);
        }

        public static void SetupAll_PluginAwake(IEnumerable<T2Module> modulesToSetup) {
            foreach(var module in modulesToSetup) {
                module.SetupConfig();
            }
            foreach(var module in modulesToSetup) {
                module.SetupAttributes();
            }
            foreach(var module in modulesToSetup) {
                module.SetupBehavior();
            }
        }
        public static void SetupAll_PluginStart(IEnumerable<T2Module> modulesToSetup) {
            foreach(var module in modulesToSetup) {
                module.SetupLate();
            }
            foreach(var module in modulesToSetup) {
                if(module.enabled) {
                    module.InstallLanguage();
                    module.Install();
                }
            }
        }

        protected T2Module() {
            name = GetType().Name;
            allModules.Add(this);
        }

        public struct ModInfo {
            public string displayName;
            public string longIdentifier;
            public string shortIdentifier;
            public ConfigFile mainConfigFile;
        }
    }
}