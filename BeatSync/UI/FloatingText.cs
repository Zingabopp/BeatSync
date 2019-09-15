using BeatSync.Utilities;
using CustomUI.BeatSaber;
using TMPro;
using UnityEngine;
using System;

namespace BeatSync.UI
{
    public class FloatingText : MonoBehaviour
    {
        public static TMP_FontAsset TMP_Font;
        private const string FontName = "Teko-Medium SDF No Glow";
        private TextMeshProUGUI textMesh;
        private int _characterLimit = 0;

        /// <summary>
        /// Limit the length of the DisplayedText string by this amount. If 0, do not limit.
        /// </summary>
        public int CharacterLimit
        {
            get { return _characterLimit; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(CharacterLimit), "CharacterLimit cannot be less than 0.");
                if (_characterLimit == value)
                    return;
                _characterLimit = value;
                if (_characterLimit > 0 && DisplayedText.Length > _characterLimit)
                    DisplayedText = DisplayedText.Substring(0, _characterLimit);
            }
        }
        private string _displayedText;
        public string DisplayedText
        {
            get { return _displayedText ?? ""; }
            set
            {
                if (value == null)
                {
                    _displayedText = value;
                    return;
                }
                if (CharacterLimit > 0 && value.Length > CharacterLimit)
                    value = value.Substring(0, CharacterLimit);
                if (_displayedText != value)
                {
                    _displayedText = value;
                    if (textMesh != null)
                        textMesh.text = value;
                }
            }
        }


        public void Awake()
        {

        }

        public void Start()
        {
            StartCoroutine(Util.WaitForResource<TMP_FontAsset>(FontName, font =>
            {
                TMP_Font = UnityEngine.Object.Instantiate<TMP_FontAsset>(font);
                CreateText();
            }));
        }

        public void CreateText()
        {
            Logger.log?.Debug("Font found, creating text.");
            GameObject gameObject = this.gameObject;//new GameObject();
            //UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.transform.position = new Vector3(0f, 0f, 2.5f);
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
            (canvas.transform as RectTransform).sizeDelta = new Vector2(200f, 50f);
            var thing = canvas.transform as RectTransform;
            Logger.log?.Info($"FloatingText AnchorMin: {Vector2ToString(thing.anchorMin)}, AnchorMax: {Vector2ToString(thing.anchorMax)}");
            TextMeshProUGUI textMeshProUGUI = BeatSaberUI.CreateText(canvas.transform as RectTransform, _displayedText, new Vector2(0f, -20f), new Vector2(400f, 20f));
            var thing2 = textMeshProUGUI.rectTransform;
            Logger.log?.Info($"FloatingText AnchorMin: {Vector2ToString(thing2.anchorMin)}, AnchorMax: {Vector2ToString(thing2.anchorMax)}");
            textMeshProUGUI.text = DisplayedText;
            textMeshProUGUI.fontSize = 10f;
            textMeshProUGUI.alignment = TextAlignmentOptions.Center;
            Logger.log?.Debug($"FloatingText {this.gameObject.name ?? ""} created.");
        }

        public string Vector2ToString(Vector2 vector)
        {
            return $"{{{vector.x}, {vector.y}}}";
        }
    }
}
