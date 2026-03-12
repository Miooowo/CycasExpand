using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.ValueProps;
using CycasExpand.src.Relics;

namespace CycasExpand.src.Patches;

/// <summary>
/// 为所有事件添加「离开」选项：选择后扣除 3 点生命并结束事件。
/// </summary>
[HarmonyPatch(typeof(EventModel), "GenerateInitialOptionsWrapper")]
public static class EventLeaveOptionPatch
{
	private const string LocTable = "events";
	private const string LeaveTitleKey = "CYCAS_EXPAND_LEAVE.title";
	private const string LeaveDescKey = "CYCAS_EXPAND_LEAVE.description";

	internal static readonly LocString LeaveTitle = new LocString(LocTable, LeaveTitleKey);
	internal static readonly LocString LeaveDescription = new LocString(LocTable, LeaveDescKey);

	private static MethodInfo? _setEventFinishedMethod;

	internal static MethodInfo SetEventFinishedMethod =>
		_setEventFinishedMethod ??= typeof(EventModel).GetMethod(
			"SetEventFinished",
			BindingFlags.NonPublic | BindingFlags.Instance,
			null,
			new[] { typeof(LocString) },
			null)!;

	[HarmonyPostfix]
	public static void Postfix(EventModel __instance, ref IReadOnlyList<EventOption> __result)
	{
		if (!PlayerHasSkipLifeButton(__instance.Owner))
			return;
		AddLeaveOption(__instance, ref __result);
	}

	/// <summary>
	/// 仅当玩家拥有遗物「跳过人生的按钮」时，在事件中显示「离开」选项。
	/// </summary>
	internal static bool PlayerHasSkipLifeButton(Player? player)
	{
		return player?.Relics.Any(r => string.Equals(r.Id.Entry, SkipLifeButton.RelicIdEntry, System.StringComparison.OrdinalIgnoreCase)) == true;
	}

	internal static void AddLeaveOption(EventModel eventModel, ref IReadOnlyList<EventOption> __result)
	{
		var options = __result.ToList();
		var leaveOption = new EventOption(
			eventModel,
			() => LeaveEvent(eventModel),
			LeaveTitle,
			LeaveDescription,
			"CYCAS_EXPAND_LEAVE",
			Enumerable.Empty<IHoverTip>()
		);
		options.Add(leaveOption);
		__result = options;
	}

	private const decimal LeaveHpCost = 3m;

	internal static async Task LeaveEvent(EventModel eventModel)
	{
		if (eventModel.Owner?.Creature != null)
			await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), eventModel.Owner.Creature, LeaveHpCost, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
		SetEventFinishedMethod.Invoke(eventModel, new object[] { LeaveDescription });
	}
}

/// <summary>
/// 先古居民事件重写了 GenerateInitialOptionsWrapper，需单独补丁才能显示「离开」选项。
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
public static class AncientEventLeaveOptionPatch
{
	[HarmonyPostfix]
	public static void Postfix(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
	{
		if (!EventLeaveOptionPatch.PlayerHasSkipLifeButton(__instance.Owner))
			return;
		EventLeaveOptionPatch.AddLeaveOption(__instance, ref __result);
	}
}
