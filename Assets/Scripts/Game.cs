using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

static class Ease
{
    public static float OutQuad(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
    public static float OutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}

[DefaultExecutionOrder(-100)]
public class Game : MonoBehaviour
{
    public static Game I;
    public enum State { Start, Play, Pause, Over }
    public State state = State.Start;
    public int lives = 3, score, best;
    public PlayerShip player;
    const int MaxLives = 6;
    Transform world;
    Camera cam;
    Font font;
    Sprite playerSpr, bulletSpr, eBulletSpr, tankShellSpr, tankSpr, hitSpr;
    Sprite lifeSpr, shieldSpr, bombSpr, btnSpr, panelSpr;
    Sprite[] planeSpr, bgSpr, digits;
    Text shieldTxt, overTxt;
    Image lifeNum, flash;
    Image[] scoreNum;
    GameObject startPanel, pausePanel, overPanel, hud;
    int kicks;
    float aged;
    AudioSource src;
    AudioClip sShot, sBoom, sGet, sHurt, sBomb;

    float planeCd, tankCd, propCd;
    float W, H;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindAnyObjectByType<Game>()) return;
        new GameObject("Game").AddComponent<Game>();
    }

    void OnDestroy()
    {
        if (I == this) I = null;
    }

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        best = PlayerPrefs.GetInt("best", 0);
        cam = Camera.main;
        if (!cam)
        {
            Debug.LogError("No Main Camera");
            return;
        }
        cam.orthographic = true;
        cam.orthographicSize = 9;
        cam.backgroundColor = new Color(0.32f, 0.52f, 0.28f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        H = cam.orthographicSize;
        W = H * cam.aspect;

        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 28);
        playerSpr = Res("Art/player/ship_0002");
        bulletSpr = Res("Art/bullet/tile_0000");
        eBulletSpr = Res("Art/bullet/tile_0000_red");
        tankShellSpr = Res("Art/enemy/tile_0030");
        tankSpr = Res("Art/enemy/tile_0028");
        hitSpr = Res("Art/bullet/tile_0004");
        lifeSpr = Res("Art/prop/tile_0024");
        bombSpr = Res("Art/prop/tile_0025");
        shieldSpr = Res("Art/prop/tile_0026");
        btnSpr = Res("Art/ui/btn-normal");
        panelSpr = Res("Art/ui/panel-normal");
        planeSpr = new[] { Res("Art/enemy/ship_0001"), Res("Art/enemy/ship_0005"), Res("Art/enemy/ship_0009") };
        bgSpr = new[] { Res("Art/map/tile_0036"), Res("Art/map/tile_0048"), Res("Art/map/tile_0060"), Res("Art/map/tile_0084") };
        digits = new[]
        {
            Res("Art/fontclip/tile_0019"), Res("Art/fontclip/tile_0020"), Res("Art/fontclip/tile_0021"),
            Res("Art/fontclip/tile_0022"), Res("Art/fontclip/tile_0023"), Res("Art/fontclip/tile_0031"),
            Res("Art/fontclip/tile_0032"), Res("Art/fontclip/tile_0033"), Res("Art/fontclip/tile_0034"),
            Res("Art/fontclip/tile_0035")
        };

        world = new GameObject("World").transform;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        sShot = Beep(920, 0.045f, 0.18f);
        sBoom = Noise(0.14f, 0.4f);
        sGet = Beep(1320, 0.1f, 0.28f);
        sHurt = Beep(180, 0.16f, 0.4f);
        sBomb = Noise(0.28f, 0.55f);
        BuildBg();
        BuildUi();
        EnsureEventSystem();
        Time.timeScale = 1;
        Show(startPanel);
    }

    static Sprite Res(string p)
    {
        return Resources.Load<Sprite>(p);
    } 

    void Update()
    {
        W = H * cam.aspect;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (state == State.Play) Pause(true);
            else if (state == State.Pause) Pause(false);
        }
        if (state != State.Play) return;

        aged += Time.deltaTime;
        float u = Mathf.Clamp01(aged / 80f);
        planeCd -= Time.deltaTime;
        tankCd -= Time.deltaTime;
        propCd -= Time.deltaTime;
        if (planeCd <= 0) { SpawnPlane(); planeCd = Mathf.Lerp(1.15f, 0.5f, u); }
        if (tankCd <= 0) { SpawnTank(); tankCd = Mathf.Lerp(3.8f, 2.1f, u); }
        if (propCd <= 0) { SpawnProp(); propCd = 8f; }
        ScrollBg();
    }

    public void StartRun()
    {
        ClearWorld();
        BuildBg();
        lives = 3;
        score = 0;
        aged = 0;
        planeCd = 0.4f;
        tankCd = 2.2f;
        propCd = 5f;
        player = Spawn(playerSpr, new Vector3(0, -H + 1.2f, 0), 0.95f).AddComponent<PlayerShip>();
        player.Init(bulletSpr, shieldSpr);
        SetShieldHud(0);
        Time.timeScale = 1;
        AudioListener.pause = false;
        kicks = 0;
        if (cam) cam.transform.position = new Vector3(0, 0, cam.transform.position.z);
        state = State.Play;
        Show(hud);
        RefreshHud();
    }

    public void Pause(bool on)
    {
        state = on ? State.Pause : State.Play;
        Time.timeScale = on ? 0 : 1;
        AudioListener.pause = on;
        pausePanel.SetActive(on);
    }

    public void AddScore(int n, Vector3 pos)
    {
        score += n;
        RefreshHud();
        FloatScore(n, pos);
    }

    public void LoseLife()
    {
        lives--;
        RefreshHud();
        if (lives <= 0) Die();
    }

    public void AddLife()
    {
        lives = Mathf.Min(MaxLives, lives + 1);
        RefreshHud();
        SfxGet();
    }

    public void Bomb()
    {
        foreach (var e in FindObjectsByType<EnemyCraft>(FindObjectsSortMode.None))
            e.Kill();
        foreach (var s in FindObjectsByType<Shot>(FindObjectsSortMode.None))
        {
            if (s.fromPlayer) continue;
            Fx(s.transform.position);
            Destroy(s.gameObject);
        }
        Kick(0.09f, 0.22f);
        FlashWhite();
        SfxBomb();
    }

    public void Kick(float freeze, float mag)
    {
        if (state != State.Play || !cam) return;
        StartCoroutine(KickCo(freeze, mag));
    }

    IEnumerator KickCo(float freeze, float mag)
    {
        kicks++;
        var home = new Vector3(0, 0, cam.transform.position.z);
        Time.timeScale = 0;
        float t = 0;
        while (t < freeze && state == State.Play && cam)
        {
            t += Time.unscaledDeltaTime;
            cam.transform.position = home + (Vector3)(Random.insideUnitCircle * mag);
            yield return null;
        }
        kicks--;
        if (kicks <= 0 && cam) cam.transform.position = home;
        if (state == State.Play && kicks <= 0) Time.timeScale = 1;
    }

    void FlashWhite()
    {
        if (flash) StartCoroutine(FlashCo());
    }

    IEnumerator FlashCo()
    {
        float t = 0;
        const float d = 0.2f;
        while (t < d && flash)
        {
            t += Time.unscaledDeltaTime;
            flash.color = new Color(1f, 1f, 1f, 0.55f * (1f - Ease.OutQuad(t / d)));
            yield return null;
        }
        if (flash) flash.color = new Color(1f, 1f, 1f, 0);
    }

    void FloatScore(int n, Vector3 pos)
    {
        if (n <= 0 || digits == null) return;
        var root = new GameObject("pts");
        root.transform.SetParent(world);
        root.transform.position = pos + Vector3.up * 0.35f;
        var s = n.ToString();
        for (int i = 0; i < s.Length; i++)
        {
            int d = s[i] - '0';
            if (d < 0 || d > 9) continue;
            var go = new GameObject("d");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3((i - (s.Length - 1) * 0.5f) * 0.38f, 0, 0);
            go.transform.localScale = Vector3.one * 0.5f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = digits[d];
            sr.sortingOrder = 8;
            sr.color = new Color(1f, 0.92f, 0.35f);
        }
        StartCoroutine(FloatPts(root));
    }

    IEnumerator FloatPts(GameObject root)
    {
        float t = 0;
        const float d = 0.55f;
        var p0 = root.transform.position;
        var srs = root.GetComponentsInChildren<SpriteRenderer>();
        while (t < d && root)
        {
            t += Time.deltaTime;
            float u = Ease.OutQuad(t / d);
            root.transform.position = p0 + Vector3.up * (0.9f * u);
            var c = new Color(1f, 0.92f, 0.35f, 1f - u);
            foreach (var sr in srs) if (sr) sr.color = c;
            yield return null;
        }
        if (root) Destroy(root);
    }

    public void SetShieldHud(float t)
    {
        if (!shieldTxt) return;
        if (t > 0)
        {
            shieldTxt.text = $"护盾 {t:0.0}s";
            shieldTxt.color = t > 1f
                ? new Color(0.45f, 0.9f, 1f)
                : Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(1f, 0.9f, 0.3f), Mathf.PingPong(Time.unscaledTime * 8f, 1f));
        }
        else
        {
            shieldTxt.text = "护盾 关闭";
            shieldTxt.color = new Color(0.75f, 0.75f, 0.75f);
        }
    }

    public void ShieldBroke()
    {
        if (!shieldTxt) return;
        shieldTxt.text = "护盾 消失!";
        shieldTxt.color = new Color(1f, 0.25f, 0.2f);
        StartCoroutine(ShieldHudFlash());
    }

    IEnumerator ShieldHudFlash()
    {
        for (int i = 0; i < 6; i++)
        {
            if (!shieldTxt) yield break;
            shieldTxt.color = i % 2 == 0 ? new Color(1f, 0.2f, 0.2f) : Color.white;
            shieldTxt.transform.localScale = Vector3.one * (i % 2 == 0 ? 1.15f : 1f);
            yield return new WaitForSeconds(0.1f);
        }
        if (shieldTxt) shieldTxt.transform.localScale = Vector3.one;
        SetShieldHud(0);
    }

    void Die()
    {
        state = State.Over;
        Time.timeScale = 0;
        if (score > best) { best = score; PlayerPrefs.SetInt("best", best); }
        overTxt.text = $"游戏结束\n分数 {score}\n最高 {best}";
        Show(overPanel);
        hud.SetActive(true);
    }

    void ClearWorld()
    {
        if (!world) return;
        for (int i = world.childCount - 1; i >= 0; i--)
            DestroyImmediate(world.GetChild(i).gameObject);
        player = null;
    }

    void SpawnPlane()
    {
        var spr = planeSpr[Random.Range(0, planeSpr.Length)];
        float u = Mathf.Clamp01(aged / 80f);
        var go = Spawn(spr, new Vector3(Random.Range(-W + 0.6f, W - 0.6f), H + 1f, 0), 0.85f);
        go.AddComponent<EnemyCraft>().Init(EnemyCraft.Kind.Plane, eBulletSpr, 1f + 0.5f * u);
    }

    void SpawnTank()
    {
        int n = 0;
        foreach (var e in FindObjectsByType<EnemyCraft>(FindObjectsSortMode.None))
            if (e.IsTank) n++;
        if (n >= 3) return;
        var go = Spawn(tankSpr, new Vector3(Random.Range(-W + 0.8f, W - 0.8f), Random.Range(1.2f, H - 1.6f), 0), 1.05f);
        go.AddComponent<EnemyCraft>().Init(EnemyCraft.Kind.Tank, tankShellSpr, 1f + 0.35f * Mathf.Clamp01(aged / 80f));
    }

    void SpawnProp()
    {
        int k = Random.Range(0, 3);
        var spr = k == 0 ? lifeSpr : k == 1 ? shieldSpr : bombSpr;
        var go = Spawn(spr, new Vector3(Random.Range(-W + 0.6f, W - 0.6f), H + 1f, 0), 0.75f);
        go.AddComponent<Drop>().kind = (Drop.Kind)k;
    }

    public GameObject Spawn(Sprite spr, Vector3 pos, float scale, bool intro = true)
    {
        var go = new GameObject(spr ? spr.name : "obj");
        go.transform.SetParent(world);
        go.transform.position = pos;
        go.transform.localScale = intro ? Vector3.zero : Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 2;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (intro) StartCoroutine(PopIn(go.transform, scale));
        return go;
    }

    IEnumerator PopIn(Transform tr, float scale)
    {
        float t = 0;
        while (t < 0.22f && tr)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.one * scale * Ease.OutBack(t / 0.22f);
            yield return null;
        }
        if (tr) tr.localScale = Vector3.one * scale;
    }

    public void Fx(Vector3 pos)
    {
        var go = Spawn(hitSpr, pos, 0.55f, false);
        Destroy(go.GetComponent<Collider2D>());
        Destroy(go.GetComponent<Rigidbody2D>());
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        StartCoroutine(Boom(go, sr, 0.55f));
    }

    IEnumerator Boom(GameObject go, SpriteRenderer sr, float scale)
    {
        float t = 0;
        const float d = 0.32f;
        while (t < d && go)
        {
            t += Time.deltaTime;
            float u = t / d;
            go.transform.localScale = Vector3.one * scale * (0.3f + 1.5f * Ease.OutBack(Mathf.Min(1f, u * 1.2f)));
            go.transform.Rotate(0, 0, 420f * Time.deltaTime);
            if (sr) sr.color = new Color(1f, 0.85f, 0.4f, 1f - Ease.OutQuad(u));
            yield return null;
        }
        if (go) Destroy(go);
    }

    Transform bgA, bgB;
    const float BgH = 20f;

    void BuildBg()
    {
        bgA = MakeBg(0);
        bgB = MakeBg(BgH);
    }

    Transform MakeBg(float y)
    {
        var go = new GameObject("bg");
        go.transform.SetParent(world);
        go.transform.position = new Vector3(0, y, 0);
        for (int i = 0; i < 14; i++)
        {
            var spr = bgSpr[Random.Range(0, bgSpr.Length)];
            if (!spr) continue;
            var deco = new GameObject("deco");
            deco.transform.SetParent(go.transform, false);
            deco.transform.localPosition = new Vector3(Random.Range(-W, W), Random.Range(-BgH * 0.5f, BgH * 0.5f), 0);
            deco.transform.localScale = Vector3.one * Random.Range(0.7f, 1.1f);
            var sr = deco.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = -8;
        }
        return go.transform;
    }

    void ScrollBg()
    {
        if (!bgA) return;
        float dy = 1.6f * Time.deltaTime;
        bgA.position += Vector3.down * dy;
        bgB.position += Vector3.down * dy;
        if (bgA.position.y < -BgH) bgA.position += Vector3.up * BgH * 2;
        if (bgB.position.y < -BgH) bgB.position += Vector3.up * BgH * 2;
    }

    void BuildUi()
    {
        var canvasGo = NewUi("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;

        hud = NewUi("HUD");
        hud.transform.SetParent(root, false);
        Stretch(hud);
        var bar = NewUi("bar");
        bar.transform.SetParent(hud.transform, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0, 1);
        barRt.anchorMax = new Vector2(1, 1);
        barRt.pivot = new Vector2(0.5f, 1);
        barRt.sizeDelta = new Vector2(0, 220);
        barRt.anchoredPosition = Vector2.zero;
        var barImg = bar.AddComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0.55f);
        scoreNum = ScoreHud(hud.transform, new Vector2(36, -24));
        lifeNum = LifeHud(hud.transform, new Vector2(36, -114));
        shieldTxt = Chip(hud.transform, shieldSpr, "护盾 关闭", new Vector2(250, -114), new Color(0.45f, 0.9f, 1f));
        Btn(hud.transform, "暂停", new Vector2(-28, -28), new Vector2(220, 100), TextAnchor.UpperRight, () => Pause(true), new Color(1f, 0.78f, 0.25f));
        hud.SetActive(false);

        var flashGo = NewUi("flash");
        flashGo.transform.SetParent(root, false);
        Stretch(flashGo);
        flash = flashGo.AddComponent<Image>();
        flash.color = new Color(1f, 1f, 1f, 0);
        flash.raycastTarget = false;

        startPanel = Panel(root, "start");
        Label(startPanel.transform, "像素飞机大战", new Vector2(0, 260), 86, TextAnchor.MiddleCenter, true, new Color(1f, 0.92f, 0.3f));
        Label(startPanel.transform, "拖动飞机 自动射击", new Vector2(0, 140), 36, TextAnchor.MiddleCenter, true, Color.white);
        Btn(startPanel.transform, "开始", new Vector2(0, -40), new Vector2(480, 140), TextAnchor.MiddleCenter, StartRun, new Color(0.45f, 0.9f, 0.4f));

        pausePanel = Panel(root, "pause");
        var pauseBox = pausePanel.transform.Find("box");
        Label(pauseBox, "暂停", new Vector2(0, 200), 80, TextAnchor.MiddleCenter, true, new Color(1f, 0.92f, 0.3f));
        Btn(pauseBox, "继续", new Vector2(0, 50), new Vector2(440, 120), TextAnchor.MiddleCenter, () => Pause(false), new Color(0.45f, 0.9f, 0.4f));
        Btn(pauseBox, "重开", new Vector2(0, -90), new Vector2(440, 120), TextAnchor.MiddleCenter, StartRun, new Color(1f, 0.78f, 0.25f));
        pausePanel.SetActive(false);

        overPanel = Panel(root, "over");
        overTxt = Label(overPanel.transform, "", new Vector2(0, 180), 52, TextAnchor.MiddleCenter, true, Color.white);
        Btn(overPanel.transform, "重新开始", new Vector2(0, -80), new Vector2(480, 130), TextAnchor.MiddleCenter, StartRun, new Color(0.45f, 0.9f, 0.4f));
        overPanel.SetActive(false);
    }

    void Show(GameObject panel)
    {
        startPanel.SetActive(panel == startPanel);
        overPanel.SetActive(panel == overPanel);
        pausePanel.SetActive(panel == pausePanel);
        hud.SetActive(panel == hud || panel == overPanel);
    }

    void RefreshHud()
    {
        if (scoreNum != null)
        {
            var s = Mathf.Max(0, score).ToString();
            int pad = scoreNum.Length - s.Length;
            for (int i = 0; i < scoreNum.Length; i++)
            {
                if (!scoreNum[i]) continue;
                if (i < pad) { scoreNum[i].enabled = false; continue; }
                scoreNum[i].enabled = true;
                scoreNum[i].sprite = digits[s[i - pad] - '0'];
            }
        }
        if (lifeNum) lifeNum.sprite = digits[Mathf.Clamp(lives, 0, 9)];
    }

    GameObject Panel(Transform parent, string name)
    {
        var go = NewUi(name);
        go.transform.SetParent(parent, false);
        Stretch(go);
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.55f);
        var box = NewUi("box");
        box.transform.SetParent(go.transform, false);
        var rt = box.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(820, 720);
        var p = box.AddComponent<Image>();
        p.sprite = panelSpr;
        p.type = Image.Type.Sliced;
        p.color = new Color(0.45f, 0.32f, 0.28f, 0.95f);
        return go;
    }

    Button Btn(Transform parent, string text, Vector2 pos, Vector2 size, TextAnchor anchor, UnityEngine.Events.UnityAction click, Color tint)
    {
        var go = NewUi(text);
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        Place(rt, pos, anchor);
        var img = go.AddComponent<Image>();
        img.sprite = btnSpr;
        img.type = Image.Type.Sliced;
        img.color = tint;
        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(click);
        var t = Label(go.transform, text, Vector2.zero, 42, TextAnchor.MiddleCenter, true, Color.white);
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        return b;
    }

    Image[] ScoreHud(Transform parent, Vector2 pos)
    {
        var go = NewUi("score");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560, 80);
        var bg = go.AddComponent<Image>();
        bg.sprite = panelSpr;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
        Label(go.transform, "分数", new Vector2(16, 0), 36, TextAnchor.MiddleLeft, false, new Color(1f, 0.92f, 0.35f));
        var nums = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            var n = NewUi("n");
            n.transform.SetParent(go.transform, false);
            var nr = n.GetComponent<RectTransform>();
            nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(0, 0.5f);
            nr.anchoredPosition = new Vector2(108 + i * 72, 0);
            nr.sizeDelta = new Vector2(68, 64);
            var img = n.AddComponent<Image>();
            img.preserveAspect = true;
            img.color = new Color(1f, 0.92f, 0.35f);
            img.sprite = digits[0];
            nums[i] = img;
        }
        return nums;
    }

    Image LifeHud(Transform parent, Vector2 pos)
    {
        var go = NewUi("life");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(196, 80);
        var bg = go.AddComponent<Image>();
        bg.sprite = panelSpr;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);

        var icon = NewUi("icon");
        icon.transform.SetParent(go.transform, false);
        var ir = icon.GetComponent<RectTransform>();
        ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0, 0.5f);
        ir.anchoredPosition = new Vector2(12, 0);
        ir.sizeDelta = new Vector2(56, 56);
        var ic = icon.AddComponent<Image>();
        ic.sprite = lifeSpr;
        ic.preserveAspect = true;

        var num = NewUi("num");
        num.transform.SetParent(go.transform, false);
        var nr = num.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(1, 0.5f);
        nr.anchoredPosition = new Vector2(-16, 0);
        nr.sizeDelta = new Vector2(72, 72);
        var img = num.AddComponent<Image>();
        img.preserveAspect = true;
        img.sprite = digits[3];
        return img;
    }

    Text Chip(Transform parent, Sprite icon, string s, Vector2 pos, Color color)
    {
        var go = NewUi("chip");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 80);
        var bg = go.AddComponent<Image>();
        bg.sprite = panelSpr;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
        if (icon)
        {
            var ic = NewUi("icon");
            ic.transform.SetParent(go.transform, false);
            var ir = ic.GetComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0, 0.5f);
            ir.anchoredPosition = new Vector2(12, 0);
            ir.sizeDelta = new Vector2(56, 56);
            var img = ic.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
        }
        return Label(go.transform, s, new Vector2(icon ? 78 : 18, 0), 40, TextAnchor.MiddleLeft, false, color);
    }

    Text Label(Transform parent, string s, Vector2 pos, int size, TextAnchor align, bool center, Color color)
    {
        var go = NewUi("txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.9f);
        ol.effectDistance = new Vector2(2, -2);
        var rt = t.rectTransform;
        rt.sizeDelta = new Vector2(720, size * 2.2f);
        Place(rt, pos, center ? TextAnchor.MiddleCenter : align);
        return t;
    }

    static void Place(RectTransform rt, Vector2 pos, TextAnchor anchor)
    {
        Vector2 a = new Vector2(0.5f, 0.5f);
        if (anchor == TextAnchor.UpperRight) a = new Vector2(1, 1);
        else if (anchor == TextAnchor.MiddleLeft) a = new Vector2(0, 0.5f);
        rt.anchorMin = rt.anchorMax = rt.pivot = a;
        rt.anchoredPosition = pos;
    }

    static GameObject NewUi(string name) => new GameObject(name, typeof(RectTransform));

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>()) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    public Vector2 Bounds() => new Vector2(W - 0.6f, H - 0.8f);

    public void SfxShot() => Ping(sShot, 0.32f);
    public void SfxBoom() => Ping(sBoom, 0.6f);
    public void SfxGet() => Ping(sGet, 0.7f);
    public void SfxHurt() => Ping(sHurt, 0.85f);
    public void SfxBomb() => Ping(sBomb, 0.9f);

    void Ping(AudioClip clip, float vol)
    {
        if (src && clip) src.PlayOneShot(clip, vol);
    }

    static AudioClip Beep(float hz, float dur, float vol)
    {
        int n = Mathf.Max(16, (int)(44100 * dur));
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / 44100f;
            d[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * (1f - t / dur) * vol;
        }
        var clip = AudioClip.Create("b", n, 1, 44100, false);
        clip.SetData(d, 0);
        return clip;
    }

    static AudioClip Noise(float dur, float vol)
    {
        int n = Mathf.Max(16, (int)(44100 * dur));
        var d = new float[n];
        float acc = 0;
        for (int i = 0; i < n; i++)
        {
            acc = acc * 0.55f + (Random.value * 2f - 1f) * 0.45f;
            d[i] = acc * (1f - i / (float)n) * vol;
        }
        var clip = AudioClip.Create("n", n, 1, 44100, false);
        clip.SetData(d, 0);
        return clip;
    }

    public bool OverUi()
    {
        var es = EventSystem.current;
        if (!es) return false;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return es.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
        return es.IsPointerOverGameObject();
    }
}

public class PlayerShip : MonoBehaviour
{
    Sprite bullet;
    float fireCd, iframe, shield, tilt;
    SpriteRenderer sr, plate;

    public void Init(Sprite b, Sprite shieldIcon)
    {
        bullet = b;
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = 4;
        var go = new GameObject("Plate");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0.55f, 0);
        go.transform.localScale = Vector3.one * 1.6f;
        plate = go.AddComponent<SpriteRenderer>();
        plate.sprite = shieldIcon;
        plate.sortingOrder = 6;
        plate.enabled = false;
    }

    void Update()
    {
        if (!Game.I || Game.I.state != Game.State.Play) return;
        Move();
        fireCd -= Time.deltaTime;
        if (fireCd <= 0) { Fire(); fireCd = 0.18f; }
        if (iframe > 0) iframe -= Time.deltaTime;
        if (shield > 0)
        {
            shield -= Time.deltaTime;
            Game.I.SetShieldHud(shield);
            if (plate)
            {
                plate.enabled = shield > 1f || Mathf.PingPong(Time.time * 12f, 1f) > 0.35f;
                plate.color = shield > 1f ? Color.white : Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f), Mathf.PingPong(Time.time * 8f, 1f));
            }
            if (shield <= 0) BreakShield();
        }
        if (sr) sr.enabled = iframe <= 0 || Mathf.PingPong(Time.time * 12, 1) > 0.4f;
    }

    void Move()
    {
        var p = transform.position;
        var b = Game.I.Bounds();
        if (!Game.I.OverUi() && PointerHeld())
        {
            var w = PointerWorld();
            p = Vector3.Lerp(p, new Vector3(w.x, w.y, 0), 18 * Time.deltaTime);
        }
        var k = Keyboard.current;
        if (k != null)
        {
            var v = Vector2.zero;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
            p += (Vector3)(v.normalized * 8f * Time.deltaTime);
        }
        p.x = Mathf.Clamp(p.x, -b.x, b.x);
        p.y = Mathf.Clamp(p.y, -b.y, b.y);
        float vx = (p.x - transform.position.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        tilt = Mathf.Lerp(tilt, Mathf.Clamp(-vx * 8f, -18f, 18f), 12f * Time.deltaTime);
        transform.position = p;
        transform.rotation = Quaternion.Euler(0, 0, tilt);
    }

    void Fire()
    {
        var muzzle = transform.position + transform.up * (shield > 0 ? 0.85f : 0.4f);
        var go = Game.I.Spawn(bullet, muzzle, 0.55f, false);
        go.GetComponent<SpriteRenderer>().sortingOrder = 3;
        go.AddComponent<Shot>().Setup(Vector2.up * 14f, true);
        Game.I.SfxShot();
    }

    public void Hit()
    {
        if (iframe > 0) return;
        if (shield > 0)
        {
            Game.I.Fx(plate ? plate.transform.position : transform.position);
            return;
        }
        iframe = 1.1f;
        Game.I.Fx(transform.position);
        Game.I.Kick(0.07f, 0.14f);
        Game.I.SfxHurt();
        Game.I.LoseLife();
        StartCoroutine(Punch());
    }

    public void GiveShield()
    {
        shield = 3f;
        if (plate) { plate.enabled = true; plate.color = Color.white; }
        Game.I.SetShieldHud(shield);
        Game.I.SfxGet();
    }

    void BreakShield()
    {
        shield = 0;
        var pos = plate ? plate.transform.position : transform.position;
        if (plate) plate.enabled = false;
        Game.I.Fx(pos);
        Game.I.SfxHurt();
        Game.I.ShieldBroke();
        StartCoroutine(Punch());
    }

    IEnumerator Punch()
    {
        var s0 = transform.localScale;
        float t = 0;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = s0 * (1f + 0.25f * Mathf.Sin(t / 0.12f * Mathf.PI));
            yield return null;
        }
        transform.localScale = s0;
    }

    static bool PointerHeld()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    static Vector3 PointerWorld()
    {
        Vector2 s = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            s = Touchscreen.current.primaryTouch.position.ReadValue();
        var z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(new Vector3(s.x, s.y, z));
    }
}

public class EnemyCraft : MonoBehaviour
{
    public enum Kind { Plane, Tank }
    public bool IsTank => kind == Kind.Tank;
    Kind kind;
    int hp;
    float speed, fireEvery, fireCd, t;
    Sprite shotSpr;
    SpriteRenderer sr;
    bool dead;

    public void Init(Kind k, Sprite shot, float haste = 1f)
    {
        kind = k;
        shotSpr = shot;
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = k == Kind.Tank ? 1 : 3;
        if (k == Kind.Plane) { hp = 2; speed = 2.4f * haste; fireEvery = 1.4f / haste; }
        else { hp = 6; fireEvery = 2.4f / haste; }
        fireCd = fireEvery * 0.4f;
    }

    void Update()
    {
        if (!Game.I || Game.I.state != Game.State.Play || dead) return;
        if (kind == Kind.Plane)
        {
            t += Time.deltaTime;
            var p = transform.position;
            p.y -= speed * Time.deltaTime;
            p.x += Mathf.Sin(t * 2f) * 0.8f * Time.deltaTime;
            transform.position = p;
            if (p.y < -Game.I.Bounds().y - 2f) { Destroy(gameObject); return; }
        }

        fireCd -= Time.deltaTime;
        if (fireCd <= 0) { Fire(); fireCd = fireEvery; }
    }

    void Fire()
    {
        var muzzle = transform.position + Vector3.down * 0.35f;
        var go = Game.I.Spawn(shotSpr, muzzle, kind == Kind.Tank ? 0.75f : 0.5f, false);
        go.GetComponent<SpriteRenderer>().sortingOrder = kind == Kind.Tank ? 2 : 3;
        Vector2 vel;
        if (kind == Kind.Tank && Game.I.player)
        {
            vel = ((Vector2)Game.I.player.transform.position - (Vector2)muzzle).normalized * 4.2f;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, -vel);
        }
        else vel = Vector2.down * 7f;
        go.AddComponent<Shot>().Setup(vel, false);
    }

    public void Hurt(int dmg)
    {
        if (dead) return;
        hp -= dmg;
        if (kind == Kind.Plane) transform.position += Vector3.up * 0.18f;
        StartCoroutine(Flash());
        if (hp <= 0) Kill();
    }

    public void Kill()
    {
        if (dead) return;
        dead = true;
        Game.I.AddScore(kind == Kind.Tank ? 300 : 100, transform.position);
        Game.I.Fx(transform.position);
        Game.I.SfxBoom();
        if (kind == Kind.Tank) Game.I.Kick(0.06f, 0.12f);
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        StartCoroutine(DieAnim());
    }

    IEnumerator Flash()
    {
        var s0 = transform.localScale;
        float t = 0;
        const float d = 0.16f;
        while (t < d && this && !dead)
        {
            t += Time.deltaTime;
            float u = t / d;
            if (sr) sr.color = Color.Lerp(new Color(1f, 0.25f, 0.2f), Color.white, Ease.OutQuad(u));
            transform.localScale = s0 * (1.4f - 0.4f * Ease.OutBack(u));
            yield return null;
        }
        if (!this || dead) yield break;
        if (sr) sr.color = Color.white;
        transform.localScale = s0;
    }

    IEnumerator DieAnim()
    {
        var s0 = transform.localScale;
        float t = 0;
        const float d = 0.22f;
        while (t < d && this)
        {
            t += Time.deltaTime;
            float u = Ease.OutQuad(t / d);
            transform.localScale = s0 * (1.25f - u);
            if (kind == Kind.Plane) transform.Rotate(0, 0, 540f * Time.deltaTime);
            if (sr) sr.color = new Color(1f, 0.5f, 0.2f, 1f - u);
            yield return null;
        }
        if (this) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        var p = o.GetComponent<PlayerShip>();
        if (p) p.Hit();
    }
}

public class Shot : MonoBehaviour
{
    public bool fromPlayer;
    Vector2 vel;
    float appear, baseScale;

    public void Setup(Vector2 v, bool player)
    {
        vel = v;
        fromPlayer = player;
        baseScale = transform.localScale.x > 0.01f ? transform.localScale.x : 0.55f;
        transform.localScale = new Vector3(baseScale * 0.2f, baseScale * 1.4f, 1f);
    }

    void Update()
    {
        appear += Time.deltaTime * 10f;
        float k = Ease.OutBack(Mathf.Min(1f, appear));
        transform.localScale = new Vector3(baseScale * k, baseScale * (0.7f + 0.3f * k), 1f);
        transform.position += (Vector3)(vel * Time.deltaTime);
        if (Mathf.Abs(transform.position.y) > 12) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        if (fromPlayer)
        {
            var e = o.GetComponent<EnemyCraft>();
            if (e) { e.Hurt(1); Destroy(gameObject); }
        }
        else
        {
            var p = o.GetComponent<PlayerShip>();
            if (p) { p.Hit(); Destroy(gameObject); }
        }
    }
}

public class Drop : MonoBehaviour
{
    public enum Kind { Life, Shield, Bomb }
    public Kind kind;

    void Update()
    {
        transform.position += Vector3.down * 2.2f * Time.deltaTime;
        if (transform.position.y < -10) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        var p = o.GetComponent<PlayerShip>();
        if (!p) return;
        if (kind == Kind.Life) Game.I.AddLife();
        else if (kind == Kind.Shield) p.GiveShield();
        else Game.I.Bomb();
        Destroy(gameObject);
    }
}
