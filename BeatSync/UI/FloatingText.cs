using BeatSync.Utilities;
using CustomUI.BeatSaber;
using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace BeatSync.UI
{
    public class FloatingText : MonoBehaviour
    {
        public static TMP_FontAsset TMP_Font;
        public Canvas Canvas { get; set; }
        private float _width;
        public float Width
        {
            get { return _width; }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    var currentPosition = gameObject.transform.localPosition;
                    currentPosition.x = 0;
                    gameObject.transform.localPosition = currentPosition + GetAlignmentOffset(TextAlignment);
                }
            }
        }
        private const string FontName = "Teko-Medium SDF No Glow";
        private TextMeshProUGUI _textMesh;
        //public TextMeshProUGUI TextMesh { get { return _textMesh; } private set { _textMesh = value; } }
        //private TextMeshPro textMesh;
        private int _characterLimit = 0;
        private TextAlignmentOptions _textAlignment = TextAlignmentOptions.Center;
        public TextAlignmentOptions TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
                    if (_textMesh != null)
                    {
                        _textMesh.alignment = value;
                    }
                    transform.localPosition = Position;
                }
            }
        }

        private FontStyles _fontStyle = FontStyles.Normal;
        public FontStyles FontStyle
        {
            get { return _fontStyle; }
            set
            {
                if (_fontStyle != value)
                {
                    _fontStyle = value;
                    if (_textMesh != null)
                    {
                        _textMesh.fontStyle = value;
                    }
                }
            }
        }

        private Color _fontColor = Color.white;
        public Color FontColor
        {
            get { return _fontColor; }
            set
            {
                if (_fontColor != value)
                {
                    _fontColor = value;
                    if (_textMesh != null)
                    {
                        _textMesh.color = value;
                    }
                }
            }
        }

        private Vector3 _position = new Vector3(0, 0, 0);
        public Vector3 Position
        {
            get
            {
                return _position;// + GetAlignmentOffset(TextAlignment);
            }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    if (_textMesh != null)
                        _textMesh.transform.localPosition = _position + GetAlignmentOffset(TextAlignment);

                }
            }
        }

        private float _fontSize = .1f;
        public float FontSize
        {
            get
            {
                return _fontSize;
            }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    if (_textMesh != null)
                        _textMesh.fontSize = _fontSize;
                }
            }
        }

        private Vector3 GetAlignmentOffset(TextAlignmentOptions textAlignment)
        {
            Vector3 offset;
            switch (TextAlignment)
            {
                case TextAlignmentOptions.Left:
                    offset = new Vector3(-Width / 2, 0, 0f);
                    break;
                case TextAlignmentOptions.Right:
                    offset = new Vector3(Width / 2, 0, 0f);
                    break;
                case TextAlignmentOptions.Center:
                    offset = new Vector3(0, 0, 0f);
                    break;
                default:
                    offset = new Vector3(0, 0, 0f);
                    break;
            }
            return offset;
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
            get { return _displayedText ?? " "; }
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
                    if (_textMesh != null)
                    {
                        Logger.log?.Info($"Setting textMesh to {_displayedText}, alignment: {_textMesh.alignment.ToString()}");
                        _textMesh.text = value;
                        //WriteThings();
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

        private IEnumerator<WaitForSeconds> WaitForCanvas()
        {
            Logger.log?.Info($"{gameObject.name}:Waiting for Canvas");
            var pollRate = new WaitForSeconds(1f);
            while (Canvas == null)
            {
                yield return pollRate;
            }
            Logger.log?.Info($"{gameObject.name}:Waiting for Font");
            StartCoroutine(Util.WaitForResource<TMP_FontAsset>(FontName, font =>
            {
                TMP_Font = UnityEngine.Object.Instantiate<TMP_FontAsset>(font);
                CreateText();
            }));
        }

        public void Start()
        {
            StartCoroutine(WaitForCanvas());
        }

        public IEnumerator<WaitForSeconds> ChangeText()
        {
            yield return new WaitForSeconds(5);
            DisplayedText = "Changed! Changed! Changed! Changed! Changed! Changed!";
            TextAlignment = TextAlignmentOptions.Center;
        }

        public void WriteThings()
        {
            return;
            _textMesh.ForceMeshUpdate();
            
            Logger.log?.Warn($"{gameObject.name} Text: {_displayedText}\n  TransformPos: {_textMesh.transform.position}\n  LocalPos: {_textMesh.transform.localPosition}\n  RT_anchored: {_textMesh.rectTransform.anchoredPosition}");
            Logger.log?.Warn($"  TextBounds: {_textMesh.textBounds.ToString()}\n  Bounds: {_textMesh.bounds.ToString()}");
            Logger.log?.Warn($" Canvas:\n  TransformPos: {Canvas.transform.position}\n  LocalPos: {Canvas.transform.localPosition}");
            Logger.log?.Critical($"  minWidth = {_textMesh.minWidth}, preferredWidth = {_textMesh.preferredWidth}");

        }

        public void CreateText()
        {
            Logger.log?.Info("Font found, creating text.");
            _textMesh = BeatSaberUI.CreateText(Canvas.transform as RectTransform, DisplayedText, new Vector2(0f, 0f), new Vector2(0f, 0f));
            _textMesh.text = DisplayedText;
            _textMesh.fontSize = FontSize;
            _textMesh.alignment = TextAlignment;
            _textMesh.transform.localPosition = Position + GetAlignmentOffset(TextAlignment);
            _textMesh.fontStyle = FontStyle;
            _textMesh.color = FontColor;
            //_textMesh.rectTransform.anchoredPosition = (Canvas.transform as RectTransform).anchoredPosition;
            Logger.log?.Info($"FloatingText {this.gameObject.name ?? ""} created.");
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
