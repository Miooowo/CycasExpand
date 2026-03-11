using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CycasExpand.src.Patches;

/// <summary>
/// 1. 灾厄效果改为在敌方回合开始后结算（在 Hook.AfterSideTurnStart 中、即中毒等效果之后）。
/// 2. 灾厄击杀瀑布巨兽时视为“直接消失”，不进入二阶段自爆（通过标记 + SteamEruptionPower 补丁实现）。
/// </summary>
public static class DoomTimingAndWaterfallPatch
{
	/// <summary>
	/// 当前正在被灾厄击杀的生物（用于让 SteamEruptionPower 不触发二阶段）。
	/// </summary>
	internal static readonly HashSet<Creature> CreaturesBeingKilledByDoom = new HashSet<Creature>();
}

[HarmonyPatch(typeof(DoomPower), nameof(DoomPower.BeforeTurnEnd))]
public static class DoomPower_BeforeTurnEnd_Patch
{
	/// <summary>
	/// 禁用原逻辑：灾厄不再在敌方回合结束时结算，改为在回合开始时结算。
	/// 跳过时必须返回 Task.CompletedTask，否则 Hook.BeforeTurnEnd 会收到 null 导致 WhenAny 报错。
	/// </summary>
	[HarmonyPrefix]
	public static bool Prefix([HarmonyArgument(1)] CombatSide side, ref Task __result)
	{
		if (side == CombatSide.Enemy)
		{
			__result = Task.CompletedTask;
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
public static class Hook_AfterSideTurnStart_DoomPatch
{
	/// <summary>
	/// 灾厄在敌方回合开始、AfterSideTurnStart 全部执行完后结算（即中毒等之后）。
	/// 将原方法的 Task 替换为“先 await 原逻辑再执行灾厄”的 Task，避免主线程阻塞导致卡死。
	/// </summary>
	[HarmonyPostfix]
	public static void Postfix(CombatState combatState, CombatSide side, ref Task __result)
	{
		if (side != CombatSide.Enemy)
			return;
		Task original = __result;
		__result = AwaitOriginalThenRunDoom(original, combatState);
	}

	private static async Task AwaitOriginalThenRunDoom(Task original, CombatState combatState)
	{
		await original;
		await RunDoomAtEnemyTurnStartAsync(combatState);
	}

	private static async Task RunDoomAtEnemyTurnStartAsync(CombatState combatState)
	{
		IReadOnlyList<Creature> enemies = combatState.GetCreaturesOnSide(CombatSide.Enemy);
		IReadOnlyList<Creature> doomed = DoomPower.GetDoomedCreatures(enemies);
		if (doomed.Count == 0)
			return;

		foreach (Creature c in doomed)
			DoomTimingAndWaterfallPatch.CreaturesBeingKilledByDoom.Add(c);

		try
		{
			await DoomPower.DoomKill(doomed);
		}
		finally
		{
			DoomTimingAndWaterfallPatch.CreaturesBeingKilledByDoom.Clear();
		}
	}
}

[HarmonyPatch(typeof(SteamEruptionPower), nameof(SteamEruptionPower.AfterDeath))]
public static class SteamEruptionPower_AfterDeath_Patch
{
	/// <summary>
	/// 若该次死亡是灾厄击杀，则不触发二阶段（不调用 TriggerAboutToBlowState）。
	/// </summary>
	[HarmonyPrefix]
	public static bool Prefix(Creature creature)
	{
		if (creature != null && DoomTimingAndWaterfallPatch.CreaturesBeingKilledByDoom.Contains(creature))
		{
			DoomTimingAndWaterfallPatch.CreaturesBeingKilledByDoom.Remove(creature);
			return false; // 跳过原方法，不进入二阶段
		}
		return true;
	}
}

[HarmonyPatch(typeof(SteamEruptionPower), nameof(SteamEruptionPower.ShouldCreatureBeRemovedFromCombatAfterDeath))]
public static class SteamEruptionPower_ShouldCreatureBeRemovedFromCombatAfterDeath_Patch
{
	/// <summary>
	/// 灾厄击杀时，瀑布巨兽应从战斗中移除（直接死亡），而不是进入二阶段。
	/// </summary>
	[HarmonyPrefix]
	public static bool Prefix(Creature creature, ref bool __result)
	{
		if (creature != null && DoomTimingAndWaterfallPatch.CreaturesBeingKilledByDoom.Contains(creature))
		{
			__result = true;
			return false; // 跳过原方法
		}
		return true;
	}
}

[HarmonyPatch(typeof(WaterfallGiant), nameof(WaterfallGiant.ShouldDisappearFromDoom), MethodType.Getter)]
public static class WaterfallGiant_ShouldDisappearFromDoom_Patch
{
	/// <summary>
	/// 灾厄击杀时视为“直接消失”，以便播放正确的死亡动画。
	/// </summary>
	[HarmonyPrefix]
	public static bool Prefix(WaterfallGiant __instance, ref bool __result)
	{
		if (__instance?.Creature != null && DoomTimingAndWaterfallPatch.CreaturesBeingKilledByDoom.Contains(__instance.Creature))
		{
			__result = true;
			return false;
		}
		return true;
	}
}
