using UnityEngine;

public class InteractPromptUI : MonoBehaviour
{
  public string PromptText
  {
    get => _promptText.text;
    set => _promptText.text = value;
  }

  [SerializeField] private TMPro.TMP_Text _promptText = null;
}