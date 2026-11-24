using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DinoPlacementBoard : MonoBehaviour
{
    [Flags]
    private enum Lane
    {
        East = 1,
        West = 2,
        North = 4,
        South = 8
    }

    [Serializable]
    private class Slot
    {
        public SpriteRenderer outline;
        public Lane lanes;
        public Transform occupant;
        public Vector2 Center => outline.transform.position;
        [NonSerialized] public Image benchIcon;
    }

    [SerializeField] private Camera boardCamera;
    [SerializeField] private Slot[] slots;
    [SerializeField, Min(0f)] private float roadStopGap = 0.24f;
    [SerializeField] private Vector2 spriteOffset = new(0f, -0.15f);

    private const float BlockerContactTolerance = 0.002f;

    private Slot[] benchSlots = Array.Empty<Slot>();
    private Func<DinosaurAttack.Species, Sprite> getBenchCard;

    public int BenchCount
    {
        get
        {
            int count = 0;
            foreach (Slot slot in benchSlots)
                if (slot.occupant != null)
                    count++;
            return count;
        }
    }

    public void ConnectBench(Image[] icons, Func<DinosaurAttack.Species, Sprite> cardLookup)
    {
        getBenchCard = cardLookup;
        benchSlots = new Slot[icons.Length];
        for (int i = 0; i < icons.Length; i++)
            benchSlots[i] = new Slot { benchIcon = icons[i] };
    }

    public bool TryAddToBench(Transform dino)
    {
        foreach (Slot slot in benchSlots)
        {
            if (slot.occupant != null)
                continue;

            slot.occupant = dino;
            SnapToSlot(slot);
            return true;
        }

        return false;
    }

    private Slot dragOrigin;
    private Transform draggedDino;
    private DinosaurAttack draggedAttack;
    private bool attackWasEnabled;
    private Pointer dragPointer;

    private void Awake()
    {
        foreach (Slot slot in slots)
            SnapToSlot(slot);
    }

    private void Update()
    {
        foreach (Slot slot in slots)
        {
            // 파괴된 Unity 오브젝트만 비우고 슬롯 색상을 복구한다.
            if (slot.occupant == null && !ReferenceEquals(slot.occupant, null))
            {
                slot.occupant = null;
                SnapToSlot(slot);
            }
        }

        if (draggedDino == null)
        {
            TryStartDrag();
            return;
        }

        Vector2 screenPosition = dragPointer.position.ReadValue();
        Vector2 pointerPosition = ScreenToWorld(screenPosition);
        if (dragPointer.press.isPressed)
        {
            draggedDino.position = new Vector3(pointerPosition.x, pointerPosition.y, draggedDino.position.z);
            return;
        }

        FinishDrag(screenPosition, pointerPosition);
    }

    private void TryStartDrag()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame)
            return;

        Vector2 screenPosition = pointer.position.ReadValue();
        Slot selectedSlot = FindBenchSlot(screenPosition) ?? FindSlot(ScreenToWorld(screenPosition));
        if (selectedSlot?.occupant == null)
            return;

        dragOrigin = selectedSlot;
        draggedDino = selectedSlot.occupant;
        dragPointer = pointer;

        // 옮기는 동안에는 아군이 적을 공격하지 않는다.
        draggedAttack = draggedDino.GetComponent<DinosaurAttack>();
        attackWasEnabled = draggedAttack != null && draggedAttack.enabled;
        if (draggedAttack != null)
            draggedAttack.enabled = false;

        if (dragOrigin.benchIcon != null)
            dragOrigin.benchIcon.enabled = false;
        Vector2 worldPosition = ScreenToWorld(screenPosition);
        draggedDino.position = new Vector3(worldPosition.x, worldPosition.y, draggedDino.position.z);
        draggedDino.gameObject.SetActive(true);
    }

    private void FinishDrag(Vector2 screenPosition, Vector2 worldPosition)
    {
        Slot destination = FindBenchSlot(screenPosition) ?? FindSlot(worldPosition);
        var sourceUnit = draggedDino.GetComponent<DinosaurAttack>();
        var targetUnit = destination?.occupant != null
            ? destination.occupant.GetComponent<DinosaurAttack>()
            : null;

        if (destination != dragOrigin && sourceUnit != null && targetUnit != null && targetUnit.TryMerge(sourceUnit))
        {
            dragOrigin.occupant = null;
            draggedDino.gameObject.SetActive(false);
            Destroy(draggedDino.gameObject);
            SnapToSlot(dragOrigin);
            SnapToSlot(destination);
        }
        else
        {
            if (destination == null || destination.occupant != null)
                destination = dragOrigin;

            dragOrigin.occupant = null;
            destination.occupant = draggedDino;
            SnapToSlot(dragOrigin);
            if (destination != dragOrigin)
                SnapToSlot(destination);

            if (draggedAttack != null)
                draggedAttack.enabled = destination.benchIcon == null &&
                    (attackWasEnabled || dragOrigin.benchIcon != null);
        }

        draggedDino = null;
        draggedAttack = null;
        dragOrigin = null;
        dragPointer = null;
    }

    private Slot FindBenchSlot(Vector2 screenPosition)
    {
        foreach (Slot slot in benchSlots)
            if (RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)slot.benchIcon.transform.parent, screenPosition, boardCamera))
                return slot;

        return null;
    }

    private Vector2 ScreenToWorld(Vector2 screen)
    {
        return boardCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -boardCamera.transform.position.z));
    }

    private Slot FindSlot(Vector2 position)
    {
        foreach (Slot slot in slots)
        {
            Vector3 point = new(position.x, position.y, slot.outline.transform.position.z);
            if (slot.outline.bounds.Contains(point))
                return slot;
        }

        return null;
    }

    private void SnapToSlot(Slot slot)
    {
        if (slot.benchIcon != null)
        {
            var emptyColor = new Color32(226, 237, 209, 255);
            var background = slot.benchIcon.transform.parent.GetComponent<Image>();
            SpriteRenderer art = slot.occupant == null ? null : slot.occupant.GetComponent<SpriteRenderer>();
            slot.benchIcon.enabled = art != null;
            background.color = emptyColor;

            if (art != null)
            {
                var dino = slot.occupant.GetComponent<DinosaurAttack>();
                Sprite card = dino != null ? getBenchCard?.Invoke(dino.Kind) : null;
                slot.benchIcon.sprite = card != null ? card : art.sprite;
                slot.benchIcon.color = card != null ? Color.white : art.color;
                if (art.color != Color.white)
                    background.color = art.color;

                slot.occupant.gameObject.SetActive(false);
            }

            return;
        }

        if (slot.occupant != null)
        {
            Vector2 position = slot.Center + spriteOffset;
            slot.occupant.position = new Vector3(position.x, position.y, slot.occupant.position.z);
        }

        slot.outline.color = slot.occupant == null
            ? new Color32(87, 136, 69, 170)
            : new Color32(25, 112, 165, 255);
    }

    public float LimitStep(Vector2 enemyPosition, Vector2 directionToNest, float maxStep,
        out DinosaurAttack blocker)
    {
        blocker = null;
        float nearestStop = float.PositiveInfinity;
        Lane incomingLane = GetIncomingLane(directionToNest);

        foreach (Slot slot in slots)
        {
            if ((slot.lanes & incomingLane) == 0 || slot.occupant == null)
                continue;

            if (slot.occupant == draggedDino || !slot.occupant.gameObject.activeInHierarchy)
                continue;

            // 옆 칸의 중심을 도로에 투영해 적 앞에 있는 정지 지점을 구한다.
            float distanceToSlotAlongRoad = Vector2.Dot(slot.Center - enemyPosition, directionToNest);
            if (distanceToSlotAlongRoad < 0f)
                continue;

            float distanceToStop = Mathf.Max(0f, distanceToSlotAlongRoad - roadStopGap);
            if (distanceToStop > maxStep + BlockerContactTolerance || distanceToSlotAlongRoad >= nearestStop ||
                !slot.occupant.TryGetComponent(out DinosaurAttack dino) || dino.CurrentHealth <= 0)
                continue;

            nearestStop = distanceToSlotAlongRoad;
            maxStep = Mathf.Min(maxStep, distanceToStop);
            blocker = dino;
        }

        return maxStep;
    }

    private static Lane GetIncomingLane(Vector2 directionToNest)
    {
        // 왼쪽으로 움직이는 적은 오른쪽 길에서 들어온다.
        if (Mathf.Abs(directionToNest.x) >= Mathf.Abs(directionToNest.y))
            return directionToNest.x < 0f ? Lane.East : Lane.West;

        return directionToNest.y < 0f ? Lane.North : Lane.South;
    }
}
