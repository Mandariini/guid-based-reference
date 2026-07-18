using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(GuidComponent))]
public class GuidComponentDrawer : Editor
{
    private GuidComponent guidComp;

    public override void OnInspectorGUI()
    {
        if (guidComp == null)
        {
            guidComp = (GuidComponent)target;
        }

        if (guidComp == null)
        {
            return;
        }

        if (PrefabUtility.IsPartOfPrefabAsset(guidComp.gameObject) || PrefabStageUtility.GetPrefabStage(guidComp.gameObject) != null)
        {
            EditorGUILayout.LabelField("Guid:", "<Prefab Asset: intentionally empty>");
            EditorGUILayout.HelpBox("Prefab assets won't store GUIDs. A unique GUID is generated per scene instance.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Guid:", guidComp.GetGuid().ToString());

        if (PrefabUtility.IsPartOfNonAssetPrefabInstance(guidComp.gameObject))
        {
            EditorGUILayout.HelpBox("This GUID is instance-specific and stays as a prefab override. Won't be applied.", MessageType.None);
        }
    }
}