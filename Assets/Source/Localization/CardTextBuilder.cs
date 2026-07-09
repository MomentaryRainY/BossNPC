using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardTextBuilder
{
    public static string GetName(CardData card)
    {
        return LocalizationManager.Instance.GetText(card.CardNameKey);
    }

    public static string GetDescription(CardData card)
    {
        string text = LocalizationManager.Instance.GetText(card.DescriptionKey);

        text = text.Replace("{range}", GetRange(card).ToString());
        text = text.Replace("{damage}", GetDamage(card).ToString());

        return text;
    }

    private static string GetDescriptionTemplate(CardData card)
    {
        return LocalizationManager.Instance.GetText(card.DescriptionKey);
    }

    public static string GetDescription(CardData card, CardDescriptionPreview preview)
    {
        string text = GetDescriptionTemplate(card);

        text = text.Replace("{range}", GetRange(card).ToString());
        text = text.Replace("{damage}", preview.Damage.ToString());

        return text;
    }

    private static int GetRange(CardData card)
    {
        if (card == null || card.TargetingRule == null)
        {
            return 0;
        }

        if(card.TargetingRule is RangeTargetRule rangeRule)
        {
            return rangeRule.MaxRangeValue;
        }

        if (card.TargetingRule is LinearRangeTargetRule LinearRangeRule)
        {
            return LinearRangeRule.MaxRangeValue;
        }

        if(card.TargetingRule is IntervalRangeTargetRule IntervalRangeRule)
        {
            return IntervalRangeRule.IntervalValue;
        }

        return 0;
    }

    private static int GetDamage(CardData card)
    {
        if (card == null || card.Effects == null)
        {
            return 0;
        }

        foreach (CardEffect effect in card.Effects)
        {
            if (effect is DamageEffect damageEffect)
            {
                return Mathf.RoundToInt(damageEffect.CardDamage);
            } else if (effect is LinearDamageEffect lineDamageEffect)
            {
                return Mathf.RoundToInt(lineDamageEffect.CardDamage);
            }
            else if (effect is RectangleDamageEffect rectangleDamageEffect)
            {
                return Mathf.RoundToInt(rectangleDamageEffect.CardDamage);
            }
            else if (effect is SquareDamageEffect squareDamageEffect)
            {
                return Mathf.RoundToInt(squareDamageEffect.CardDamage);
            }
        }

        return 0;
    }
}
