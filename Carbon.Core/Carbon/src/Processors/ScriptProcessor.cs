﻿///
/// Copyright (c) 2022 Carbon Community 
/// All rights reserved
/// 

using System;
using System.IO;
using System.Linq;
using Carbon.Base;
using Carbon.Core;

namespace Carbon.Processors
{
	public class ScriptProcessor : BaseProcessor
	{
		public override bool EnableWatcher => Community.IsConfigReady ? Community.Runtime.Config.ScriptWatchers : true;
		public override string Folder => Defines.GetPluginsFolder();
		public override string Extension => ".cs";
		public override Type IndexedType => typeof(Script);

		public bool AllPendingScriptsComplete()
		{
			foreach (var instance in InstanceBuffer)
			{
				if (instance.Value is Script script)
				{
					if (script._loader != null && !script._loader.HasFinished) return false;
				}
			}

			return true;
		}
		public bool AllNonRequiresScriptsComplete()
		{
			foreach (var instance in InstanceBuffer)
			{
				if (instance.Value is Script script)
				{
					if (script._loader != null && !script._loader.HasRequires && !script._loader.HasFinished) return false;
				}
			}

			return true;
		}

		public class Script : Instance
		{
			internal ScriptLoader _loader;

			public override Parser Parser => new ScriptParser();

			public override void Dispose()
			{
				try
				{
					_loader?.Clear();
				}
				catch (Exception ex)
				{
					Carbon.Logger.Error($"Error disposing {File}", ex);
				}

				_loader = null;
			}
			public override void Execute()
			{
				try
				{
					_loader = new ScriptLoader();
					_loader.Parser = Parser;
					_loader.File = File;
					_loader.Mod = Community.Runtime.Plugins;
					_loader.Instance = this;
					_loader.Load();
				}
				catch (Exception ex)
				{
					Carbon.Logger.Warn($"Failed processing {Path.GetFileNameWithoutExtension(File)}:\n{ex}");
				}
			}
		}

		public class ScriptParser : Parser
		{
			public override void Process(string input, out string output)
			{
				output = input
					.Replace(".IPlayer", ".AsIPlayer()")
					.Replace("using Harmony;", "using HarmonyLib;")
					.Replace("new HarmonyInstance", "new Harmony");
			}
		}
	}
}
