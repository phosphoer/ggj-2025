using UnityEngine;

public class GameGlobals : Singleton<GameGlobals>
{
  private void OnEnable()
  {
    Instance = this;
  }
}