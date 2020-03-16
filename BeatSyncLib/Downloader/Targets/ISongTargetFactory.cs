using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader.Targets
{
    public interface ISongTargetFactory
    {
        /// <summary>
        /// Default settings to use for <see cref="CreateTarget(ISong)"/> 
        /// or when <see cref="CreateTarget(ISong, ISongTargetFactorySettings)"/> is given a null value for the <see cref="ISongTargetFactorySettings"/> parameter.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="settings"/> is invalid for this <see cref="ISongTargetFactory"/>.</exception>
        ISongTargetFactorySettings DefaultSettings { get; set; }
        /// <summary>
        /// Returns true if <paramref name="settings"/> is valid for this <see cref="ISongTargetFactory"/>.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>True if the settings are valid.</returns>
        bool IsValidSettings(ISongTargetFactorySettings settings);
        /// <summary>
        /// Creates a new <see cref="ISongTarget"/> for <paramref name="song"/> using <see cref="DefaultSettings"/>.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="song"/> is null.</exception>
        ISongTarget CreateTarget(ISong song);
        /// <summary>
        /// Creates a new <see cref="ISongTarget"/> for <paramref name="song"/> using <paramref name="settings"/> as the <see cref="ISongTargetFactorySettings"/>.
        /// If <paramref name="settings"/> is null, use <see cref="DefaultSettings"/>.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="song"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="settings"/> is invalid for this <see cref="ISongTargetFactory"/>.</exception>
        ISongTarget CreateTarget(ISong song, ISongTargetFactorySettings settings);
    }

    public interface ISongTargetFactorySettings
    {

    }
}
