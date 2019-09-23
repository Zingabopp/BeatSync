using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSync.UI
{
    public class TextMeshList : MonoBehaviour
    {
        #region Properties

        public Canvas Canvas { get; set; }
        public int NumTexts { get; private set; }
        private float _width;
        public float Width
        {
            get { return _width; }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    if (HeaderText != null)
                        HeaderText.Width = _width;
                    for (int i = 0; i < gameObject.transform.childCount; i++)
                    {
                        var child = gameObject.transform.GetChild(i)?.GetComponent<FloatingText>();
                        if (child != null)
                            child.Width = _width;
                    }
                }
            }
        }
        private int _next = 0;
        private int Next
        {
            get { return _next; }
            set
            {
                while (value < 0)
                    value = value + NumTexts;
                while (value >= NumTexts)
                    value = value - NumTexts;
                _next = value;
            }
        }

        private int Last
        {
            get
            {
                var last = Next - 1;
                while (last < 0)
                    last = last + NumTexts;
                return last;
            }
        }
        private PostText[] _postTexts;
        private PostText[] PostTexts
        {
            get { return _postTexts; }
            set { _postTexts = value; }
        }

        public float RowSpacing { get; set; }

        private FloatingText HeaderText { get; set; }

        private string _fullHeader;
        private string FullHeader
        {
            get { return _fullHeader ?? string.Empty; }
            set
            {
                if (_fullHeader != value)
                {
                    _fullHeader = value;
                    if (HeaderText != null)
                        HeaderText.DisplayedText = _fullHeader;
                }
            }
        }

        private string _header;
        public string Header
        {
            get { return _header ?? string.Empty; }
            set
            {
                if (_header != value)
                {
                    _header = value;
                    FullHeader = $"{_header}{(string.IsNullOrEmpty(_subHeader) ? "" : $" ({_subHeader})")}";
                }
            }
        }

        private string _subHeader;
        public string SubHeader
        {
            get { return _subHeader; }
            set
            {
                if (_subHeader != value)
                {
                    _subHeader = value;
                    FullHeader = $"{_header}{(string.IsNullOrEmpty(_subHeader) ? "" : $" ({_subHeader})")}";
                }
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numTexts"></param>
        /// <exception cref="ArgumentException">Thrown when numTexts < 1.</exception>
        public void Initialize(int numTexts)
        {
            if (numTexts < 1)
                throw new ArgumentException("numTexts cannot be less than 1 when creating TextMeshList.", nameof(numTexts));
            RowSpacing = .1f;
            NumTexts = numTexts;
            Width = 2f;
            Canvas = gameObject.GetComponent<Canvas>();
            if (Canvas == null)
                throw new InvalidOperationException("Canvas is null in TextMeshList");

            HeaderText = new GameObject($"{gameObject.name}: Header").AddComponent<FloatingText>();
            HeaderText.Canvas = Canvas;
            HeaderText.Width = Width;
            HeaderText.TextAlignment = TMPro.TextAlignmentOptions.Center;
            HeaderText.transform.SetParent(Canvas.transform, false);
            HeaderText.Position = new Vector3(0, RowSpacing, 0);
            HeaderText.FontStyle = TMPro.FontStyles.Underline;
            PostTexts = new PostText[NumTexts];

            for (int i = 0; i < numTexts; i++)
            {
                try
                {
                    var text = new GameObject($"{gameObject.name}: TextList[{i}]").AddComponent<FloatingText>();
                    text.Canvas = Canvas;
                    text.Width = Width;
                    var postText = new PostText() { PostId = 0, FloatingText = text };
                    PostTexts[i] = postText;
                    //if (i % 2 == 0)
                    //{
                    //    text.TextAlignment = TMPro.TextAlignmentOptions.Right;
                    //}
                    //else
                    text.TextAlignment = TMPro.TextAlignmentOptions.Left;
                    text.transform.SetParent(Canvas.transform, false);
                    text.Position = new Vector3(0, -i * RowSpacing, 0);
                    //text.DisplayedText = $"Text {i}";
                }
                catch (Exception ex)
                {
                    Logger.log?.Error(ex);
                }
            }
        }

        public void Post(int postId, string text, Color color)
        {
            if (PostTexts == null)
            {
                Logger.log?.Error("Unable to post text, FloatingTexts is null.");
                return;
            }

            //switch (Next)
            //{
            //    case 0:
            //        text = text + "-";
            //        break;
            //    case 1:
            //        text = text + "---";
            //        break;
            //    case 2:
            //        text = text + "-----";
            //        break;
            //    case 3:
            //        text = text + "-------";
            //        break;
            //    case 4:
            //        text = text + "---------";
            //        break;
            //    default:
            //        break;
            //}
            //HeaderText.DisplayedText = $"{gameObject.name}: Header ({text})";
            //HeaderText.WriteThings();
            PostTexts[Next].PostId = postId;
            PostTexts[Next].FloatingText.FontColor = color;
            PostTexts[Next].FloatingText.DisplayedText = text;

            //FloatingTexts[Next].WriteThings();
            Next++;
            if (Next != 0)
            {
                PostTexts[Next].FloatingText.DisplayedText = string.Empty;
                PostTexts[Next].PostId = 0;
            }
        }

        public bool ReplacePost(int postId, string text, Color? color)
        {
            var floatingText = PostTexts.FirstOrDefault(p => p.PostId == postId)?.FloatingText;
            if (floatingText != null)
            {
                floatingText.DisplayedText = text;
                if (color != null)
                    floatingText.FontColor = color ?? Color.white;
                return true;
            }
            return false;
        }

        public bool AppendPost(int postId, string text, Color? color)
        {
            var floatingText = PostTexts.FirstOrDefault(p => p.PostId == postId)?.FloatingText;
            if (floatingText != null)
            {
                floatingText.DisplayedText = floatingText.DisplayedText + text;
                if (color != null)
                    floatingText.FontColor = color ?? Color.white;
                return true;
            }
            return false;
        }

        public bool PostExists(int postId)
        {
            return PostTexts.Any(p => p.PostId == postId);
        }

        public string GetPost(int postId)
        {
            return PostTexts.FirstOrDefault(p => p.PostId == postId)?.FloatingText?.DisplayedText;
        }

        public void AppendLast(string text, UI.FontColor fontColor = FontColor.None)
        {
            PostTexts[Last].FloatingText.DisplayedText = PostTexts[Last].FloatingText.DisplayedText + text;
            if (fontColor != FontColor.None)
                PostTexts[Last].FloatingText.FontColor = GetUnityColor(fontColor) ?? Color.white;
        }

        public void Clear()
        {
            foreach (var item in PostTexts)
            {
                item.PostId = 0;
                item.FloatingText.DisplayedText = string.Empty;
            }
            Next = 0;
        }

        public void SetHeaderColor(FontColor color)
        {
            var newColor = GetUnityColor(color);
            if (newColor == null)
                return;
            else
                HeaderText.FontColor = newColor ?? Color.white;
        }

        public FontColor? GetHeaderColor()
        {
            Color unityColor = HeaderText.FontColor;
            if (unityColor == Color.white)
                return FontColor.White;
            else if (unityColor == Color.red)
                return FontColor.Red;
            else if (unityColor == Color.yellow)
                return FontColor.Yellow;
            else if (unityColor == Color.green)
                return FontColor.Green;
            else
                return null;
        }

        public static Color? GetUnityColor(FontColor color)
        {
            Color newColor;
            switch (color)
            {
                case FontColor.None:
                    return null;
                case FontColor.White:
                    newColor = Color.white;
                    break;
                case FontColor.Red:
                    newColor = Color.red;
                    break;
                case FontColor.Yellow:
                    newColor = Color.yellow;
                    break;
                case FontColor.Green:
                    newColor = Color.green;
                    break;
                default:
                    newColor = Color.white;
                    break;
            }
            return newColor;
        }

        public void MoveRelative(Vector3 vector)
        {
            Canvas.transform.localPosition = Canvas.transform.localPosition + vector;
        }

        public void MoveRelative(float x, float y, float z)
        {
            MoveRelative(new Vector3(x, y, z));
        }

        public void RotateRelative(float angle)
        {
            var currentRotation = Canvas.transform.localRotation.eulerAngles;
            currentRotation.y += angle;
            Canvas.transform.localRotation = Quaternion.Euler(currentRotation);
        }

        #region Overloads

        public void Post(int postId, string text, UI.FontColor color)
        {
            Post(postId, text, GetUnityColor(color) ?? Color.white);
        }

        public void Post(int postId, string text)
        {
            Post(postId, text, Color.white);
        }

        public bool AppendPost(int postId, string text, FontColor color)
        {
            return AppendPost(postId, text, GetUnityColor(color));
        }

        public bool AppendPost(int postId, string text)
        {
            return AppendPost(postId, text, null);
        }

        public bool ReplacePost(int postId, string text, FontColor color)
        {
            return ReplacePost(postId, text, GetUnityColor(color));
        }

        public bool ReplacePost(int postId, string text)
        {
            return ReplacePost(postId, text, null);
        }

        #endregion

        #region Monobehaviour


        public void Awake()
        {

        }

        public void Start()
        {

        }

        public void OnDisable()
        {
            Logger.log?.Info($"Disabling TextMeshList");
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child != null)
                    child.SetActive(false);
            }
        }
        public void OnEnable()
        {
            Logger.log?.Info($"Enabling TextMeshList");
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child != null)
                    child.SetActive(true);
            }
        }

        public void OnDestroy()
        {
            Logger.log?.Info($"Destroying TextMeshList");
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child != null)
                    GameObject.Destroy(child);
            }
        }

        #endregion
    }


}
