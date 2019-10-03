using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace BeatSync.UI
{
    public class PostText
    {
        
        private FloatingText _floatingText;
        public FloatingText FloatingText
        {
            private get { return _floatingText; }
            set
            {
                HMMainThreadDispatcher.instance.Enqueue(() =>
                {
                    if (_floatingText != value)
                    {
                        _floatingText = value;
                        _floatingText.TextAlignment = TextAlignment;
                        _floatingText.FontStyle = FontStyle;
                        _floatingText.FontColor = FontColor;
                        _floatingText.FontSize = FontSize;
                        _floatingText.CharacterLimit = CharacterLimit;
                        _floatingText.DisplayedText = DisplayedText;
                    }
                });
            }
        }

        public int PostId { get; set; }
        public bool Pinned { get; set; }

        private TextAlignment _textAlignment = TextAlignment.Left;
        public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
                    HMMainThreadDispatcher.instance.Enqueue(() =>
                    {
                        if (_floatingText != null)
                        {
                            _floatingText.TextAlignment = value;
                        }
                    });
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
                    HMMainThreadDispatcher.instance.Enqueue(() =>
                    {
                        if (_floatingText != null)
                        {
                            _floatingText.FontStyle = value;
                        }
                    });
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
                    HMMainThreadDispatcher.instance.Enqueue(() =>
                    {
                        if (_floatingText != null)
                        {
                            _floatingText.FontColor = value;
                        }
                    });
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
                    HMMainThreadDispatcher.instance.Enqueue(() =>
                    {
                        if (_floatingText != null)
                            _floatingText.FontSize = _fontSize;
                    });
                }
            }
        }

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
            get { return _displayedText ?? string.Empty; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (CharacterLimit > 0 && value.Length > CharacterLimit)
                        value = value.Substring(0, CharacterLimit);
                }
                if (_displayedText != value)
                {
                    _displayedText = value;
                    HMMainThreadDispatcher.instance.Enqueue(() =>
                    {
                        if (_floatingText != null)
                        {
                            _floatingText.DisplayedText = value;
                        }
                    });
                }
            }
        }

        public void Clear()
        {
            PostId = 0;
            Pinned = false;
            DisplayedText = string.Empty;
            FontColor = Color.white;
            FontStyle = FontStyles.Normal;
        }
    }
}
