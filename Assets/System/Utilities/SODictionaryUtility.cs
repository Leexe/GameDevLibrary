#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Credit to MicWu
public static class SODictionaryUtility
{
	/// <summary>
	///     Generates a dictionary of ScriptableObjects, keyed by their Id property.
	/// </summary>
	public static Dictionary<string, T> GenerateSODict<T>(string searchFolder)
		where T : ScriptableObject
	{
		var dict = new Dictionary<string, T>();

		// First, check if the SO contains an "Id" property. (reflection voodoo)
		PropertyInfo idProperty = typeof(T).GetProperty("Id");
		if (idProperty == null)
		{
			Debug.LogError($"{typeof(T).Name} does not have an 'Id' property. Cannot generate dictionary.");
			return dict;
		}

		// search through specified folder and assemble dictionary
		string typeName = typeof(T).Name;
		string typeFilter = $"t:{typeName}";
		string[] guids = AssetDatabase.FindAssets(typeFilter, new[] { searchFolder });

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			T so = AssetDatabase.LoadAssetAtPath<T>(path);

			if (so != null)
			{
				string id = idProperty.GetValue(so) as string;

				if (!string.IsNullOrEmpty(id))
				{
					if (dict.ContainsKey(id))
					{
						Debug.LogWarning($"Duplicate {typeName} ID '{id}' found in {path}. Skipping.");
						continue;
					}

					dict[id] = so;
				}
			}
		}

		Debug.Log($"Generated {typeName} Dictionary with {dict.Count} entries.");
		return dict;
	}
}
#endif
