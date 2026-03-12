using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace CycasExpand.src.Relics;

/// <summary>
/// 跳过人生的按钮：拥有此遗物时，在任意事件中可选择「离开」跳过该事件。
/// </summary>
public sealed class SkipLifeButton : RelicModel
{
	/// <summary>
	/// 用于判断玩家是否拥有该遗物（与 Id.Entry 一致）。
	/// </summary>
	public const string RelicIdEntry = "SKIP_LIFE_BUTTON";

	public override RelicRarity Rarity => RelicRarity.Ancient;
}
