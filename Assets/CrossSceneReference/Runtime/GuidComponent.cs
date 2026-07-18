using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// This component gives a GameObject a stable, non-replicatable Globally Unique IDentifier.
// It can be used to reference a specific instance of an object no matter where it is.
// This can also be used for other systems, such as Save/Load game
[ExecuteInEditMode, DisallowMultipleComponent]
public class GuidComponent : MonoBehaviour, ISerializationCallbackReceiver
{
    // System guid we use for comparison and generation
    System.Guid guid = System.Guid.Empty;

    // Unity's serialization system doesn't know about System.Guid, so we convert to a byte array
    // Fun fact, we tried using strings at first, but that allocated memory and was twice as slow
    [SerializeField]
    private byte[] serializedGuid;


    public bool IsGuidAssigned()
    {
        return guid != System.Guid.Empty;
    }


    // When de-serializing or creating this component, we want to either restore our serialized GUID
    // or create a new one.
    void CreateGuid()
    {
        // if our serialized data is invalid, then we are a new object and need a new GUID
        if (serializedGuid == null || serializedGuid.Length != 16)
        {
#if UNITY_EDITOR
            // if in editor, make sure we aren't a prefab of some kind
            if (IsAssetOnDisk())
            {
                return;
            }
#endif
            // During prefab apply/revert and other editor operations, serialized data can be
            // momentarily cleared even though this instance already has a valid runtime GUID.
            if (guid != System.Guid.Empty)
            {
                serializedGuid = guid.ToByteArray();
#if UNITY_EDITOR
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(this))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                }
#endif
            }
            else
            {
#if UNITY_EDITOR
                Undo.RecordObject(this, "Added GUID");
#endif
                guid = System.Guid.NewGuid();
                serializedGuid = guid.ToByteArray();

#if UNITY_EDITOR
                // If we are creating a new GUID for a prefab instance of a prefab, but we have somehow lost our prefab connection
                // force a save of the modified prefab instance properties
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(this))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                }
#endif
            }
        }
        else if (guid == System.Guid.Empty)
        {
            // otherwise, we should set our system guid to our serialized guid
            guid = new System.Guid(serializedGuid);
        }

        // register with the GUID Manager so that other components can access this
        if (guid != System.Guid.Empty)
        {
            if (!GuidManager.Add(this))
            {
                // if registration fails, we probably have a duplicate or invalid GUID, get us a new one.
                serializedGuid = null;
                guid = System.Guid.Empty;
                CreateGuid();
            }
        }
    }

#if UNITY_EDITOR
    private bool IsAssetOnDisk()
    {
        // 'this' can be a destroyed/invalid Unity object during domain reload;
        // PrefabUtility will throw ArgumentNullException in that case, so guard first.
        if (this == null || gameObject == null)
        {
            return false;
        }

        if (PrefabUtility.IsPartOfPrefabAsset(gameObject) || EditorUtility.IsPersistent(gameObject))
        {
            return true;
        }

        // Objects in preview scenes (prefab mode stage, PrefabUtility.LoadPrefabContents, previews)
        // are not persistent yet, but their contents get saved into a prefab asset, so they must
        // never own a GUID either. Without this, editing a prefab (variant) in prefab mode bakes
        // serializedGuid into the file as prefab instance overrides.
        // Scene instances always live in the main stage, so they are unaffected; momentary clears
        // during apply/revert are recovered from the runtime guid in CreateGuid.
        return gameObject.scene.IsValid() && EditorSceneManager.IsPreviewScene(gameObject.scene);
    }
#endif

    // We cannot allow a GUID to be saved into a prefab, and we need to convert to byte[]
    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        // This lets us detect if we are a prefab instance or a prefab asset.
        // A prefab asset cannot contain a GUID since it would then be duplicated when instanced.
        if (IsAssetOnDisk())
        {
            serializedGuid = null;
            guid = System.Guid.Empty;
        }
        else
#endif
        {
            if (guid != System.Guid.Empty)
            {
                serializedGuid = guid.ToByteArray();
            }
        }
    }

    // On load, we can go head a restore our system guid for later use
    public void OnAfterDeserialize()
    {
        if (serializedGuid != null && serializedGuid.Length == 16)
        {
            guid = new System.Guid(serializedGuid);
        }
    }

    void Awake()
    {
        CreateGuid();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        // similar to on Serialize, but gets called on Copying a Component or Applying a Prefab
        // at a time that lets us detect what we are
        if (IsAssetOnDisk())
        {
            // release any registration made before we knew we are prefab contents
            // (e.g. a prefab variant that had a GUID baked into it as an override)
            if (guid != System.Guid.Empty)
            {
                GuidManager.Remove(guid);
            }
            serializedGuid = null;
            guid = System.Guid.Empty;
        }
        else
#endif
        {
            CreateGuid();
        }
    }

    // Never return an invalid GUID
    public System.Guid GetGuid()
    {
        if (guid == System.Guid.Empty && serializedGuid != null && serializedGuid.Length == 16)
        {
            guid = new System.Guid(serializedGuid);
        }

        return guid;
    }

    // let the manager know we are gone, so other objects no longer find this
    public void OnDestroy()
    {
        GuidManager.Remove(guid);
    }
}
