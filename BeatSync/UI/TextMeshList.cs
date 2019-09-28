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
            get 
            {
                Next = _next; // Make sure Next isn't pointing to a pinned text.
                return _next; 
            }
            set
            {
                while (value < 0)
                    value = value + NumTexts;
                while (value >= NumTexts)
                    value = value - NumTexts;
                while (PostTexts[value].Pinned && value < PostTexts.Length - 1)
                    value++;
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

        private FloatingText[] _floatingTexts;
        private FloatingText[] FloatingTexts
        {
            get { return _floatingTexts; }
            set { _floatingTexts = value; }
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

            HeaderText = new GameObject($"{gameObject.name}.Header").AddComponent<FloatingText>();
            GameObject.DontDestroyOnLoad(HeaderText.gameObject);
            HeaderText.Canvas = Canvas;
            HeaderText.Width = Width;
            HeaderText.TextAlignment = TextAlignment.Center;
            HeaderText.transform.SetParent(Canvas.transform, false);
            HeaderText.Position = new Vector3(0, RowSpacing, 0);
            HeaderText.FontStyle = TMPro.FontStyles.Underline;
            PostTexts = new PostText[NumTexts];
            FloatingTexts = new FloatingText[NumTexts];

            for (int i = 0; i < numTexts; i++)
            {
                try
                {
                    var text = new GameObject($"{gameObject.name}.TextList[{i}]").AddComponent<FloatingText>();
                    GameObject.DontDestroyOnLoad(text.gameObject);
                    text.Canvas = Canvas;
                    text.Width = Width;
                    var postText = new PostText() { PostId = 0, FloatingText = text };
                    PostTexts[i] = postText;
                    //if (i % 2 == 0)
                    //{
                    //    text.TextAlignment = TMPro.TextAlignmentOptions.Right;
                    //}
                    //else
                    text.TextAlignment = TextAlignment.Left;
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
                PostTexts[Next].Clear();
            }
        }

        private void Swap(int first, int second)
        {
            if (first == second)
                return;
            if (!(first >= 0 && first < PostTexts.Length))
                throw new ArgumentOutOfRangeException(nameof(first), $"first value of {first} needs to be between 0 and {PostTexts.Length - 1}");
            if (!(second >= 0 && second < PostTexts.Length))
                throw new ArgumentOutOfRangeException(nameof(second), $"second value of {second} needs to be between 0 and {PostTexts.Length - 1}");
            var firstText = PostTexts[first];
            var secondText = PostTexts[second];
            int tempId = firstText.PostId;
            bool tempPinned = firstText.Pinned;
            string tempText = firstText.FloatingText.DisplayedText;
            Color tempColor = firstText.FloatingText.FontColor;

            firstText.PostId = secondText.PostId;
            firstText.Pinned = secondText.Pinned;
            firstText.FloatingText.DisplayedText = secondText.FloatingText.DisplayedText;
            firstText.FloatingText.FontColor = secondText.FloatingText.FontColor;

            secondText.PostId = tempId;
            secondText.Pinned = tempPinned;
            secondText.FloatingText.DisplayedText = tempText;
            secondText.FloatingText.FontColor = tempColor;
        }

        public bool Pin(int postId)
        {
            Logger.log?.Info($"Pinned Posts: {string.Join(", ", PostTexts.Select(p => p.Pinned))}");
            int firstFreeIndex = 0;
            while (PostTexts[firstFreeIndex].Pinned && firstFreeIndex < PostTexts.Length)
            {
                if (PostTexts[firstFreeIndex].PostId == postId)
                    return true;
                firstFreeIndex++;
            }
            if (firstFreeIndex == PostTexts.Length)
                return false;

            int targetIndex = firstFreeIndex;
            while (PostTexts[targetIndex].PostId != postId && targetIndex < PostTexts.Length)
            {
                targetIndex++;
                if (targetIndex >= PostTexts.Length)
                    return false;
            }
            PostTexts[targetIndex].Pinned = true;
            for (int i = targetIndex - 1; i >= 0; i--)
            {
                if (!PostTexts[i].Pinned)
                    Swap(i, i + 1);
                else
                    break;
            }
            Logger.log?.Info($"Pinned Posts: {string.Join(", ", PostTexts.Select(p => p.Pinned))}");
            return true;
        }
        private LinkedList<PostText> linked = new LinkedList<PostText>();

        public bool UnpinAndRemove(int postId)
        {
            bool foundPost = false;
            for (int i = 0; i < PostTexts.Length; i++)
            {
                if (PostTexts[i].PostId == postId)
                {
                    PostTexts[i].Clear();
                    foundPost = true;
                }
                if (foundPost && i < PostTexts.Length - 1)
                {
                    Swap(i, i + 1);
                }
            }
            return foundPost;
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
                item.Clear();
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
            //Logger.log?.Info($"Disabling TextMeshList");
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                if (child != null)
                    child.SetActive(false);
            }
        }
        public void OnEnable()
        {
            //Logger.log?.Info($"Enabling TextMeshList");
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                //Logger.log?.Info($"Enabling child {child?.gameObject.name}");
                if (child != null)
                    child.SetActive(true);
            }
        }

        public void OnDestroy()
        {
            //Logger.log?.Info($"Destroying TextMeshList");
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
