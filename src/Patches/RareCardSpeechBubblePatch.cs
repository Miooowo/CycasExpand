using System.Globalization;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace CycasExpand.src.Patches;

/// <summary>
/// 当玩家打出非打击/防御的卡牌时，在角色头顶显示气泡。
/// 静默猎人显示「……」；故障机器人显示卡牌英文名的 16 进制 ASCII；储君显示卡牌名加「！」；其他角色显示卡牌名。
/// </summary>
[HarmonyPatch(typeof(CombatHistory), "CardPlayStarted")]
public static class CombatHistory_CardPlayStarted_RareCardSpeechPatch
{
	private static readonly HashSet<string> BasicStrikeDefendIds = new(StringComparer.OrdinalIgnoreCase)
	{
		"STRIKE_IRONCLAD", "STRIKE_SILENT", "STRIKE_DEFECT", "STRIKE_REGENT",
		"DEFEND_IRONCLAD", "DEFEND_SILENT", "DEFEND_DEFECT", "DEFEND_REGENT"
	};

	[HarmonyPostfix]
	public static void Postfix(CombatState combatState, CardPlay cardPlay)
	{
		if (TestMode.IsOn)
			return;
		if (BasicStrikeDefendIds.Contains(cardPlay.Card.Id.Entry))
			return;
		if (!cardPlay.IsFirstInSeries)
			return;
		if (cardPlay.Card.Owner?.Creature == null)
			return;
		if (cardPlay.Card.Owner.Creature.IsDead)
			return;
		if (NCombatRoom.Instance?.CombatVfxContainer == null)
			return;

		string characterId = cardPlay.Card.Owner.Character.Id.Entry;
		string bubbleText = GetBubbleText(cardPlay.Card, characterId);
		NSpeechBubbleVfx? bubble = NSpeechBubbleVfx.Create(bubbleText, cardPlay.Card.Owner.Creature, 1.5);
		if (bubble != null)
			NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(bubble);
	}

	private static string GetBubbleText(CardModel card, string characterId)
	{
		switch (characterId?.ToLowerInvariant())
		{
			case "silent":
				return "……";
			case "defect":
				return CardNameToHexAscii(GetEnglishStyleCardName(card));
			case "regent":
				return card.TitleLocString.GetFormattedText() + "！";
			default:
				return card.TitleLocString.GetFormattedText();
		}
	}

	/// <summary>
	/// 用卡牌 Id.Entry 转成类似英文标题的字符串（用于故障机器人十六进制显示）。
	/// </summary>
	private static string GetEnglishStyleCardName(CardModel card)
	{
		string entry = card.Id.Entry;
		if (string.IsNullOrEmpty(entry))
			return entry;
		// 下划线改空格，再转成首字母大写的标题形式
		string withSpaces = entry.Replace('_', ' ');
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(withSpaces.ToLowerInvariant());
	}

	/// <summary>
	/// 将字符串转为 16 进制 ASCII 显示（每 4 字节一组，如 0x54494D45）。
	/// </summary>
	private static string CardNameToHexAscii(string text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;
		byte[] bytes = Encoding.ASCII.GetBytes(text);
		var sb = new StringBuilder();
		for (int i = 0; i < bytes.Length; i += 4)
		{
			if (sb.Length > 0)
				sb.Append(' ');
			uint word = 0;
			for (int j = 0; j < 4 && i + j < bytes.Length; j++)
				word = (word << 8) | bytes[i + j];
			sb.Append("0x").Append(word.ToString("X8"));
		}
		return sb.ToString();
	}
}
