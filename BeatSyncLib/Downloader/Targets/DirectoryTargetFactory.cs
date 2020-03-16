using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeatSyncLib.Downloader.Targets
{
    public class DirectoryTargetFactory : ISongTargetFactory
    {
        private ISongTargetFactorySettings _defaultSettings;
        public ISongTargetFactorySettings DefaultSettings 
        {
            get { return _defaultSettings; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(DefaultSettings), "DefaultSettings cannot be null.");
                if(value is DirectoryTargetFactorySettings castSettings)
                {
                    _defaultSettings = castSettings;
                    return;
                }
                throw new ArgumentException($"Type of {nameof(DefaultSettings)} is invalid for {nameof(DirectoryTargetFactory)}: {value.GetType().Name}.");
            }
        }
        public string SongsDirectory { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="DirectoryTargetFactory"/> using the given song directory. 
        /// If <paramref name="settings"/> is null, a default <see cref="DirectoryTargetFactorySettings"/> is created.
        /// </summary>
        /// <param name="songsDirectory"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="songsDirectory"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="settings"/> is not a <see cref="DirectoryTargetFactorySettings"/>.</exception>
        public DirectoryTargetFactory(string songsDirectory, ISongTargetFactorySettings settings = null)
        {
            DefaultSettings = settings ?? new DirectoryTargetFactorySettings();
            if (string.IsNullOrEmpty(songsDirectory))
                throw new ArgumentNullException(nameof(songsDirectory), $"{nameof(songsDirectory)} cannot be null or empty.");
            try
            {
                songsDirectory = Path.GetFullPath(songsDirectory);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{nameof(songsDirectory)} does not contain a valid path: '{songsDirectory}'", nameof(songsDirectory), ex);
            }
            if (!Directory.Exists(songsDirectory))
                throw new ArgumentException($"Directory specified in {nameof(songsDirectory)} does not exist: '{songsDirectory}'", nameof(songsDirectory));
            SongsDirectory = songsDirectory;
        }

        public bool IsValidSettings(ISongTargetFactorySettings settings)
        {
            return settings is DirectoryTargetFactorySettings _;
        }

        public ISongTarget CreateTarget(ISong song)
        {

            return CreateTarget(song, DefaultSettings);
        }

        public ISongTarget CreateTarget(ISong song, ISongTargetFactorySettings settings)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "Cannot create a target from a null song.");
            if (settings == null)
                settings = DefaultSettings;
            if (settings is DirectoryTargetFactorySettings castSettings)
            {
                return new DirectoryTarget(SongsDirectory, song, castSettings.OverwriteTarget);
            }
            else
                throw new ArgumentException($"Type of {nameof(settings)} is invalid for {nameof(DirectoryTargetFactory)}: {settings.GetType().Name}.");
        }
    }

    public class DirectoryTargetFactorySettings : ISongTargetFactorySettings
    {
        public bool OverwriteTarget { get; set; }
        public DirectoryTargetFactorySettings()
        {
            OverwriteTarget = false;
        }
    }
}
