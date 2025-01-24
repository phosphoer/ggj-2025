public class UpdateManager : Singleton<UpdateManager>
{
  public static event System.Action OnUpdate;

  private void Awake()
  {
    Instance = this;
  }

  private void Update()
  {
    OnUpdate?.Invoke();
  }
}