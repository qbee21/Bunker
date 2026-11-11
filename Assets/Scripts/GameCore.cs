using System.Collections;
using Action = System.Action;
using UnityEngine;
using UnityEngine.UI;
using PlayerPrefs = UnityEngine.PlayerPrefs;

[System.Serializable]
public class EntityRang
{
    public EntityData[] entity;
}

public class GameCore : MonoBehaviour
{
    public static GameCore instance;
    public int level;
    public int enemyLevel;
    public Camera cam;
    public GameObject map;
    public GameObject pointTower;
    public GameObject pointEnemyTower;
    public EntityRang[] entitys;
    public Sprite[] baseSkin;
    public GameObject entityPrefab;
    public float food;
    public float speedFill;
    public Image fillFoodPanel;
    public Text foodView;
    public Spawner spawner;
    public Button[] card;
    [Tooltip("Assign the saved AlliedUnitLimitNotice Text from the Battle panel. Never created at runtime.")]
    public Text alliedLimitNotice;
	public Text levelView;
	public HideObject ho;
    public GameBalance balance;
    public EconomyCore economy;
    public LoadoutBoard board;
    public WaveManager waveManager;
    public PlatformBridge platform;
    public GameSound sound;
    public Tower playerTower;
    public Tower enemyTower;
    public CoinPickup coinPrefab;
    public GameObject menuPanel;
    public GameObject prepPanel;
    public GameObject battlePanel;
    public GameObject resultPanel;
    public GameObject pausePanel;
    public Text resultView;
    public Text statusView;
    public Text noticeView;
    public Text[] unitViews;
    public int wave = 1;
    public bool inBattle;
    public bool paused;
    public bool initialized { get; private set; }
    BunkerPlatformUI platformUI;
    float foodTimer;
    float noticeTimer;
    float uiTimer;
    [Tooltip("Additional non-battle UI outside the standard panels. Hidden before battle starts.")]
    public GameObject[] extraNonBattleUI;
    Image[] viewportBorders;
    bool finishing;
    bool lastVictory;
    int lastFinishedWave;
    int lastBonus;

    void Awake() { instance = this; EraContentCatalog.Install(this); }

    IEnumerator Start()
    {
        yield return null;
        while (!platform.DataReady) yield return null;
        economy.Load();
        platform.RestoreProgress();
        LocalText.english = PlayerPrefs.GetString("BunkerLanguage", Application.systemLanguage == SystemLanguage.Russian ? "ru" : "en") == "en";
        sound.muted = PlayerPrefs.GetInt("BunkerMuted", 0) == 1;
        wave = economy.data.completedWave + 1;
        level = Mathf.Clamp(level, 0, entitys.Length - 1);
        enemyLevel = Mathf.Clamp(enemyLevel, 0, entitys.Length - 1);
        platform.ApplyEnvironmentLanguage();
        board.Load();
        ApplyEra();
        ShowMenu();
        CreateViewportGap();
        RefreshLanguage();
        platformUI = GetComponent<BunkerPlatformUI>();
        if (platformUI == null) platformUI = gameObject.AddComponent<BunkerPlatformUI>();
        platformUI.Initialize(this);
        initialized = true;
        platform.CompleteInitialization();
        economy.Save();
    }

    void Update()
    {
        if (!initialized) return;
        noticeTimer -= Time.unscaledDeltaTime;
        if (noticeTimer <= 0) noticeView.text = "";
        if (!inBattle || paused || finishing) return;
        foodTimer += Time.deltaTime;
        while (foodTimer >= economy.FoodInterval())
        {
            foodTimer -= economy.FoodInterval();
            food = Mathf.Min(economy.FoodCapacity(), food + 1);
        }
        if (playerTower.combatState.alive && waveManager.Cleared() && (!waveManager.hasBunker || !enemyTower.combatState.alive)) Finish(true);
        uiTimer -= Time.deltaTime;
	    if (uiTimer <= 0) { uiTimer = 0.05f; RefreshBattle(); }
    }

    public void ShowMenu()
    {
        if (LeaveResultWithAd(ShowMenu)) return;
        ApplyEra();
        menuPanel.SetActive(true);
        prepPanel.SetActive(false);
        battlePanel.SetActive(false);
        resultPanel.SetActive(false);
        pausePanel.SetActive(false);
        economy.ReloadData();
        RefreshBattle();
    }

    public void Prepare()
    {
        if (LeaveResultWithAd(Prepare)) return;
        if (inBattle) return;
        if (!platform.CanPlay()) { Message(LocalText.Pick("Подключение SDK…", "Connecting SDK…")); return; }
        if (!waveManager.CanStart(wave)) { ShowMenu(); Message(LocalText.Pick("Сюжетная кампания пройдена!", "Story campaign completed!")); return; }
        menuPanel.SetActive(false);
        resultPanel.SetActive(false);
        prepPanel.SetActive(true);
        ApplyEra();
        board.Prepare();
        RefreshBattle();
        sound.PlayClick();
    }

    public void BeginBattle()
    {
        if (!waveManager.CanStart(wave)) { ShowMenu(); Message(LocalText.Pick("Сюжетная кампания пройдена!", "Story campaign completed!")); return; }
        if (!platform.CanPlay() || inBattle || !board.Armed()) { Message(LocalText.Pick("Сначала размести хотя бы одно оружие", "Place at least one weapon first")); return; }
        board.Save();
        ApplyEra();
        ClearField();
	    if (board.visuals != null) board.visuals.EndDrag();
        menuPanel.SetActive(false);
        prepPanel.SetActive(false);
        resultPanel.SetActive(false);
        pausePanel.SetActive(false);
        battlePanel.SetActive(false);
        if (extraNonBattleUI != null)
            foreach (GameObject panel in extraNonBattleUI)
                if (panel != null) panel.SetActive(false);
        paused = false;
        platform.SyncPause();
        Canvas.ForceUpdateCanvases();
        UpdateViewportGap();
        inBattle = true;
        finishing = false;
        food = economy.StartingFood();
        foodTimer = 0;
        playerTower.ResetTower(economy.BunkerHealth(), true);
        waveManager.Begin(wave);
        enemyTower.ResetTower(waveManager.bunkerHealth, waveManager.hasBunker);
        battlePanel.SetActive(true);
        platform.Gameplay(true);
        RefreshBattle();
    }

    public void Spawn(int index)
    {
        if (!inBattle || paused || index < 0 || index >= 3) return;
        int count = AliveAlliedUnits();
        if (count >= Mathf.Max(1, balance.maxAlliedUnits)) { Message(LocalText.Pick("Лимит защитников: ", "Defender limit: ") + balance.maxAlliedUnits); return; }
        float price = entitys[level].entity[index].price;
        if (food < price) return;
        food -= price;
        spawner.SpawnWithIndex(index);
        RefreshBattle();
    }

    // Kept as a serialized UnityEvent target for old scenes. iOS has no rewarded ads.
    public void RewardFood() { }

    public void UnitDied(CombatEntity unit)
    {
        if (!inBattle || finishing) return;
        RefreshAlliedLimitUI();
        if (unit.isTower)
        {
            if (!unit.isEnemy) Finish(false);
            else waveManager.OnEnemyBunkerDestroyed();
            return;
        }
        if (unit.isEnemy)
        {
            waveManager.EnemyDied();
            if (unit.coins >= 10 || Random.value <= balance.coinChance)
            {
                CoinPickup coin = Instantiate(coinPrefab, unit.transform.position, Quaternion.identity, map.transform);
                coin.value = unit.coins;
            }
        }
    }

    void Finish(bool victory)
    {
        if (finishing) return;
        finishing = true;
        inBattle = false;
        RefreshAlliedLimitUI();
        waveManager.Stop();
	    CollectCoins();
	    ho.isBattle = false;
        int bonus = 0;
        int finishedWave = wave;
        if (victory)
        {
            bonus = waveManager.victoryCoins;
            economy.AddCoin(bonus);
            if (wave > economy.data.completedWave) board.GrantWaveSlots();
            economy.data.completedWave = Mathf.Max(economy.data.completedWave, wave);
            platform.Score(economy.data.completedWave);
            wave++;
            sound.PlayWin();
        }
        if (!victory && waveManager.killed >= Mathf.Max(1, balance.defeatMinimumKills)
            && waveManager.BattleSeconds >= balance.defeatMinimumSeconds)
        {
            bonus = Mathf.Max(0, balance.defeatCoins + balance.Era(wave) * balance.defeatCoinsPerEra);
            economy.AddCoin(bonus);
        }
        economy.Save();
        platform.Gameplay(false);
        battlePanel.SetActive(false);
        resultPanel.SetActive(true);
        lastVictory = victory;
        lastFinishedWave = finishedWave;
        lastBonus = bonus;
        RefreshResult();
        ClearField();
    }

    void RefreshResult()
    {
        if (lastVictory && lastFinishedWave >= Mathf.Max(1, balance.campaignWaves))
        {
            resultView.text = LocalText.Pick("Кампания пройдена!\nВсе 10 эр и 50 волн позади.", "Campaign completed!\nAll 10 eras and 50 waves cleared.")
                + "\n+" + lastBonus + LocalText.Pick(" монет", " coins");
            return;
        }
        resultView.text = lastVictory ? LocalText.Pick("Волна пройдена!", "Wave cleared!") + "\n" + lastFinishedWave + "\n+" + lastBonus + LocalText.Pick(" монет", " coins") : LocalText.Pick("Бункер разрушен\nУлучшения и собранные монеты сохранены.\nПовтори волну ", "Bunker destroyed\nUpgrades and collected coins are saved.\nRetry wave ") + wave + (lastBonus > 0 ? "\n+" + lastBonus + LocalText.Pick(" монет за бой", " battle coins") : "");
    }

    public void CollectCoins()
    {
        CoinPickup[] coins = map.GetComponentsInChildren<CoinPickup>();
        foreach (CoinPickup coin in coins) coin.Collect();
    }

    void ClearField()
    {
        Entity[] units = map.GetComponentsInChildren<Entity>();
        foreach (Entity unit in units) { unit.combatState.alive = false; unit.gameObject.SetActive(false); Destroy(unit.gameObject); }
        Projectile[] shots = map.GetComponentsInChildren<Projectile>();
        foreach (Projectile shot in shots) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
    }

    public void Pause()
    {
        if (!inBattle) return;
        paused = !paused;
        pausePanel.SetActive(paused);
        platform.SyncPause();
        platform.Gameplay(!paused);
    }

    public void Retreat()
    {
        paused = false;
        platform.SyncPause();
        pausePanel.SetActive(false);
        Finish(false);
    }

    public void ToggleLanguage() { SetLanguage(!LocalText.english); }
    public void SetLanguage(bool english)
    {
        LocalText.english = english;
        PlayerPrefs.SetString("BunkerLanguage", english ? "en" : "ru");
        PlayerPrefs.Save();
        RefreshLanguage();
    }

    void RefreshLanguage()
    {
        LocalText[] texts = GetComponentsInChildren<LocalText>(true);
        foreach (LocalText text in texts) text.Refresh();
        LocalText[] sceneTexts = menuPanel.transform.root.GetComponentsInChildren<LocalText>(true);
        foreach (LocalText text in sceneTexts) text.Refresh();
        economy.ReloadData();
        board.Refresh();
        if (resultPanel.activeSelf) RefreshResult();
        RefreshBattle();
    }

    public int AliveAlliedUnits()
    {
        int count = 0;
        foreach (CombatEntity unit in CombatEntity.all)
            if (unit != null && unit.alive && !unit.isEnemy && !unit.isTower) count++;
        return count;
    }

    void RefreshAlliedLimitUI(int count = -1)
    {
        // If not wired yet, keep the buttons visible rather than hide all recruitment UI.
        if (alliedLimitNotice == null) return;
        if (count < 0) count = AliveAlliedUnits();
        int limit = Mathf.Max(1, balance.maxAlliedUnits);
        bool atLimit = inBattle && count >= limit;
        alliedLimitNotice.gameObject.SetActive(atLimit);
        if (atLimit)
            alliedLimitNotice.text = LocalText.Pick("Достигнут лимит союзников", "Ally limit reached");
        if (card != null)
            foreach (Button button in card)
                if (button != null) button.gameObject.SetActive(!atLimit);
    }

    void RefreshBattle()
    {
        int alliedCount = AliveAlliedUnits();
        RefreshAlliedLimitUI(alliedCount);
        foodView.text = LocalText.Pick("Еда: ", "Food: ") + Mathf.FloorToInt(food) + "/" + economy.FoodCapacity();
        fillFoodPanel.fillAmount = foodTimer / economy.FoodInterval();
	    levelView.text = LocalText.Pick("Волна ", "Wave ") + wave;
	    statusView.text = LocalText.Pick("Врагов: ", "Enemies: ") + (waveManager.remaining + waveManager.alive) + (waveManager.hasBunker ? LocalText.Pick(" + бункер", " + bunker") : "");
        string[] ru = { "Пехота", "Метатель", "Тяжёлый" };
        string[] en = { "Infantry", "Thrower", "Heavy" };
        for (int i = 0; i < 3; i++)
        {
            EntityData data = entitys[level].entity[i];
            unitViews[i].text = (LocalText.english ? en[i] : ru[i]) + "\n" + data.price + LocalText.Pick(" еды", " food");
            card[i].interactable = inBattle && !paused && food >= data.price && alliedCount < Mathf.Max(1, balance.maxAlliedUnits);
        }
    }

    public void Message(string text) { noticeView.text = text; noticeTimer = 4; }
    void ApplyEra()
    {
        level = Mathf.Clamp(balance.Era(wave), 0, entitys.Length - 1);
        enemyLevel = level;
        int skinIndex = Mathf.Min(level, baseSkin.Length - 1);
        playerTower.bunkerSkin.sprite = baseSkin[skinIndex];
        enemyTower.bunkerSkin.sprite = baseSkin[skinIndex];
        for (int i = 0; i < card.Length; i++)
        {
            Transform portrait = card[i].transform.Find("Portrait");
            if (portrait != null) portrait.GetComponent<Image>().sprite = entitys[level].entity[i].skin;
        }
    }

    // Opaque borders repaint ALL pixels outside the partial camera viewport.
    // They sit behind existing UI, retaining the scene's menu background colour.
    void CreateViewportGap()
    {
        Canvas canvas = levelView.canvas;
        if (canvas == null || cam == null) return;
        canvas = canvas.rootCanvas;
        Color color = new Color(0.18f, 0.13f, 0.12f, 1);
        Image background = menuPanel.GetComponent<Image>();
        if (background != null) color = background.color;
        color.a = 1;
        viewportBorders = new Image[4];
        for (int i = 0; i < viewportBorders.Length; i++)
        {
            GameObject border = new GameObject("ViewportBorder" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            border.transform.SetParent(canvas.transform, false);
            border.transform.SetAsFirstSibling();
            viewportBorders[i] = border.GetComponent<Image>();
            viewportBorders[i].color = color;
            viewportBorders[i].raycastTarget = false;
        }
        Canvas.willRenderCanvases += UpdateViewportGap;
        UpdateViewportGap();
    }

    void LateUpdate() { UpdateViewportGap(); }
    void OnDestroy() { Canvas.willRenderCanvases -= UpdateViewportGap; }

    void UpdateViewportGap()
    {
        if (viewportBorders == null || cam == null) return;
        Rect r = cam.rect;
        SetViewportBorder(0, new Vector2(0, 0), new Vector2(r.xMin, 1));
        SetViewportBorder(1, new Vector2(r.xMax, 0), new Vector2(1, 1));
        SetViewportBorder(2, new Vector2(r.xMin, 0), new Vector2(r.xMax, r.yMin));
        SetViewportBorder(3, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, 1));
    }

    void SetViewportBorder(int index, Vector2 min, Vector2 max)
    {
        Image image = viewportBorders[index];
        if (image == null) return;
        image.enabled = max.x > min.x && max.y > min.y;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    bool LeaveResultWithAd(Action next)
    {
        if (resultPanel == null || !resultPanel.activeSelf) return false;
        resultPanel.SetActive(false);
        next();
        return true;
    }

    // Compatibility targets for existing UnityEvents. Monetized rewards are removed.
    public bool CanRewardMenuCoins() { return false; }
    public void RewardMenuCoins() { }
    public void ReloadCloudProgress()
    {
        if (!initialized || inBattle) return;
        wave = economy.data.completedWave + 1;
        board.Load();
        ApplyEra();
        RefreshLanguage();
    }
    void OnApplicationPause(bool pause) { if (pause && initialized) economy.Save(); }
    void OnApplicationQuit() { if (initialized) economy.Save(); }
}
