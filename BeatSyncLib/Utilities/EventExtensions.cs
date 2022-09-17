using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatSyncLib.Utilities
{
    public static class EventExtensions
    {

        public static void RaiseEventSafe(this EventHandler? e, object sender, string eventName, ILogger? logger = null)
        {
            if (e == null) 
                return;
            EventHandler[] handlers = e.GetInvocationList().Select(d => (EventHandler)d).ToArray()
                ?? Array.Empty<EventHandler>();
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    handlers[i].Invoke(sender, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    logger?.Error($"Error in '{eventName}' handlers '{handlers[i]?.Method.Name}': {ex.Message}");
                    logger?.Debug(ex);
                }
            }
        }

        public static void RaiseEventSafe<TArgs>(this EventHandler<TArgs>? e, object sender, TArgs args, 
            string eventName, ILogger? logger = null)
        {
            if (e == null) 
                return;
            EventHandler<TArgs>[] handlers = e.GetInvocationList().Select(d => (EventHandler<TArgs>)d).ToArray()
                ?? Array.Empty<EventHandler<TArgs>>();
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    handlers[i].Invoke(sender, args);
                }
                catch (Exception ex)
                {
                    logger?.Error($"Error in '{eventName}' handlers '{handlers[i]?.Method.Name}': {ex.Message}");
                    logger?.Debug(ex);
                }
            }
        }
    }
}
