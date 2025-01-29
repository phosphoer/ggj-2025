using Rewired.Glyphs.UnityUI;
using UnityEngine;

public class ChewUI : MonoBehaviour
{
  [SerializeField] private UnityUIPlayerControllerElementGlyph _glyphHint = null;
  [SerializeField] private TMPro.TMP_Text _chewText = null;
  [SerializeField] private string _chewStartString = "Chew";
  [SerializeField] private string _chewMoreString = "Chew More!";

  public void SetPlayerInput(Rewired.Player playerInput)
  {
    _glyphHint.playerId = playerInput.id;
  }

  public void SetChewedState()
  {
    _chewText.text = _chewMoreString;
  }

  private void Start()
  {
    _chewText.text = _chewStartString;
  }
}