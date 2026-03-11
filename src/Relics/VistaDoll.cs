using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace CycasExpand.src.Relics;

/// <summary>
/// 薇斯塔玩偶：商店折扣30%，进入商店时每10金币回复1点生命值，战胜敌人获得的金币翻倍。
/// </summary>
public sealed class VistaDoll : RelicModel
{
	private const string _discountKey = "Discount";
	private const decimal DiscountPercent = 30m;
	private const int GoldPerHealTick = 10;
	private const decimal HealPerTick = 1m;
	private const int CombatGoldMultiplier = 2;

	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar("Discount", DiscountPercent) };

	public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
	{
		if (player != Owner)
			return originalPrice;
		return originalPrice * (1m - DynamicVars["Discount"].BaseValue / 100m);
	}

	public override bool ShouldRefillMerchantEntry(MerchantEntry entry, Player player)
	{
		return player == Owner;
	}

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (Owner?.Creature == null || Owner.Creature.IsDead)
			return;
		if (room is not MerchantRoom)
			return;
		int gold = Owner.Gold;
		decimal healAmount = gold / GoldPerHealTick * HealPerTick;
		if (healAmount > 0m)
		{
			Flash();
			await CreatureCmd.Heal(Owner.Creature, healAmount);
		}
	}

	public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		if (player != Owner)
			return false;
		if (room is not CombatRoom)
			return false;
		var newRewards = new List<Reward>();
		foreach (Reward reward in rewards)
		{
			if (reward is GoldReward goldReward)
				newRewards.Add(new GoldReward(goldReward.Amount * CombatGoldMultiplier, player));
			else
				newRewards.Add(reward);
		}
		rewards.Clear();
		rewards.AddRange(newRewards);
		return true;
	}
}
