using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using CycasExpand.src.Relics;

namespace CycasExpand.src.Patches;

/// <summary>
/// 在涅奥事件中新增选项：失去 15 血上限，获得遗物「跳过人生的按钮」。
/// </summary>
[HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
public static class NeowSkipLifeOptionPatch
{
	private const string LocTable = "events";
	private const string OptionTitleKey = "CYCAS_NEOW_SKIP_LIFE.title";
	private const string OptionDescKey = "CYCAS_NEOW_SKIP_LIFE.description";

	private static readonly LocString OptionTitle = new LocString(LocTable, OptionTitleKey);
	private static readonly LocString OptionDescription = new LocString(LocTable, OptionDescKey);

	private static PropertyInfo? _customDonePageProp;
	private static MethodInfo? _doneMethod;

	private static PropertyInfo CustomDonePageProp =>
		_customDonePageProp ??= typeof(AncientEventModel).GetProperty(
			"CustomDonePage",
			BindingFlags.NonPublic | BindingFlags.Instance)!;

	private static MethodInfo DoneMethod =>
		_doneMethod ??= typeof(AncientEventModel).GetMethod(
			"Done",
			BindingFlags.NonPublic | BindingFlags.Instance)!;

	[HarmonyPostfix]
	public static void Postfix(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result)
	{
		if (__instance is not Neow neow)
			return;
		if (neow.Owner?.RunState.Modifiers.Count != 0)
			return;

		var options = __result.ToList();
		var relic = ModelDb.Relic<SkipLifeButton>().ToMutable();
		relic.Owner = neow.Owner;

		var skipLifeOption = new EventOption(
			neow,
			() => OnSkipLifeChosen(neow),
			OptionTitle,
			OptionDescription,
			"CYCAS_NEOW_SKIP_LIFE",
			relic.HoverTips
		);
		skipLifeOption.WithRelic(relic);

		options.Add(skipLifeOption);
		__result = options;
	}

	private static async Task OnSkipLifeChosen(Neow neow)
	{
		var owner = neow.Owner;
		if (owner?.Creature == null)
			return;

		await CreatureCmd.LoseMaxHp(new BlockingPlayerChoiceContext(), owner.Creature, 15m, isFromCard: false);

		var skipLifeRelic = ModelDb.Relic<SkipLifeButton>().ToMutable();
		await RelicCmd.Obtain(skipLifeRelic, owner);

		CustomDonePageProp.SetValue(neow, "NEOW.pages.DONE.POSITIVE.description");
		DoneMethod.Invoke(neow, null);
	}
}
