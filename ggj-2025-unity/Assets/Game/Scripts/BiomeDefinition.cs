using UnityEngine;

[CreateAssetMenu(fileName = "new-biome-definition", menuName = "Sub Game/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
  public SkyboxColors SkyboxColors;
  public float FogFar = 1000;
  public float FogNear = 500;
  public SoundBank SfxAmbient;
}