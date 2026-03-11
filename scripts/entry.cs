using CycasExpand.src.Cards;
using CycasExpand.src.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace CycasExpand.scripts;

[ModInitializer("Init")]
public class Entry
{
	public const string ModId = "CycasExpand";
	private static Harmony? _harmony;

	public static void Init()
	{
		_harmony = new Harmony("CycasExpand");
		_harmony.PatchAll();
		Log.Debug("Mod initialized!");
		ModHelper.AddModelToPool(typeof(IroncladCardPool), typeof(Reaper));
		ModHelper.AddModelToPool<SharedRelicPool, VistaDoll>();
		ModHelper.AddModelToPool<SharedRelicPool, wxwDoll>();
		ModHelper.AddModelToPool<SharedRelicPool, BaizealerDoll>();
	}
}
