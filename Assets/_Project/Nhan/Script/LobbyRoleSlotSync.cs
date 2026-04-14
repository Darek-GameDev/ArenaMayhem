using Fusion;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LobbyRoleSlotSync : MonoBehaviour
{
    [System.Serializable]
    private class SlotView
    {
        public GameObject slotRoot;
        public GameObject knightImage;
        public GameObject archerImage;
    }

    [Header("Slots")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private SlotView player1 = new SlotView();
    [SerializeField] private SlotView player2 = new SlotView();
    [SerializeField] private SlotView player3 = new SlotView();
    [SerializeField] private SlotView player4 = new SlotView();
    [SerializeField] private SlotView player5 = new SlotView();
    [SerializeField] private SlotView player6 = new SlotView();
    [SerializeField] private bool autoFindBySlotNames = true;
    [SerializeField] private bool fallbackToContainerChildOrder = true;
    [SerializeField] private bool cloneMissingRoleImagesFromSlot1 = true;
    [SerializeField] private bool hideUnusedSlots = true;

    [Header("Fallback")]
    [SerializeField] private SharedPlayerClassType fallbackClass = SharedPlayerClassType.Unknown;

    private readonly Dictionary<int, SharedPlayerClassType> cachedClassByPlayerId = new Dictionary<int, SharedPlayerClassType>();

    private void Awake()
    {
        ResolveReferences();
        EnsureRoleImagesExist();
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureRoleImagesExist();
        ResolveReferences();
        SyncNow();
    }

    private void LateUpdate()
    {
        SyncNow();
    }

    [ContextMenu("Resolve Slot References")]
    private void ResolveReferences()
    {
        if (slotsContainer == null)
        {
            slotsContainer = FindSlotsContainer();
        }

        SlotView[] slots = GetSlotViews();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = new SlotView();
            }

            SlotView slot = slots[i];

            if (slot.slotRoot == null)
            {
                if (slot.knightImage != null)
                {
                    Transform parent = slot.knightImage.transform.parent;
                    slot.slotRoot = parent != null ? parent.gameObject : slot.knightImage;
                }
                else if (slot.archerImage != null)
                {
                    Transform parent = slot.archerImage.transform.parent;
                    slot.slotRoot = parent != null ? parent.gameObject : slot.archerImage;
                }
            }

            if (slot.slotRoot == null && autoFindBySlotNames)
            {
                slot.slotRoot = FindSlotRoot(i);
            }

            if (slot.slotRoot == null && fallbackToContainerChildOrder)
            {
                slot.slotRoot = FindSlotRootByIndex(i);
            }

            if (slot.slotRoot == null)
            {
                continue;
            }

            if (slot.knightImage == null)
            {
                slot.knightImage = FindChildByKeyword(slot.slotRoot.transform, "knight");
            }

            if (slot.archerImage == null)
            {
                slot.archerImage = FindChildByKeyword(slot.slotRoot.transform, "archer");
            }
        }
    }

    private void SyncNow()
    {
        SlotView[] slots = GetSlotViews();
        if (slots.Length == 0)
        {
            return;
        }

        SharedRoomSessionManager sessionManager = SharedRoomSessionManager.Instance;
        if (sessionManager == null || !sessionManager.HasActiveSession)
        {
            cachedClassByPlayerId.Clear();
            ApplyOfflineVisualState();
            return;
        }

        var players = sessionManager.GetPlayersOrderedById();
        PruneClassCache(players);

        for (int i = 0; i < slots.Length; i++)
        {
            SlotView slot = slots[i];
            if (slot == null || slot.slotRoot == null)
            {
                continue;
            }

            bool occupied = i < players.Count;
            slot.slotRoot.SetActive(!hideUnusedSlots || occupied);

            if (!occupied)
            {
                SetRoleVisible(slot, SharedPlayerClassType.Unknown);
                continue;
            }

            SharedPlayerClassType classType = ResolvePlayerClass(sessionManager, players[i]);
            SetRoleVisible(slot, classType);
        }
    }

    private SharedPlayerClassType ResolvePlayerClass(SharedRoomSessionManager sessionManager, PlayerRef player)
    {
        if (sessionManager != null && sessionManager.Runner != null && SharedRoomSessionManager.TryGetPlayerClass(sessionManager.Runner, player, out SharedPlayerClassType networkClass) && networkClass != SharedPlayerClassType.Unknown)
        {
            cachedClassByPlayerId[player.RawEncoded] = networkClass;
            return networkClass;
        }

        SharedPlayerClassType directClass = sessionManager.GetPlayerClass(player, SharedPlayerClassType.Unknown);
        if (directClass != SharedPlayerClassType.Unknown)
        {
            cachedClassByPlayerId[player.RawEncoded] = directClass;
            return directClass;
        }

        if (sessionManager != null && sessionManager.Runner != null && sessionManager.Runner.LocalPlayer == player)
        {
            SharedPlayerClassType localClass = SharedPlayerClassTypeUtility.FromClassName(ClassChoose.LastConfirmedClassName);
            if (localClass != SharedPlayerClassType.Unknown)
            {
                cachedClassByPlayerId[player.RawEncoded] = localClass;
                return localClass;
            }

            return SharedPlayerClassType.Unknown;
        }

        return SharedPlayerClassType.Unknown;
    }

    private void PruneClassCache(IReadOnlyList<PlayerRef> activePlayers)
    {
        if (cachedClassByPlayerId.Count == 0)
        {
            return;
        }

        HashSet<int> activeIds = new HashSet<int>();
        for (int i = 0; i < activePlayers.Count; i++)
        {
            activeIds.Add(activePlayers[i].RawEncoded);
        }

        List<int> toRemove = null;
        foreach (KeyValuePair<int, SharedPlayerClassType> pair in cachedClassByPlayerId)
        {
            if (activeIds.Contains(pair.Key))
            {
                continue;
            }

            if (toRemove == null)
            {
                toRemove = new List<int>();
            }

            toRemove.Add(pair.Key);
        }

        if (toRemove == null)
        {
            return;
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            cachedClassByPlayerId.Remove(toRemove[i]);
        }
    }

    private void ApplyOfflineVisualState()
    {
        SlotView[] slots = GetSlotViews();
        for (int i = 0; i < slots.Length; i++)
        {
            SlotView slot = slots[i];
            if (slot == null || slot.slotRoot == null)
            {
                continue;
            }

            bool isFirst = i == 0;
            slot.slotRoot.SetActive(!hideUnusedSlots || isFirst);

            if (isFirst)
            {
                SharedPlayerClassType localClass = SharedPlayerClassTypeUtility.FromClassName(ClassChoose.LastConfirmedClassName);
                SetRoleVisible(slot, localClass);
            }
            else
            {
                SetRoleVisible(slot, SharedPlayerClassType.Unknown);
            }
        }
    }

    private void SetRoleVisible(SlotView slot, SharedPlayerClassType classType)
    {
        if (slot == null)
        {
            return;
        }

        bool showKnight = classType == SharedPlayerClassType.Knight;
        bool showArcher = classType == SharedPlayerClassType.Archer;

        if (slot.knightImage != null)
        {
            slot.knightImage.SetActive(showKnight);
        }

        if (slot.archerImage != null)
        {
            slot.archerImage.SetActive(showArcher);
        }
    }

    private void EnsureRoleImagesExist()
    {
        SlotView[] slots = GetSlotViews();
        if (!cloneMissingRoleImagesFromSlot1 || slots.Length <= 1)
        {
            return;
        }

        SlotView first = slots[0];
        if (first == null || first.slotRoot == null)
        {
            return;
        }

        GameObject templateKnight = first.knightImage != null ? first.knightImage : FindChildByKeyword(first.slotRoot.transform, "knight");
        GameObject templateArcher = first.archerImage != null ? first.archerImage : FindChildByKeyword(first.slotRoot.transform, "archer");

        for (int i = 1; i < slots.Length; i++)
        {
            SlotView slot = slots[i];
            if (slot == null || slot.slotRoot == null)
            {
                continue;
            }

            if (slot.knightImage == null && templateKnight != null)
            {
                slot.knightImage = Instantiate(templateKnight, slot.slotRoot.transform, false);
                slot.knightImage.name = $"P{i + 1}_Knight";
                slot.knightImage.SetActive(false);
            }

            if (slot.archerImage == null && templateArcher != null)
            {
                slot.archerImage = Instantiate(templateArcher, slot.slotRoot.transform, false);
                slot.archerImage.name = $"P{i + 1}_Archer";
                slot.archerImage.SetActive(false);
            }
        }
    }

    private Transform FindSlotsContainer()
    {
        GameObject byName = GameObject.Find("Player Slot");
        if (byName != null)
        {
            return byName.transform;
        }

        return null;
    }

    private GameObject FindSlotRoot(int slotIndex)
    {
        if (slotsContainer == null)
        {
            return null;
        }

        string slotName = $"Slot {slotIndex + 1}";
        Transform direct = slotsContainer.Find(slotName);
        if (direct != null)
        {
            return direct.gameObject;
        }

        string compactName = $"Slot{slotIndex + 1}";
        Transform compact = slotsContainer.Find(compactName);
        if (compact != null)
        {
            return compact.gameObject;
        }

        return null;
    }

    private GameObject FindSlotRootByIndex(int slotIndex)
    {
        if (slotsContainer == null || slotIndex < 0 || slotIndex >= slotsContainer.childCount)
        {
            return null;
        }

        Transform child = slotsContainer.GetChild(slotIndex);
        return child != null ? child.gameObject : null;
    }

    private static GameObject FindChildByKeyword(Transform root, string keyword)
    {
        if (root == null || string.IsNullOrEmpty(keyword))
        {
            return null;
        }

        string loweredKeyword = keyword.ToLowerInvariant();
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == root)
            {
                continue;
            }

            if (child.name.ToLowerInvariant().Contains(loweredKeyword))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private SlotView[] GetSlotViews()
    {
        return new[]
        {
            player1,
            player2,
            player3,
            player4,
            player5,
            player6,
        };
    }
}