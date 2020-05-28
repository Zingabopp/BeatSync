using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncLib
{
    public interface IStatusManager
    {
        /// <summary>
        /// Post text to the specified target. Returns a post ID.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        int Post(string targetName, string text);
        /// <summary>
        /// Post text in the provided color to the specified target. Returns a post ID.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        int Post(string targetName, string text, FontColor color);
        /// <summary>
        /// Returns the text of the post with the provided postId. Returns null if the post no longer exists.
        /// </summary>
        /// <param name="postId"></param>
        /// <returns></returns>
        string GetPost(int postId);
        /// <summary>
        /// Replaces the specified post's text with the text provided. Returns false if the post no longer exists.
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        bool ReplacePost(int postId, string text);
        /// <summary>
        /// Replaces the specified post's text with the text provided. Returns false if the post no longer exists.
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        bool ReplacePost(int postId, string text, FontColor color);
        /// <summary>
        /// Appends the specified post's text with the text provided.
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        bool AppendPost(int postId, string text);
        /// <summary>
        /// Appends the specified post's text with the text provided and changes the font color.
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        bool AppendPost(int postId, string text, FontColor color);
        /// <summary>
        /// Returns true if the post with the provided postId still exists.
        /// </summary>
        /// <param name="postId"></param>
        /// <returns></returns>
        bool PostExists(int postId);
        /// <summary>
        /// Pins a post to the top of the list so it doesn't get overwritten.
        /// </summary>
        /// <param name="postId"></param>
        /// <returns></returns>
        bool PinPost(int postId);
        /// <summary>
        /// Unpins and removes a post from the list.
        /// </summary>
        /// <param name="postId"></param>
        /// <returns></returns>
        bool UnpinAndRemovePost(int postId);
        /// <summary>
        /// Gets the text of the target's header.
        /// </summary>
        /// <param name="targetName"></param>
        /// <returns></returns>
        string GetHeader(string targetName);
        /// <summary>
        /// Gets the text of the target's SubHeader.
        /// </summary>
        /// <param name="targetName"></param>
        /// <returns></returns>
        string GetSubHeader(string targetName);
        /// <summary>
        /// Gets the color of the target's header.
        /// </summary>
        /// <param name="targetName"></param>
        /// <returns></returns>
        FontColor? GetHeaderColor(string targetName);
        /// <summary>
        /// Sets the header text of the specified target.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="text"></param>
        void SetHeader(string targetName, string text);
        /// <summary>
        /// Sets the header text and color of the specified target.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="text"></param>
        /// <param name="color"></param>
        void SetHeader(string targetName, string text, FontColor color);
        /// <summary>
        /// Sets the subheader text of the specified target.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="text"></param>
        void SetSubHeader(string targetName, string text);
        /// <summary>
        /// Sets the subheader text and header color of the specified target.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="color"></param>
        void SetHeaderColor(string targetName, FontColor color);

        /// <summary>
        /// Clears all posts from all targets.
        /// </summary>
        void ClearAll();
        /// <summary>
        /// Clears all posts from the specified target.
        /// </summary>
        /// <param name="targetName"></param>
        void Clear(string targetName);
        /// <summary>
        /// Removes the post from the specified target, if it exists. Returns false if the post doesn't exist.
        /// </summary>
        /// <param name="targetName"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        bool RemovePost(int postId);
    }

    public enum FontColor
    {
        None = 0,
        White = 1,
        Red = 2,
        Yellow = 3,
        Green = 4
    }

    public enum TextAlignment
    {
        Left = 0,
        Center = 1,
        Right = 2
    }
}
