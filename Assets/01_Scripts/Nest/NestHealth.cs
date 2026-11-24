using UnityEngine;

public class NestHealth : MonoBehaviour
{
    [SerializeField] private GameObject[] eggs = System.Array.Empty<GameObject>();
    [SerializeField, Min(1)] private int hitsPerEgg = 3;
    [SerializeField, Min(0f)] private float immunityAfterBreak = 1f;

    private int hitsOnCurrentEgg;
    private double immuneUntil;

    public int RemainingEggs { get; private set; }
    public bool HasEggs => RemainingEggs > 0;

    private void Awake()
    {
        RemainingEggs = eggs.Length;
    }

    public void TakeHit()
    {
        double now = Time.timeAsDouble;
        if (!HasEggs || now < immuneUntil)
            return;

        hitsOnCurrentEgg++;
        if (hitsOnCurrentEgg < Mathf.Max(1, hitsPerEgg))
            return;

        hitsOnCurrentEgg = 0;
        RemainingEggs--;
        eggs[RemainingEggs].SetActive(false);
        immuneUntil = now + immunityAfterBreak;
    }
}
