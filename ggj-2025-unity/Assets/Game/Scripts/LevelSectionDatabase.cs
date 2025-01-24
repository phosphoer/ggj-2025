using UnityEngine;

[CreateAssetMenu(fileName = "LevelSectionDatabase", menuName = "Scriptable Objects/LevelSectionDatabase")]
public class LevelSectionDatabase : ScriptableObject
{
	[SerializeField]
	private LevelSection[] _levelSections = null;
	public LevelSection[] LevelSections => _levelSections;
}