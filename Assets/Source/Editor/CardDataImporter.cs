#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CardCsvImporter
{
    private const string CardCsvPath = "Assets/Data/CSV/cards.csv";
    private const string CardOutputDir = "Assets/Data/Cards";
    private const string EffectOutputDir = "Assets/Data/CardEffects";
    private const string ConditionOutputDir = "Assets/Data/CardConditions";
    private const string TargetingOutputDir = "Assets/Data/CardTargetingRules";
    private const string ContentPrefabDir = "Assets/Prefabs/Card/CardContents";

    [MenuItem("FGR/Import Cards From CSV")]
    public static void Import()
    {
        EnsureFolder(CardOutputDir);
        EnsureFolder(EffectOutputDir);
        EnsureFolder(ConditionOutputDir);
        EnsureFolder(TargetingOutputDir);
        EnsureFolder(ContentPrefabDir);

        List<string[]> rows = CsvUtility.Parse(File.ReadAllText(CardCsvPath));

        for (int i = 1; i < rows.Count; i++)
        {
            string[] columns = rows[i];

            string id = columns[0].Trim();
            string nameKey = columns[1].Trim();
            int cost = int.Parse(columns[2].Trim());
            string descriptionKey = columns[3].Trim();
            string effectsRaw = columns[4].Trim();
            string targetingRaw = columns[5].Trim();
            string conditionsRaw = columns[6].Trim();
            string contentId = columns[7].Trim();

            CardData card = LoadOrCreateCard(id);

            SerializedObject so = new SerializedObject(card);

            GameObject contentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>
                ($"{ContentPrefabDir}/{contentId}.prefab");

            so.FindProperty("CardId").stringValue = id;
            so.FindProperty("CardNameKey").stringValue = nameKey;
            so.FindProperty("BaseCost").intValue = cost;
            so.FindProperty("ContentPrefab").objectReferenceValue = contentPrefab;
            so.FindProperty("DescriptionKey").stringValue = string.IsNullOrWhiteSpace(descriptionKey)
                ? $"card.{id}.desc"
                : descriptionKey;
            so.FindProperty("TargetingRule").objectReferenceValue = ParseTargeting(targetingRaw);

            SetObjectReferenceList(so.FindProperty("Effects"), ParseEffects(effectsRaw));
            SetObjectReferenceList(so.FindProperty("Conditions"), ParseConditions(conditionsRaw));

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(card);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Card CSV import complete.");
    }
    private static void SetObjectReferenceList<T>(SerializedProperty property, List<T> values)
    where T : UnityEngine.Object
    {
        property.ClearArray();

        for (int i = 0; i < values.Count; i++)
        {
            property.InsertArrayElementAtIndex(i);
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static CardData LoadOrCreateCard(string id)
    {
        string path = $"{CardOutputDir}/{id}.asset";
        CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

        if (card == null)
        {
            card = ScriptableObject.CreateInstance<CardData>();
            AssetDatabase.CreateAsset(card, path);
        }

        return card;
    }

    private static CardTargetingRule ParseTargeting(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        string[] parts = raw.Split(':');
        string type = parts[0].Trim();

        if (type == "Range")
        {
            int range = int.Parse(parts[1].Trim());
            return GetOrCreateRangeTargetingRule(range);

        } else if (type == "LinearRange")
        {
            int range = int.Parse(parts[1].Trim());
            return GetOrCreateLinearRangeTargetingRule(range);
        } else if (type == "IntervalRange")
        {
            int range = int.Parse(parts[1].Trim());
            return GetOrCreateIntervalRangeTargetingRule(range);
        }
        Debug.LogWarning($"Unknown targeting rule: {raw}");
        return null;

    }

    private static List<CardEffect> ParseEffects(string raw)
    {
        List<CardEffect> effects = new();

        foreach (string token in raw.Split('|'))
        {
            if (string.IsNullOrWhiteSpace(token)) continue;

            string[] parts = token.Split(':');
            string type = parts[0].Trim();

            if (type == "Damage")
            {
                float damage = float.Parse(parts[1].Trim());
                effects.Add(GetOrCreateDamageEffect(damage));
            } else if (type == "LinearDamage")
            {
                float damage = float.Parse(parts[1].Trim());
                effects.Add(GetOrCreateLinearDamageEffect(damage));
            } else if (type == "RectangleDamage")
            {
                float damage = float.Parse(parts[1].Trim());
                effects.Add(GetOrCreateRectangleDamageEffect(damage));
            } else if (type == "SquareDamage")
            {
                float damage = float.Parse(parts[1].Trim());
                effects.Add(GetOrCreateSquareDamageEffect(damage));
            }
            else
            {
                Debug.LogWarning($"Unknown effect: {token}");
            }
        }

        return effects;
    }

    private static List<CardCondition> ParseConditions(string raw)
    {
        List<CardCondition> conditions = new();

        foreach (string token in raw.Split('|'))
        {
            if (string.IsNullOrWhiteSpace(token)) continue;

            string[] parts = token.Split(':');
            string type = parts[0].Trim();

            if (type == "EnemyTarget")
            {
                conditions.Add(GetOrCreateEnemyTargetCondition());
            }
            else
            {
                Debug.LogWarning($"Unknown condition: {token}");
            }
        }

        return conditions;
    }

    private static DamageEffect GetOrCreateDamageEffect(float damage)
    {
        string path = $"{EffectOutputDir}/Damage_{damage}.asset";
        DamageEffect effect = AssetDatabase.LoadAssetAtPath<DamageEffect>(path);

        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<DamageEffect>();
            AssetDatabase.CreateAsset(effect, path);
        }

        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("Damage").floatValue = damage;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(effect);
        return effect;
    }

    private static EnemyTargetCondition GetOrCreateEnemyTargetCondition()
    {
        string path = $"{ConditionOutputDir}/EnemyTarget.asset";
        EnemyTargetCondition condition = AssetDatabase.LoadAssetAtPath<EnemyTargetCondition>(path);

        if (condition == null)
        {
            condition = ScriptableObject.CreateInstance<EnemyTargetCondition>();
            AssetDatabase.CreateAsset(condition, path);
        }

        return condition;
    }

    private static RangeTargetRule GetOrCreateRangeTargetingRule(int range)
    {
        string path = $"{TargetingOutputDir}/Range_{range}.asset";
        RangeTargetRule rule = AssetDatabase.LoadAssetAtPath<RangeTargetRule>(path);

        if (rule == null)
        {
            rule = ScriptableObject.CreateInstance<RangeTargetRule>();
            AssetDatabase.CreateAsset(rule, path);
        }

        SerializedObject so = new SerializedObject(rule);
        so.FindProperty("MaxRange").intValue = range;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(rule);
        return rule;
    }

    private static LinearRangeTargetRule GetOrCreateLinearRangeTargetingRule(int range)
    {
        string path = $"{TargetingOutputDir}/LinearRange_{range}.asset";
        LinearRangeTargetRule rule = AssetDatabase.LoadAssetAtPath<LinearRangeTargetRule>(path);

        if (rule == null)
        {
            rule = ScriptableObject.CreateInstance<LinearRangeTargetRule>();
            AssetDatabase.CreateAsset(rule, path);
        }

        SerializedObject so = new SerializedObject(rule);
        so.FindProperty("MaxRange").intValue = range;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(rule);
        return rule;
    }

    private static LinearDamageEffect GetOrCreateLinearDamageEffect(float damage)
    {
        string path = $"{EffectOutputDir}/LinearDamage_{damage}.asset";
        LinearDamageEffect effect = AssetDatabase.LoadAssetAtPath<LinearDamageEffect>(path);

        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<LinearDamageEffect>();
            AssetDatabase.CreateAsset(effect, path);
        }

        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("Damage").floatValue = damage;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(effect);
        return effect;
    }

    private static IntervalRangeTargetRule GetOrCreateIntervalRangeTargetingRule(int range)
    {
        string path = $"{TargetingOutputDir}/IntervalRange_{range}.asset";
        IntervalRangeTargetRule rule = AssetDatabase.LoadAssetAtPath<IntervalRangeTargetRule>(path);

        if (rule == null)
        {
            rule = ScriptableObject.CreateInstance<IntervalRangeTargetRule>();
            AssetDatabase.CreateAsset(rule, path);
        }

        SerializedObject so = new SerializedObject(rule);
        so.FindProperty("Interval").intValue = range;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(rule);
        return rule;
    }

    private static SquareDamageEffect GetOrCreateSquareDamageEffect(float damage)
    {
        string path = $"{EffectOutputDir}/SquareDamage_{damage}.asset";
        SquareDamageEffect effect = AssetDatabase.LoadAssetAtPath<SquareDamageEffect>(path);

        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<SquareDamageEffect>();
            AssetDatabase.CreateAsset(effect, path);
        }

        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("Damage").floatValue = damage;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(effect);
        return effect;
    }

    private static RectangleDamageEffect GetOrCreateRectangleDamageEffect(float damage)
    {
        string path = $"{EffectOutputDir}/RectangleDamage_{damage}.asset";
        RectangleDamageEffect effect = AssetDatabase.LoadAssetAtPath<RectangleDamageEffect>(path);

        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<RectangleDamageEffect>();
            AssetDatabase.CreateAsset(effect, path);
        }

        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("Damage").floatValue = damage;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(effect);
        return effect;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        string name = Path.GetFileName(folder);

        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif