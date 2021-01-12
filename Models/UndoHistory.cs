using System;
using System.Collections.Generic;

namespace SudokuBlazor.Models
{
    using UndoSnapshotEntry = Dictionary<string, object>;

    public class UndoHistory
    {
        private readonly List<UndoSnapshotEntry> snapshots = new();
        private UndoSnapshotEntry pendingSnapshot = null;
        private int snapshotIndex = -1;

        public void Reset()
        {
            snapshots.Clear();
            pendingSnapshot = null;
            snapshotIndex = -1;
        }

        public void BeginPendingSnapshot()
        {
            if (pendingSnapshot != null)
            {
                throw new InvalidOperationException("Pending snapshot is already in progress.");
            }

            pendingSnapshot = new UndoSnapshotEntry();
        }

        public void StorePendingSnapshotData(string key, object data)
        {
            if (pendingSnapshot == null)
            {
                throw new InvalidOperationException("No pending snapshot is in progress.");
            }

            pendingSnapshot[key] = data;
        }

        public void CommitPendingSnapshot()
        {
            if (pendingSnapshot == null)
            {
                throw new InvalidOperationException("No pending snapshot is in progress.");
            }

            if (snapshotIndex + 1 < snapshots.Count)
            {
                snapshots.RemoveRange(snapshotIndex + 1, snapshots.Count - (snapshotIndex + 1));
            }
            snapshots.Add(pendingSnapshot);
            snapshotIndex = snapshots.Count - 1;
            pendingSnapshot = null;
        }

        public void CancelPendingSnapshot()
        {
            if (pendingSnapshot == null)
            {
                throw new InvalidOperationException("No pending snapshot is in progress.");
            }
            pendingSnapshot = null;
        }

        public UndoSnapshotEntry Undo()
        {
            if (snapshotIndex - 1 < 0)
            {
                return null;
            }
            return snapshots[--snapshotIndex];
        }

        public UndoSnapshotEntry Redo()
        {
            if (snapshotIndex + 1 >= snapshots.Count)
            {
                return null;
            }
            return snapshots[++snapshotIndex];
        }
    }
}
