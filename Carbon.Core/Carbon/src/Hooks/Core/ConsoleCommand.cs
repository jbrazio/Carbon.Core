﻿///
/// Copyright (c) 2022 Carbon Community 
/// All rights reserved
/// 

using System;
using Carbon;
using Carbon.Core;
using Carbon.Extensions;
using Facepunch.Extend;
using Oxide.Core;

namespace Carbon.Hooks
{
	[CarbonHook.AlwaysPatched]
	[CarbonHook("OnCarbonCommand"), CarbonHook.Category(Hook.Category.Enum.Core)]
	[CarbonHook.Parameter("player", typeof(BasePlayer))]
	[CarbonHook.Parameter("message", typeof(string))]
	[CarbonHook.Info("Called whenever a Carbon server command is called.")]
	[CarbonHook.Patch(typeof(ConsoleSystem), "Run")]
	public class CarbonConsoleCommand
	{
		internal static string[] EmptyArgs = new string[0];

		public static bool Prefix(ConsoleSystem.Option options, string strCommand, object[] args)
		{
			if (Community.Runtime == null) return true;

			try
			{
				var split = strCommand.Split(ConsoleArgEx.CommandSpacing, StringSplitOptions.RemoveEmptyEntries);
				var command = split[0].Trim();
				var args2 = split.Length > 1 ? strCommand.Substring(command.Length + 1).SplitQuotesStrings() : EmptyArgs;
				Facepunch.Pool.Free(ref split);

				foreach (var cmd in Community.Runtime.AllConsoleCommands)
				{
					if (cmd.Command == command)
					{
						try
						{
							cmd.Callback?.Invoke(options.Connection?.player as BasePlayer, command, args2);
						}
						catch (Exception ex)
						{
							Carbon.Logger.Error("ConsoleSystem_Run", ex);
						}

						return false;
					}
				}
			}
			catch { }

			return true;
		}
	}

	[CarbonHook.AlwaysPatched]
	[Hook("OnServerCommand"), Hook.Category(Hook.Category.Enum.Server)]
	[Hook.Parameter("arg", typeof(ConsoleSystem.Arg))]
	[Hook.Info("Useful for intercepting commands before they get to their intended target.")]
	[Hook.Patch(typeof(ConsoleSystem), "Internal", true)]
	public class ServerConsoleCommand
	{
		public static bool Prefix(ConsoleSystem.Arg arg)
		{
			if (arg.Invalid)
			{
				return false;
			}

			return Interface.CallHook("OnServerCommand", arg) == null;
		}
	}
}
