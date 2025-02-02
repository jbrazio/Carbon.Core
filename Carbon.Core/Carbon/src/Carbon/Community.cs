﻿///
/// Copyright (c) 2022 Carbon Community 
/// All rights reserved
/// 

#if !(WIN || UNIX)
#error Target architecture not defined
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Carbon;
using Carbon.Base.Interfaces;
using Carbon.Core;
using Carbon.Extensions;
using Carbon.Processors;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Plugins;
using UnityEngine;

namespace Carbon
{
	public class Community
	{
		public static string Version { get; set; } = "Unknown";
		public static string InformationalVersion { get; set; } = "Unknown";

		public static bool IsServerFullyInitialized => RelationshipManager.ServerInstance != null;
		public static Community Runtime { get; set; }

		public static bool IsConfigReady => Runtime != null && Runtime.Config != null;

		public const OS OperatingSystem =
#if WIN
			 OS.Win;
#elif UNIX
			 OS.Linux;
#else
#error Target architecture not defined
#endif

		public enum OS
		{
			Win,
			Linux
		}

		public HookProcessor HookProcessor { get; set; }
		public Config Config { get; set; }
		public RustPlugin CorePlugin { get; set; }
		public Loader.CarbonMod Plugins { get; set; }
		public Entities Entities { get; set; }
		public bool IsInitialized { get; set; }

		internal static List<string> _addons = new List<string> { "carbon." };

		#region Config

		public void LoadConfig()
		{
			if (!OsEx.File.Exists(Defines.GetConfigFile()))
			{
				SaveConfig();
				return;
			}

			Config = JsonConvert.DeserializeObject<Config>(OsEx.File.ReadText(Defines.GetConfigFile()));
		}

		public void SaveConfig()
		{
			if (Config == null) Config = new Config();

			OsEx.File.Create(Defines.GetConfigFile(), JsonConvert.SerializeObject(Config, Formatting.Indented));
		}

		#endregion

		#region Commands

		public List<OxideCommand> AllChatCommands { get; } = new List<OxideCommand>();
		public List<OxideCommand> AllConsoleCommands { get; } = new List<OxideCommand>();

		internal void _clearCommands(bool all = false)
		{
			if (all)
			{
				AllChatCommands.Clear();
				AllConsoleCommands.Clear();
			}
			else
			{
				AllChatCommands.RemoveAll(x => !(x.Plugin is IModule) && (x.Plugin is RustPlugin && !(x.Plugin as RustPlugin).IsCorePlugin));
				AllConsoleCommands.RemoveAll(x => !(x.Plugin is IModule) && (x.Plugin is RustPlugin && !(x.Plugin as RustPlugin).IsCorePlugin));
			}
		}
		internal void _installDefaultCommands()
		{
			CorePlugin = new CorePlugin { Name = "Core", IsCorePlugin = true };
			Plugins = new Loader.CarbonMod { Name = "Scripts", IsCoreMod = true };
			CorePlugin.IInit();

			Loader._loadedMods.Add(new Loader.CarbonMod { Name = "Carbon Community", IsCoreMod = true, Plugins = new List<RustPlugin> { CorePlugin } });
			Loader._loadedMods.Add(Plugins);

			Loader.ProcessCommands(typeof(CorePlugin), CorePlugin, prefix: "c");
		}

		#endregion

		#region Processors

		public CarbonProcessor CarbonProcessor { get; set; }
		public ScriptProcessor ScriptProcessor { get; set; }
		public WebScriptProcessor WebScriptProcessor { get; set; }
		public HarmonyProcessor HarmonyProcessor { get; set; }
		public ModuleProcessor ModuleProcessor { get; set; }

		internal void _installProcessors()
		{
			if (ScriptProcessor == null ||
				WebScriptProcessor == null ||
				HarmonyProcessor == null ||
				ModuleProcessor == null ||
				CarbonProcessor)
			{
				_uninstallProcessors();

				var gameObject = new GameObject("Processors");
				ScriptProcessor = gameObject.AddComponent<ScriptProcessor>();
				WebScriptProcessor = gameObject.AddComponent<WebScriptProcessor>();
				HarmonyProcessor = gameObject.AddComponent<HarmonyProcessor>();
				CarbonProcessor = gameObject.AddComponent<CarbonProcessor>();
				HookProcessor = new HookProcessor();
				ModuleProcessor = new ModuleProcessor();
				Entities = new Entities();
			}
			Carbon.Logger.Log("Installed processors");

			_registerProcessors();
		}
		internal void _registerProcessors()
		{
			if (ScriptProcessor != null) ScriptProcessor?.Start();
			if (WebScriptProcessor != null) WebScriptProcessor?.Start();
			if (HarmonyProcessor != null) HarmonyProcessor?.Start();

			if (ScriptProcessor != null) ScriptProcessor.InvokeRepeating(() => { RefreshConsoleInfo(); }, 1f, 1f);
			Carbon.Logger.Log("Registered processors");
		}
		internal void _uninstallProcessors()
		{
			var obj = ScriptProcessor == null ? null : ScriptProcessor.gameObject;

			try
			{
				if (ScriptProcessor != null) ScriptProcessor?.Dispose();
				if (WebScriptProcessor != null) WebScriptProcessor?.Dispose();
				if (HarmonyProcessor != null) HarmonyProcessor?.Dispose();
				if (ModuleProcessor != null) ModuleProcessor?.Dispose();
				if (CarbonProcessor != null) CarbonProcessor?.Dispose();
			}
			catch { }

			try
			{
				if (ScriptProcessor != null) UnityEngine.Object.DestroyImmediate(ScriptProcessor);
				if (WebScriptProcessor != null) UnityEngine.Object.DestroyImmediate(WebScriptProcessor);
				if (HarmonyProcessor != null) UnityEngine.Object.DestroyImmediate(HarmonyProcessor);
				if (CarbonProcessor != null) UnityEngine.Object.DestroyImmediate(CarbonProcessor);
			}
			catch { }

			try
			{
				if (obj != null) UnityEngine.Object.Destroy(obj);
			}
			catch { }
		}

		#endregion

		#region Logging

		public static void LogCommand(object message, BasePlayer player = null)
		{
			if (player == null)
			{
				Carbon.Logger.Log(message);
				return;
			}

			player.SendConsoleCommand($"echo {message}");
		}

		#endregion

		public static void ReloadPlugins()
		{
			Loader.ClearAllRequirees();

			Loader.LoadCarbonMods();
			ScriptLoader.LoadAll();
		}
		public static void ClearPlugins()
		{
			Runtime?._clearCommands();
			Loader.UnloadCarbonMods();
		}

		public void RefreshConsoleInfo()
		{
#if WIN
			if (!IsServerFullyInitialized) return;
			if (ServerConsole.Instance.input.statusText.Length != 4) ServerConsole.Instance.input.statusText = new string[4];

			var version =
#if DEBUG
			InformationalVersion;
#else
					Version;
#endif

			ServerConsole.Instance.input.statusText[3] = $" Carbon v{version}, {Loader._loadedMods.Count:n0} mods, {Loader._loadedMods.Sum(x => x.Plugins.Count):n0} plgs";
#endif
		}

		public void Initialize()
		{
			if (IsInitialized) return;

			#region Handle Versions

			var assembly = typeof(Community).Assembly;

			try { InformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion; } catch { }
			try { Version = assembly.GetName().Version.ToString(); } catch { }

			#endregion

			LoadConfig();

			Carbon.Logger._init();

			Carbon.Logger.Log("Loaded config");

			Carbon.Logger.Log($"Loading...");

			Defines.Initialize();

			_installProcessors();

			Interface.Initialize();

			_clearCommands();
			_installDefaultCommands();

			HookValidator.Refresh();
			Carbon.Logger.Log("Fetched oxide hooks");

			ReloadPlugins();

			Carbon.Logger.Log($"Loaded.");

			RefreshConsoleInfo();

			IsInitialized = true;

			Entities.Init();

			HookProcessor.InstallAlwaysPatchedHooks();
		}
		public void Uninitalize()
		{
			try
			{
				_uninstallProcessors();
				_clearCommands(all: true);

				ClearPlugins();
				Loader._loadedMods.Clear();
				UnityEngine.Debug.Log($"Unloaded Carbon.");

#if WIN
				try
				{
					if (ServerConsole.Instance != null && ServerConsole.Instance.input != null)
					{
						ServerConsole.Instance.input.statusText[3] = "";
						ServerConsole.Instance.input.statusText = new string[3];
					}
				}
				catch { }
#endif

				Entities.Dispose();

				foreach (var hook in HookProcessor.Patches)
				{
					HookProcessor.UninstallHooks(hook.Key, shutdown: true);
				}

				Carbon.Logger._dispose();
			}
			catch (Exception ex)
			{
				Carbon.Logger.Error($"Failed Carbon uninitialization.", ex);
			}
		}
	}
}
