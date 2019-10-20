using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using SongFeedReaders.Readers;
using System.Collections.Concurrent;

namespace BeatSync.UI
{
    public class UIController : MonoBehaviour, IStatusManager
    {
        public int TextRows { get; set; }
        private int FadeTime { get; set; }
        private float RowSpacing { get; set; }
        private Dictionary<string, TextMeshList> _statusLists;
        private Dictionary<string, TextMeshList> statusLists
        {
            get { return _statusLists; }
            set
            {
                if (_statusLists == value)
                    return;
                _statusLists = value;
                _readOnlyWrapper = null;
            }
        }

        private ConcurrentDictionary<int, string> PostHistory = new ConcurrentDictionary<int, string>();

        private IReadOnlyDictionary<string, TextMeshList> _readOnlyWrapper;
        public IReadOnlyDictionary<string, TextMeshList> StatusLists
        {
            get
            {
                if (_statusLists == null)
                    return null;
                if (_readOnlyWrapper == null)
                    _readOnlyWrapper = new ReadOnlyDictionary<string, TextMeshList>(_statusLists);
                return _readOnlyWrapper;
            }
        }
        private float _distance;
        public float Distance
        {
            get
            {
                return _distance;
            }
            set
            {
                if (_distance != value)
                {
                    var difference = value - _distance;
                    _distance = value;
                    if (StatusLists != null)
                    {
                        foreach (var item in StatusLists.Values)
                        {
                            item.MoveRelative(0, 0, difference);
                            //item.gameObject.transform.localPosition = new Vector3(0, 0, _distance);
                            FacePlayer();
                        }
                    }
                }
            }
        }

        private float _height;
        public float Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height != value)
                {
                    //Logger.log?.Info($"Height: {_height} > {value}");
                    _height = value;
                    var currentPos = gameObject.transform.position;
                    currentPos.y = _height;
                    gameObject.transform.position = currentPos;
                    FacePlayer();

                }
            }
        }
        private float _horizontalDegrees;
        public float HorizontalDegrees
        {
            get
            {
                return _horizontalDegrees;
            }
            set
            {
                if (_horizontalDegrees != value)
                {
                    //Logger.log?.Info($"VerticalDegrees: {_horizontalDegrees} > {value}");
                    var diff = value - _horizontalDegrees;
                    _horizontalDegrees = value;
                    RotateRelative(new Vector3(0, 1, 0), diff);
                }
            }
        }

        public Vector3 PlayerPos { get; set; }

        private object _idLockObject = new object();
        private int _nextId = 1;
        private int NextId
        {
            get
            {
                int returnedId = 0;
                lock (_idLockObject)
                {
                    returnedId = _nextId;
                    _nextId++;
                }
                return returnedId;
            }
        }

        #region IStatusManager

        public void SetHeader(string targetName, string text, FontColor color)
        {
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                statusList.Header = text;
                if (color != FontColor.None)
                    statusList.SetHeaderColor(color);
            }
        }

        public int Post(string targetName, string text, FontColor color)
        {
            var postId = NextId;
            if (postId == 0)
                Logger.log?.Error($"postId is 0 during Post, this shouldn't happen");
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                if (!PostHistory.TryAdd(postId, targetName))
                    Logger.log?.Warn($"postId {postId} for {targetName} couldn't be added.");
                statusList.Post(postId, text, color);
#if DEBUG
                Logger.log?.Info($"Posting text {text} to {targetName}.{postId}");
#endif
                return postId;
            }
            return 0;
        }

        public string GetPost(int postId)
        {
            if (PostHistory.TryGetValue(postId, out string targetName))
            {
                if (StatusLists.TryGetValue(targetName, out var statusList))
                {
                    var text = statusList.GetPost(postId);
                    if (text != null)
                        return text;
                }
            }
            PostHistory.TryRemove(postId, out var _); // Doesn't exist anymore, remove
            return null;
        }

        public bool ReplacePost(int postId, string text, FontColor color)
        {
            if (PostHistory.TryGetValue(postId, out string targetName))
            {
                if (StatusLists.TryGetValue(targetName, out var statusList))
                {
                    if (statusList.ReplacePost(postId, text, color))
                        return true;
                }
            }
            PostHistory.TryRemove(postId, out var _); // Doesn't exist anymore, remove
            return false;
        }

        public bool AppendPost(int postId, string text, FontColor color)
        {
            if (PostHistory.TryGetValue(postId, out string targetName))
            {
                if (StatusLists.TryGetValue(targetName, out var statusList))
                {
                    if (statusList.AppendPost(postId, text, color))
                        return true;
                    //else
                    //{
                    //    Logger.log?.Debug($"Failed to append PostId {postId} for {targetName} at AppendPost({postId}, {text}, {color})");
                    //    var postIds = string.Join(", ", statusList.PostTexts.Select(l => l.PostId).ToList());
                    //    Logger.log?.Debug($"Valid PostIds: {postIds}");
                    //}
                }
                //else
                //    Logger.log?.Debug($"Failed to append PostId {postId} for {targetName} at StatusLists.TryGetValue({targetName})");
            }
            //else
            //    Logger.log?.Debug($"Failed to append PostId {postId} at PostHistory.TryGetValue({postId})");
            PostHistory.TryRemove(postId, out var _); // Doesn't exist anymore, remove
            return false;
        }

        public bool PostExists(int postId)
        {
            if (PostHistory.TryGetValue(postId, out string targetName))
            {
                if (StatusLists.TryGetValue(targetName, out var statusList))
                {
                    if (statusList.PostExists(postId))
                        return true;
                }
            }
            PostHistory.TryRemove(postId, out var _); // Doesn't exist anymore, remove
            return false;
        }

        public bool PinPost(int postId)
        {
            if (PostHistory.TryGetValue(postId, out string targetName))
            {
                if (StatusLists.TryGetValue(targetName, out var statusList))
                {
                    if (statusList.PostExists(postId))
                        return statusList.Pin(postId);
                }
            }
            PostHistory.TryRemove(postId, out var _); // Doesn't exist anymore, remove
            return false;
        }

        public bool UnpinAndRemovePost(int postId)
        {
            if (PostHistory.TryGetValue(postId, out string targetName))
            {
                if (StatusLists.TryGetValue(targetName, out var statusList))
                {
                    if (statusList.PostExists(postId))
                        return statusList.UnpinAndRemove(postId);
                }
            }
            PostHistory.TryRemove(postId, out var _); // Doesn't exist anymore, remove
            return false;
        }

        public string GetHeader(string targetName)
        {
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                return statusList.Header;
            }
            return null;
        }

        public string GetSubHeader(string targetName)
        {
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                return statusList.SubHeader;
            }
            return null;
        }

        public FontColor? GetHeaderColor(string targetName)
        {
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                return statusList.GetHeaderColor();
            }
            return null;
        }

        public void SetSubHeader(string targetName, string text)
        {
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                statusList.SubHeader = text;
            }
        }

        public void SetHeaderColor(string targetName, FontColor color)
        {
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                statusList.SetHeaderColor(color);
            }
        }

        public void Clear(string targetName)
        {
            if (StatusLists.TryGetValue(targetName, out var statusList))
            {
                statusList.Clear();
            }
            else
                Logger.log?.Warn($"Unable to clear {targetName}");
        }

        public void ClearAll()
        {
            foreach (var item in StatusLists.Values)
            {
                item.Clear();
            }
        }


        /// <summary>
        /// Removes the post from the specified target, if it exists. Returns false if the post doesn't exist.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public bool RemovePost(int postId)
        {
            bool successful = false;
            if (PostHistory.TryGetValue(postId, out string targetName))
            {
                if (StatusLists.TryGetValue(targetName, out var statusList))
                {
                    if (statusList.RemovePost(postId))
                        successful = true;
                }
            }
            PostHistory.TryRemove(postId, out var _); // Doesn't exist anymore, remove
            return successful;
        }

        #region Overloads
        public int Post(string targetName, string text)
        {
            return Post(targetName, text, FontColor.None);
        }

        public void SetHeader(string targetName, string text)
        {
            SetHeader(targetName, text, FontColor.None);
        }

        public bool AppendPost(int postId, string text)
        {
            return AppendPost(postId, text, FontColor.None);
        }

        public bool ReplacePost(int postId, string text)
        {
            return ReplacePost(postId, text, FontColor.None);
        }

        #endregion

        #endregion

        public void Awake()
        {
            //Logger.log?.Info("UIController awake.");
            _statusLists = new Dictionary<string, TextMeshList>();
            PlayerPos = new Vector3(0, 1.7f, 0);
            UpdateSettings();
            //Logger.log?.Info($"UIController Position: {gameObject.transform.position}");
            //WriteParents(gameObject);
            //CreateCanvas();

        }

        public void UpdateSettings()
        {
            TextRows = Plugin.config.Value.StatusUI.TextRows;
            FadeTime = Plugin.config.Value.StatusUI.FadeTime;
            RowSpacing = Plugin.config.Value.StatusUI.RowSpacing;
            Distance = Plugin.config.Value.StatusUI.Distance;
            Height = Plugin.config.Value.StatusUI.Height;
            HorizontalDegrees = Plugin.config.Value.StatusUI.HorizontalAngle;
        }

        public void TriggerFade()
        {
            if(FadeTime > 0)
                StartCoroutine(DestroyAfterSeconds(FadeTime));
        }

        private void WriteParents(GameObject gObject)
        {

            while (gObject != null)
            {
                Logger.log?.Info($"  {gObject.name}");
                gObject = gObject.transform.parent?.gameObject;
            }
        }

        private Canvas CreateCanvas(GameObject parent)
        {
            var canvas = parent.AddComponent<Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.WorldSpace;
            (canvas.transform as RectTransform).sizeDelta = new Vector2(4f, 2f);
            canvas.transform.SetParent(parent.transform, false);
            //Logger.log?.Warn($"UIController Position: {parent.transform.position}");
            //canvas.transform.position = new Vector3(0, 0, Distance);
            //Logger.log?.Info($"UIController Position: {parent.transform.position}");
            return canvas;
        }

        public void Start()
        {
            //FloatingTexts = new TextMeshList(TextRows, Canvas);
            var beastSaber = CreateTextList("BeastSaberStatus", "BeastSaber");

            var beatSaver = CreateTextList("BeatSaverStatus", "Beat Saver");
            beatSaver.MoveRelative(-2f, 0, -1f);
            var heightDiff = beastSaber.Canvas.transform.position.y - beatSaver.Canvas.transform.position.y;
            //beatSaver.MoveRelative(0, heightDiff, 0);
            beatSaver.RotateRelative(-45);

            var scoreSaber = CreateTextList("ScoreSaberStatus", "ScoreSaber");
            scoreSaber.MoveRelative(2f, 0, -1f);
            heightDiff = scoreSaber.Canvas.transform.position.y - beatSaver.Canvas.transform.position.y;
            //beatSaver.MoveRelative(0, heightDiff, 0);
            scoreSaber.RotateRelative(45);

            _statusLists.Add(BeatSaverReader.NameKey, beatSaver);
            _statusLists.Add(BeastSaberReader.NameKey, beastSaber);
            _statusLists.Add(ScoreSaberReader.NameKey, scoreSaber);
            FacePlayer();
            //StartCoroutine(TestPost());
        }

        public TextMeshList CreateTextList(string name, string headerText)
        {
            var textsGO = new GameObject("BeatSync." + name);
            GameObject.DontDestroyOnLoad(textsGO.gameObject);
            CreateCanvas(textsGO);
            var textList = textsGO.AddComponent<TextMeshList>();
            textsGO.transform.localPosition = new Vector3(0, 0, Distance);
            textList.Initialize(TextRows);
            textList.Header = headerText;
            textsGO.transform.SetParent(gameObject.transform, false);
            return textList;
        }

        public static IEnumerator<WaitForSeconds> DestroyAfterSeconds(int seconds)
        {
            if (Plugin.StatusController == null)
                yield break;
            else
            {
                yield return new WaitForSeconds(seconds);
                GameObject.Destroy(Plugin.StatusController.gameObject);
            }
        }

        public IEnumerator<WaitForSeconds> TestPost()
        {
            var rate = new WaitForSeconds(2);
            int index = 1;
            while (true)
            {
                yield return rate;
                foreach (var item in StatusLists.Values)
                {
                    item.Header = $"{item.gameObject.name}: Header";
                    if (index % 2 == 0)
                        item.SubHeader = $"Index: {index}";
                    else
                        item.SubHeader = string.Empty;
                    item.Post(0, $"Index: {index}");
                }

                index++;
            }
        }

        public void OnDestroy()
        {
            //Logger.log?.Warn("Destroying UI Controller.");
            foreach (var item in StatusLists.Values)
            {
                if (item != null)
                    GameObject.Destroy(item.gameObject);
            }
            Plugin.StatusController = null;
        }

        public void FacePlayer()
        {
            foreach (var item in StatusLists.Values)
            {
                var currentRotation = item.Canvas.transform.localRotation;
                currentRotation.SetLookRotation(item.Canvas.transform.position - PlayerPos);
                item.Canvas.transform.localRotation = currentRotation;
            }
        }

        public void RotateRelative(Vector3 axis, float angle)
        {
            gameObject.transform.Rotate(axis, angle);
        }

        #region Monobehaviour
        public void OnDisable()
        {
            foreach (var item in StatusLists.Values)
            {
                item.gameObject.SetActive(false);
            }
        }

        public void OnEnable()
        {
            foreach (var item in StatusLists.Values)
            {
                item.gameObject.SetActive(true);
            }
        }

        //public void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.UpArrow))
        //    {
        //        Height = Height + .1f;
        //        Logger.log?.Info($"UIController Position: {gameObject.transform.position}, Rotation: {gameObject.transform.rotation}");

        //    }
        //    if (Input.GetKeyDown(KeyCode.DownArrow))
        //    {
        //        Height = Height - .1f;
        //        Logger.log?.Info($"UIController Position: {gameObject.transform.position}, Rotation: {gameObject.transform.rotation}");
        //    }
        //    if (Input.GetKeyDown(KeyCode.LeftArrow))
        //    {
        //        HorizontalDegrees = HorizontalDegrees - 5;
        //        Logger.log?.Info($"UIController Position: {gameObject.transform.position}, Rotation: {gameObject.transform.rotation}");
        //    }
        //    if (Input.GetKeyDown(KeyCode.RightArrow))
        //    {
        //        HorizontalDegrees = HorizontalDegrees + 5;
        //        Logger.log?.Info($"UIController Position: {gameObject.transform.position}, Rotation: {gameObject.transform.rotation}");
        //    }
        //    if (Input.GetKeyDown(KeyCode.PageUp))
        //    {
        //        Distance = Distance + .1f;
        //        Logger.log?.Info($"UIController Position: {gameObject.transform.position}, Rotation: {gameObject.transform.rotation}");
        //    }
        //    if (Input.GetKeyDown(KeyCode.PageDown))
        //    {
        //        Distance = Distance - .1f;
        //        Logger.log?.Info($"UIController Position: {gameObject.transform.position}, Rotation: {gameObject.transform.rotation}");
        //    }

        //}
        #endregion



    }
}
