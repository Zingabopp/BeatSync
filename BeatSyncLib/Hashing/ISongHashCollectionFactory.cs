using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Hashing
{
    public interface ISongHashCollectionFactory
    {
        ISongHashCollection GetCollection(string path);
    }
}
