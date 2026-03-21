using UnityEngine;
using static PlayModeChangesSaver.Editor.ChangesTracker.SnapshotManager;


namespace PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper
{
    public static class Name_SH //Name SnapShot Helper
    {
        public static NameSnapshot GetNameSnapshot(GameObject go)
        {
            if (!go)
            {
                return null;
            }

            string key = GetGoKey(go);
            bool found = NameSnapshots.TryGetValue(key, out var snap);
            return found ? snap : null;
        }

        public static void SetNameSnapshot(GameObject go, NameSnapshot snapshot)
        {
            if (snapshot==null)
            {
                return;
            }

            string key = GetGoKey(go);
            NameSnapshots[key] = snapshot;
        }
    }
}