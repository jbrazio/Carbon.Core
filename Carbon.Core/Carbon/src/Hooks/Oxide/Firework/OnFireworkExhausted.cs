﻿///
/// Copyright (c) 2022 Carbon Community 
/// All rights reserved
/// 

using Carbon.Core;

namespace Carbon.Hooks
{
	[OxideHook("OnFireworkExhausted"), OxideHook.Category(Hook.Category.Enum.Firework)]
	[OxideHook.Parameter("this", typeof(BaseFirework))]
	[OxideHook.Info("Called when the firework is over")]
	[OxideHook.Patch(typeof(BaseFirework), "OnExhausted")]
	public class BaseFirework_OnExhausted
	{
		public static void Postfix(ref BaseFirework __instance)
		{
			HookCaller.CallStaticHook("OnFireworkExhausted", __instance);
		}
	}
}
