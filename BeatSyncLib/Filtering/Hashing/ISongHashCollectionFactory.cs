namespace BeatSyncLib.Filtering.Hashing
{
    public interface ISongHashCollectionFactory
    {
        ISongHashCollection GetCollection(string path);
    }
}
