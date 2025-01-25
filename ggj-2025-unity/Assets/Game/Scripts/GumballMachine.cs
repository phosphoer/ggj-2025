using UnityEngine;

public class GumballMachine : MonoBehaviour, ISlappable
{
  public float SlapWobbleScale = 10;
  public Item[] GumballPrefabs;
  public RangedInt SpawnCountRange = new(2, 5);
  public RangedFloat SpawnForceRange = new(1, 2);
  public float GumballSpawnRadius = 0.5f;

  [SerializeField] private WobbleAnimation _wobble = null;
  [SerializeField] private Transform _spawnRoot = null;

  void ISlappable.ReceiveSlap(Vector3 fromPos)
  {
    Vector3 wobbleAxis = Vector3.Cross(Vector3.up, (transform.position - fromPos).normalized);
    _wobble.StartWobble(wobbleAxis, SlapWobbleScale);

    SpawnGumballs();
  }

  public void SpawnGumballs()
  {
    int spawnCount = SpawnCountRange.RandomValue;
    for (int i = 0; i < spawnCount; ++i)
    {
      Item gumballPrefab = GumballPrefabs[Random.Range(0, GumballPrefabs.Length)];
      Item gumball = Instantiate(gumballPrefab);
      Vector3 spawnOffset = Random.insideUnitSphere * GumballSpawnRadius;
      gumball.transform.position = _spawnRoot.position + spawnOffset;
      gumball.Rigidbody.AddForce(spawnOffset.normalized * SpawnForceRange.RandomValue, ForceMode.VelocityChange);
    }
  }
}