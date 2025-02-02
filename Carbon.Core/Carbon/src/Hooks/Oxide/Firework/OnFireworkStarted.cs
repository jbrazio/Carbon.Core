﻿///
/// Copyright (c) 2022 Carbon Community 
/// All rights reserved
/// 

using Carbon.Core;

namespace Carbon.Hooks
{
	[OxideHook("OnFireworkStarted"), OxideHook.Category(Hook.Category.Enum.Firework)]
	[OxideHook.Parameter("this", typeof(BaseFirework))]
	[OxideHook.Info("Called when the firework starts shooting")]
	[OxideHook.Patch(typeof(BaseFirework), "Begin")]
	public class BaseFirework_Begin
	{
		public static void Postfix(ref BaseFirework __instance)
		{
			HookCaller.CallStaticHook("OnFireworkStarted", __instance);
		}
	}
}
