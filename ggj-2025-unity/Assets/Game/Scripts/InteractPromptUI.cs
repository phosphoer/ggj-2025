using Rewired.Glyphs.UnityUI;
using UnityEngine;

public class InteractPromptUI : MonoBehaviour
{
  public string PromptText
  {
    get => _promptText.text;
    set => _promptText.text = value;
  }

  public UnityUIPlayerControllerElementGlyph GlyphHint => _glyphHint;

  [SerializeField] private TMPro.TMP_Text _promptText = null;
  [SerializeField] private UnityUIPlayerControllerElementGlyph _glyphHint = null;
}