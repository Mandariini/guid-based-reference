using UnityEngine;

public static class GuidReferenceExtensions
{
  /// <summary>
  /// Lazily resolves a <see cref="GuidReference"/> to a cached component of type <typeparamref name="T"/>.
  /// Returns the cached value immediately on subsequent calls. Logs warnings if the reference is
  /// missing/invalid or if the target GameObject lacks the expected component.
  /// </summary>
  public static T ResolveComponent<T>(this GuidReference guidRef, ref T cache, Object context) where T : Component
  {
    if (cache != null) { return cache; }

    if (guidRef == null || guidRef.gameObject == null)
    {
      Debug.LogWarning($"GuidReference for {typeof(T).Name} is missing or invalid.");
      return null;
    }

    cache = guidRef.gameObject.GetComponent<T>();
    if (cache == null)
    {
      Debug.LogWarning($"GuidReference is assigned but the referenced GameObject does not have a {typeof(T).Name} component.");
    }

    return cache;
  }
}
