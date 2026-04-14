using System;
using Fusion;
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
    [SerializeField] private bool cloneMissingRoleImagesFromSlot1 = true;
    [SerializeField] private bool hideUnusedSlots = true;

    [Header("Fallback")]
    [SerializeField] private SharedPlayerClassType fallbackClass = SharedPlayerClassType.Unknown;

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

            if (slot.slotRoot == null && autoFindBySlotNames)
            {
                slot.slotRoot = FindSlotRoot(i);
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
            ApplyOfflineVisualState();
            return;
        }

        var players = sessionManager.GetPlayersOrderedById();

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

            SharedPlayerClassType classType = sessionManager.GetPlayerClass(players[i], SharedPlayerClassType.Unknown);
            if (classType == SharedPlayerClassType.Unknown)
            {
                SetRoleVisible(slot, SharedPlayerClassType.Unknown);
                continue;
            }

            SetRoleVisible(slot, classType);
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

        Transform recursive = FindChildByExactName(slotsContainer, slotName);
        if (recursive != null)
        {
            return recursive.gameObject;
        }

        recursive = FindChildByExactName(slotsContainer, compactName);
        if (recursive != null)
        {
            return recursive.gameObject;
        }

        return null;
    }

    private static Transform FindChildByExactName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || child == root)
            {
                continue;
            }

            if (string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
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