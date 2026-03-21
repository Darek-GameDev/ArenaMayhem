using System.Collections.Generic;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker
{
    // Responsibility: Define context structures for change operations
    // Contains all context structs used across the tracker to pass data between recorders/applicators and stores.

    public struct TransformStoreContext
    {
        public string ScenePath { get; set; }
        public string ObjectPath { get; set; }
        public string GlobalObjectId { get; set; }
        public Snapshot Original { get; set; }
    }

    public struct ComponentStoreContext
    {
        public string ScenePath { get; set; }
        public string ObjectPath { get; set; }
        public int ComponentIndex { get; set; }
        public Component Component { get; set; }
        public CompSnapshot OriginalSnapshot { get; set; }
        public List<string> PropertyPaths { get; set; }
        public bool IncludeMaterialChanges { get; set; }
    }

    public struct ComponentChangeContext
    {
        public string ScenePath { get; set; }
        public string ObjectPath { get; set; }
        public string GlobalObjectId { get; set; }
        public int ComponentIndex { get; set; }
        public Component Component { get; set; }
        public List<string> PropertyPaths { get; set; }
        public List<string> SerializedValues { get; set; }
        public List<string> ValueTypes { get; set; }
        public bool IncludeMaterialChanges { get; set; }
        public List<string> MaterialGuids { get; set; }
    }

    public struct TransformChangeContext
    {
        public string ScenePath { get; set; }
        public string ObjectPath { get; set; }
        public string GlobalObjectId { get; set; }
        public Snapshot Current { get; set; }
        public List<string> ModifiedProperties { get; set; }
    }

    public struct NameChangeContext
    {
        public string ScenePath { get; set; }
        public string ObjectPath { get; set; }
        public string GlobalObjectId { get; set; }
        public string OriginalName { get; set; }
        public string NewName { get; set; }
    }
}