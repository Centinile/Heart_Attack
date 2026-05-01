#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingData), true)] // true = applies to subclasses
public class BuildingDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        BuildingData data = (BuildingData)target;

        // --- Header banner ---
        GUIStyle bannerStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.backgroundColor = GetTierColor(data.Tier);
        GUILayout.Box($"{data.StructureName}  |  {data.GetStructureType()}  |  Tier {(int)data.Tier}", bannerStyle, GUILayout.ExpandWidth(true));
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        // --- Base Settings ---
        DrawSection("Base Settings", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("structureName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHP"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tier"));
        });

        // --- Economy ---
        DrawSection("Economy", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nutrientCost"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hydrationCost"));
        });

        // --- Upgrade ---
        DrawSection("Upgrade", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nextLevelData"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("upgradeCost"));
        });

        // --- Visual ---
        DrawSection("Visual", () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"));
        });

        // --- Research-specific tier unlock checkboxes ---
        if (data is ResearchData)
        {
            DrawSection("Tier Unlocks", () =>
            {
                EditorGUILayout.HelpBox("Only one checkbox should be active. The highest checked tier wins.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unlocksTier2"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unlocksTier3"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unlocksTier4"));
            });
        }

        // --- Draw any remaining subclass fields not already drawn ---
        DrawRemainingFields();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSection(string title, System.Action content)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11
        };

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, headerStyle);
        EditorGUI.indentLevel++;
        content();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);
    }

    private void DrawRemainingFields()
    {
        // Fields already drawn manually — skip these
        var skipFields = new System.Collections.Generic.HashSet<string>
        {
            "m_Script", "structureName", "maxHP", "tier",
            "nutrientCost", "hydrationCost", "nextLevelData", "upgradeCost",
            "icon", "prefab", "unlocksTier2", "unlocksTier3", "unlocksTier4"
        };

        SerializedProperty prop = serializedObject.GetIterator();
        prop.NextVisible(true);
        while (prop.NextVisible(false))
        {
            if (!skipFields.Contains(prop.name))
                EditorGUILayout.PropertyField(prop, true);
        }
    }

    private Color GetTierColor(BuildingTier tier) => tier switch
    {
        BuildingTier.Tier1 => new Color(0.3f, 0.5f, 0.3f),
        BuildingTier.Tier2 => new Color(0.2f, 0.4f, 0.7f),
        BuildingTier.Tier3 => new Color(0.6f, 0.3f, 0.6f),
        BuildingTier.Tier4 => new Color(0.7f, 0.4f, 0.1f),
        _ => Color.gray
    };
}
#endif