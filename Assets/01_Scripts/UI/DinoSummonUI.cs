using UnityEngine;
using UnityEngine.UI;

public class DinoSummonUI : MonoBehaviour
{
    private const int SummonCost = 60;

    [SerializeField] private DinoPlacementBoard board;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private NestHealth nest;
    [Header("Scene UI")]
    [SerializeField] private Text waveText;
    [SerializeField] private Text goldText;
    [SerializeField] private Text benchText;
    [SerializeField] private Button summonButton;
    [SerializeField] private Image[] benchIcons;

    [Header("Summon Prefabs")]
    [SerializeField] private DinosaurAttack[] summonPrefabs;

    [Header("Bench Cards")]
    [SerializeField] private Sprite raptorCard;
    [SerializeField] private Sprite ankylosaurusCard;
    [SerializeField] private Sprite pterosaurCard;
    [SerializeField] private Sprite brachiosaurusCard;
    [SerializeField] private Sprite pachycephalosaurusCard;

    private int gold = 180;

    private void Start()
    {
        board.ConnectBench(benchIcons, GetBenchCard);
        summonButton.onClick.AddListener(Summon);
        spawner.GoldEarned += AddGold;
        RefreshHud();
    }

    private void Update()
    {
        RefreshHud();
    }

    private void OnDestroy()
    {
        if (summonButton != null)
            summonButton.onClick.RemoveListener(Summon);

        if (spawner != null)
            spawner.GoldEarned -= AddGold;
    }

    private void AddGold(int amount)
    {
        gold += amount;
    }

    private void RefreshHud()
    {
        if (summonButton == null)
            return;

        waveText.text = spawner.WaveStatus;
        goldText.text = $"GOLD {gold}    EGGS {nest.RemainingEggs}";
        int filled = board.BenchCount;
        benchText.text = $"DINOSAUR BENCH    {filled} / {benchIcons.Length}";
        summonButton.interactable = gold >= SummonCost && filled < benchIcons.Length && nest.HasEggs;
    }

    private void Summon()
    {
        if (gold < SummonCost || board.BenchCount >= benchIcons.Length || !nest.HasEggs || summonPrefabs == null || summonPrefabs.Length == 0)
            return;

        DinosaurAttack prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];
        DinosaurAttack dino = Instantiate(prefab);

        if (board.TryAddToBench(dino.transform))
            gold -= SummonCost;
        else
            Destroy(dino.gameObject);

        RefreshHud();
    }

    private Sprite GetBenchCard(DinosaurAttack.Species species)
    {
        return species switch
        {
            DinosaurAttack.Species.Raptor => raptorCard,
            DinosaurAttack.Species.Ankylosaurus => ankylosaurusCard,
            DinosaurAttack.Species.Pterosaur => pterosaurCard,
            DinosaurAttack.Species.Brachiosaurus => brachiosaurusCard,
            DinosaurAttack.Species.Pachycephalosaurus => pachycephalosaurusCard,
            _ => null
        };
    }
}
