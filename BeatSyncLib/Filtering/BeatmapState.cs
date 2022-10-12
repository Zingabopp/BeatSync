namespace BeatSyncLib.Filtering
{
    public enum BeatmapState
    {
        /// <summary>
        /// Beatmap is wanted
        /// </summary>
        Wanted = 0,
        /// <summary>
        /// Beatmap exists and is not wanted
        /// </summary>
        Exists = 1,
        /// <summary>
        /// Beatmap does not exist and is not wanted
        /// </summary>
        NotWanted = 2,
        /// <summary>
        /// Do not process anymore beatmaps
        /// </summary>
        BreakProcessing = 3
    }
}
