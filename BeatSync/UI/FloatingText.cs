using BeatSync.Utilities;
using CustomUI.BeatSaber;
using TMPro;
using UnityEngine;

namespace BeatSync.UI
{
    public class FloatingText : MonoBehaviour
    {
        public static TMP_FontAsset TMP_Font;
        private const string FontName = "Teko-Medium SDF No Glow";
        private TextMeshProUGUI textMesh;

        private string _displayedText;
        public string DisplayedText
        {
            get { return _displayedText ?? ""; }
            set
            {
                if(_displayedText != value)
                {
                    _displayedText = value;
                    if (textMesh != null)
                        textMesh.text = value;
                }
            }
        }


        public void Awake()
        {
            StartCoroutine(Util.WaitForResource<TMP_FontAsset>(FontName, font =>
            {
                TMP_Font = UnityEngine.Object.Instantiate<TMP_FontAsset>(font);
                CreateText();
            }));
        }

        public void Start()
        {

        }

        public void CreateText()
        {
            Logger.log?.Debug("Font found, creating text.");
            GameObject gameObject = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.transform.position = new Vector3(0f, 0f, 2.5f);
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
            (canvas.transform as RectTransform).sizeDelta = new Vector2(200f, 50f);
            TextMeshProUGUI textMeshProUGUI = BeatSaberUI.CreateText(canvas.transform as RectTransform, _displayedText, new Vector2(0f, -20f), new Vector2(400f, 20f));
            textMeshProUGUI.text = _displayedText;
            textMeshProUGUI.fontSize = 10f;
            textMeshProUGUI.alignment = TextAlignmentOptions.Center;
            Logger.log?.Debug("Text created.");
        }


    }
}
