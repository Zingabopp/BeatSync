using BeatSync.Utilities;
using CustomUI.BeatSaber;
using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace BeatSync.UI
{
    public class FloatingText : MonoBehaviour
    {
        public static TMP_FontAsset TMP_Font;
        private const string FontName = "Teko-Medium SDF No Glow";
        //private TextMeshProUGUI textMesh;
        private TextMeshPro textMesh;
        private int _characterLimit = 0;
        private TextAlignmentOptions _textAlignment = TextAlignmentOptions.Left;
        public TextAlignmentOptions TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                if(_textAlignment != value)
                {
                    _textAlignment = value;
                    if(textMesh != null)
                    {
                        textMesh.alignment = value;
                    }
                    transform.position = Position;
                }
            }
        }

        private Vector3 Position
        {
            get
            {
                switch (TextAlignment)
                {
                    case TextAlignmentOptions.Left:
                        return new Vector3(10, 0, 1.5f);
                    case TextAlignmentOptions.Right:
                        return new Vector3(-10, 0, 1.5f);
                    case TextAlignmentOptions.Center:
                        return new Vector3(0, 0, 1.5f);
                    default:
                        return new Vector3(0, 0, 1.5f);
                }
            }
        }

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
                    {
                        Logger.log?.Info($"Setting textMesh to {_displayedText}");
                        textMesh.text = value;
                        WriteThings();
                    }
                    else
                        Logger.log?.Info($"textMesh is null when trying to make it {_displayedText}");
                }
            }
        }


        public void Awake()
        {
            Logger.log?.Info("FloatingText awake.");
        }

        public void Start()
        {
            StartCoroutine(Util.WaitForResource<TMP_FontAsset>(FontName, font =>
            {
                TMP_Font = UnityEngine.Object.Instantiate<TMP_FontAsset>(font);
                CreateText();
            }));
        }

        public IEnumerator<WaitForSeconds> ChangeText()
        {
            yield return new WaitForSeconds(5);
            DisplayedText = "Changed! Changed! Changed! Changed! Changed! Changed!";
            TextAlignment = TextAlignmentOptions.Left;
        }

        public void CreateText()
        {
            
            //gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            textMesh = gameObject.AddComponent<TextMeshPro>();
            textMesh.font = TMP_Font;
            //textMesh.text = "Instance 2";
            textMesh.fontSize = 5f;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.enableWordWrapping = true;
            textMesh.renderMode = TextRenderFlags.Render;
            TextAlignment = TextAlignmentOptions.Right;
            gameObject.layer = 5;
            textMesh.text = _displayedText;
            gameObject.transform.position = Position;
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            WriteThings();
            StartCoroutine(ChangeText());
        }

        public void WriteThings()
        {
            textMesh.ForceMeshUpdate();
            Logger.log?.Warn($"TextMesh TransformPos: {textMesh.transform.position}\n  RT_anchored: {textMesh.rectTransform.anchoredPosition}");
            Logger.log?.Warn($"TextMesh Text: {_displayedText}\n  TextBounds: {textMesh.textBounds.ToString()}\n  Bounds: {textMesh.bounds.ToString()}");
        }

        public void CreateTextOld()
        {
            Logger.log?.Debug("Font found, creating text.");
            GameObject gameObject = this.gameObject;//new GameObject();
            //UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.transform.position = new Vector3(0f, 1f, 1.5f);
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0f);
            gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Canvas canvas = gameObject.AddComponent<Canvas>();
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.red);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.Apply();
            
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

        public string Vector3ToString(Vector3 vector)
        {
            return $"{{{vector.x}, {vector.y}, {vector.z}}}";
        }
    }
}
