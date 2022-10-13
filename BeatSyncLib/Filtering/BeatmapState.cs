namespace BeatSyncLib.Filtering
{
    /// <summary>
    /// Defines a state for filtering. The order is important (higher numbers take priority).
    /// </summary>
    public enum BeatmapState
    {
        /// <summary>
        /// Beatmap is wanted
        /// </summary>
        Wanted = 0,
        /// <summary>
        /// Beatmap was just downloaded
        /// </summary>
        Downloaded = 1,
        /// <summary>
        /// Beatmap exists and is not wanted
        /// </summary>
        Exists = 2,
        /// <summary>
        /// Beatmap does not exist and is not wanted
        /// </summary>
        NotWanted = 3,
        /// <summary>
        /// Do not process anymore beatmaps
        /// </summary>
        BreakProcessing = 4
    }
}
