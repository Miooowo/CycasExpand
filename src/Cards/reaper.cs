using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CycasExpand.src.Cards;

/// <summary>
/// 死亡收割：对所有敌人造成伤害，未被格挡的伤害回复生命。
/// </summary>
public sealed class Reaper : CardModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DamageVar(4m, ValueProp.Move) };

	public Reaper()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		AttackCommand attack = await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.TargetingAllOpponents(base.CombatState)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		int unblockedTotal = attack.Results.Sum(r => r.UnblockedDamage);
		if (unblockedTotal > 0)
			await CreatureCmd.Heal(base.Owner.Creature, unblockedTotal);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(1m);
	}
}
