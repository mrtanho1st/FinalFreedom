using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FinalFreedom
{
    public sealed class UIManager : MonoBehaviour
    {
        GameManager game;
        RectTransform safe, hud, overlay, radar;
        Rect screenSafe;
        Text score, coins, distance, speed, multiplier, powerups, pursuit, toast;
        RectTransform healthFill, nitroFill, policeFill;
        RectTransform playerDot;
        readonly RectTransform[] policeDots = new RectTransform[5];
        float toastUntil, nextHud;
        Font font;
        Color muted = new Color(0.60f, 0.72f, 0.77f);
        Color panel = new Color(0.025f, 0.045f, 0.073f, 0.96f);
        RectTransform rankingContent;
        Text rankingStatus;
        public void Initialize(GameManager manager)
        {
            game = manager;
            font = Resources.Load<Font>("FinalFreedomFont");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("UI - responsive safe area", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scale = canvasObject.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1080, 1920); scale.matchWidthOrHeight = 0.5f;
            if (EventSystem.current == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            safe = Rect(canvasObject.transform, "Safe area", 0, 0, 1, 1); ApplySafeArea();
            hud = Rect(safe, "Driving HUD", 0, 0, 1, 1);
            BuildHUD();
            overlay = Rect(safe, "Menu screens", 0, 0, 1, 1);
            toast = Label(safe, "", 0.13f, 0.68f, 0.87f, 0.73f, 32, ProceduralArt.Gold, TextAnchor.MiddleCenter);
            toast.gameObject.SetActive(false);
            game.Leaderboard.Changed += UpdateRanking;
        }
        RectTransform Rect(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        Image Panel(Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            var rect = Rect(parent, "Panel", x0, y0, x1, y1); var image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        Text Label(Transform parent, string content, float x0, float y0, float x1, float y1, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rect = Rect(parent, content, x0, y0, x1, y1); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.text = content; text.fontSize = size; text.color = color; text.alignment = align;
            text.supportRichText = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            return text;
        }
        Button Button(Transform parent, string content, float x0, float y0, float x1, float y1, UnityAction click, bool primary = false)
        {
            var image = Panel(parent, primary ? ProceduralArt.Gold : new Color(0.12f, 0.21f, 0.27f, 0.97f), x0, y0, x1, y1);
            image.gameObject.name = content; image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (click != null) button.onClick.AddListener(click);
            Label(image.transform, content, 0.05f, 0.05f, 0.95f, 0.95f, 34, primary ? ProceduralArt.Ink : Color.white, TextAnchor.MiddleCenter);
            return button;
        }
        RectTransform Bar(Transform parent, Color color, float x0, float y0, float x1, float y1)
        {
            var background = Panel(parent, new Color(0.16f, 0.24f, 0.29f), x0, y0, x1, y1);
            return (RectTransform)Panel(background.transform, color, 0, 0, 1, 1).transform;
        }
        static void Fill(RectTransform rect, float value) { rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1); }
        void BuildHUD()
        {
            Panel(hud, panel, 0.025f, 0.81f, 0.975f, 0.97f);
            Label(hud, "INTEGRITY", 0.055f, 0.936f, 0.44f, 0.96f, 23, muted);
            Label(hud, "NITRO / HOLD", 0.54f, 0.936f, 0.94f, 0.96f, 23, muted);
            healthFill = Bar(hud, new Color(0.3f, 0.9f, 0.62f), 0.055f, 0.914f, 0.46f, 0.929f);
            nitroFill = Bar(hud, ProceduralArt.Cyan, 0.54f, 0.914f, 0.94f, 0.929f);
            score = Label(hud, "000000", 0.055f, 0.845f, 0.50f, 0.908f, 60, Color.white);
            distance = Label(hud, "0 m", 0.055f, 0.817f, 0.45f, 0.845f, 25, muted);
            coins = Label(hud, "$ 0", 0.54f, 0.854f, 0.81f, 0.902f, 42, ProceduralArt.Gold);
            multiplier = Label(hud, "x1", 0.54f, 0.817f, 0.8f, 0.849f, 26, ProceduralArt.Cyan);
            Button(hud, "II", 0.845f, 0.829f, 0.95f, 0.894f, game.TogglePause);
            pursuit = Label(hud, "PURSUIT", 0.055f, 0.776f, 0.50f, 0.803f, 22, Color.white);
            policeFill = Bar(hud, new Color(1, 0.25f, 0.27f), 0.055f, 0.756f, 0.4f, 0.768f);
            radar = (RectTransform)Panel(hud, panel, 0.80f, 0.57f, 0.965f, 0.79f).transform;
            for (int i = 1; i < 4; i++) Panel(radar, new Color(0.3f, 0.43f, 0.46f), i * 0.25f - 0.006f, 0, i * 0.25f + 0.006f, 1);
            playerDot = Dot(radar, ProceduralArt.Cyan);
            for (int i = 0; i < policeDots.Length; i++) policeDots[i] = Dot(radar, new Color(1, 0.27f, 0.28f));
            speed = Label(hud, "0\nKM/H", 0.04f, 0.18f, 0.24f, 0.28f, 42, Color.white, TextAnchor.MiddleCenter);
            powerups = Label(hud, "", 0.28f, 0.16f, 0.94f, 0.28f, 27, ProceduralArt.Cyan, TextAnchor.MiddleRight);
            Button(hud, "<", 0.035f, 0.044f, 0.20f, 0.11f, () => game.Player.MoveLane(-1));
            Button(hud, ">", 0.22f, 0.044f, 0.385f, 0.11f, () => game.Player.MoveLane(1));
            Button(hud, "BRAKE", 0.43f, 0.044f, 0.66f, 0.11f, () => game.Player.Brake());
            Button(hud, "NITRO", 0.70f, 0.044f, 0.965f, 0.11f, null, true).gameObject.AddComponent<HoldButton>();
            Label(hud, "SWIPE TO DODGE  /  HOLD TO BOOST", 0.05f, 0.012f, 0.95f, 0.035f, 20, muted, TextAnchor.MiddleCenter);
        }
        RectTransform Dot(Transform parent, Color color)
        {
            var rect = (RectTransform)Panel(parent, color, 0.5f, 0.5f, 0.5f, 0.5f).transform;
            rect.sizeDelta = new Vector2(16, 25); return rect;
        }
        void ClearOverlay()
        {
            rankingContent = null; rankingStatus = null;
            foreach (Transform child in overlay) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            overlay.gameObject.SetActive(true); hud.gameObject.SetActive(false);
            toast.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        void Scrim()
        {
            Panel(overlay, new Color(0.02f, 0.04f, 0.07f, 0.70f), 0, 0, 1, 1);
            Panel(overlay, ProceduralArt.Gold, 0.08f, 0.91f, 0.20f, 0.916f);
        }
        public void ShowMenu()
        {
            if (!SaveService.HasPlayerName) { ShowNameEntry(); return; }
            ClearOverlay(); Scrim();
            Label(overlay, "FINAL\nFREEDOM", 0.08f, 0.65f, 0.94f, 0.9f, 112, Color.white);
            Label(overlay, "FOUR LANES. ONE WAY OUT.", 0.085f, 0.60f, 0.93f, 0.65f, 30, ProceduralArt.Gold);
            Label(overlay, "ENDLESS CITY  /  HIGH SPEED PURSUIT", 0.085f, 0.558f, 0.93f, 0.599f, 23, muted);
            Button(overlay, "PLAY   >", 0.08f, 0.439f, 0.92f, 0.516f, game.BeginRun, true);
            Button(overlay, "GARAGE", 0.08f, 0.345f, 0.48f, 0.415f, ShowShop);
            Button(overlay, "LEADERBOARD", 0.52f, 0.345f, 0.92f, 0.415f, ShowRecords);
            Button(overlay, "HOW TO PLAY", 0.08f, 0.257f, 0.92f, 0.324f, ShowHelp);
            Label(overlay, "BEST  " + SaveService.Best.ToString("N0") + "      WALLET  $" + SaveService.Wallet, 0.08f, 0.19f, 0.92f, 0.236f, 28, ProceduralArt.Gold);
            Label(overlay, "READY: " + SaveService.CurrentVehicle.displayName, 0.08f, 0.152f, 0.92f, 0.19f, 23, muted);
            Button(overlay, game.Effects.Muted ? "SOUND: OFF" : "SOUND: ON", 0.08f, 0.061f, 0.5f, 0.113f, () => { game.Effects.ToggleMute(); ShowMenu(); });
            Label(overlay, "FINALFREEDOM / 01", 0.54f, 0.055f, 0.92f, 0.12f, 19, muted, TextAnchor.MiddleRight);
        }
        public void ShowHUD() { overlay.gameObject.SetActive(false); hud.gameObject.SetActive(true); nextHud = 0; }
        public void ShowGameOver()
        {
            ClearOverlay(); Scrim();
            Label(overlay, "BUSTED.", 0.08f, 0.70f, 0.94f, 0.88f, 108, new Color(1, 0.3f, 0.25f));
            Label(overlay, "EVERY ESCAPE STARTS AGAIN.", 0.085f, 0.643f, 0.93f, 0.70f, 28, muted);
            Label(overlay, "SCORE     " + Mathf.FloorToInt(game.Score).ToString("N0") + "\nDISTANCE     " + game.Distance.ToString("N0") + " m\nCOINS BANKED     $" + game.Coins + "\nPERSONAL BEST     " + SaveService.Best.ToString("N0"), 0.09f, 0.35f, 0.92f, 0.61f, 39, Color.white);
            Button(overlay, "RETRY   >", 0.08f, 0.239f, 0.92f, 0.319f, game.BeginRun, true);
            Button(overlay, "MAIN MENU", 0.08f, 0.137f, 0.92f, 0.209f, game.ToMenu);
        }
        public void ShowPause()
        {
            ClearOverlay(); Scrim();
            Label(overlay, "PAUSED", 0.08f, 0.63f, 0.94f, 0.84f, 100, Color.white);
            Button(overlay, "RESUME", 0.08f, 0.48f, 0.92f, 0.57f, game.TogglePause, true);
            Button(overlay, "MAIN MENU / BANK COINS", 0.08f, 0.36f, 0.92f, 0.44f, game.ToMenu);
            Button(overlay, game.Effects.Muted ? "SOUND: OFF" : "SOUND: ON", 0.08f, 0.24f, 0.92f, 0.32f, () => { game.Effects.ToggleMute(); ShowPause(); });
        }
        public void ShowShop()
        {
            ClearOverlay();
            Panel(overlay, new Color(0.025f, 0.045f, 0.073f, 0.98f), 0, 0, 1, 1);
            var vehicles = VehicleCatalog.Instance.vehicles;
            Label(overlay, "THE GARAGE", 0.08f, 0.875f, 0.94f, 0.95f, 62, Color.white);
            Label(overlay, vehicles.Length + " VEHICLES / ALL AVAILABLE", 0.08f, 0.835f, 0.94f, 0.872f, 25, ProceduralArt.Gold);
            var image = Rect(overlay, "Vehicle 3D preview", 0.08f, 0.57f, 0.92f, 0.82f).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            var preview = image.gameObject.AddComponent<GaragePreview>();
            preview.Initialize(image, transform);
            var name = Label(overlay, "", 0.08f, 0.515f, 0.92f, 0.568f, 31, Color.white, TextAnchor.MiddleCenter);
            var dimensions = Label(overlay, "", 0.08f, 0.476f, 0.92f, 0.515f, 23, muted, TextAnchor.MiddleCenter);

            var list = Panel(overlay, panel, 0.08f, 0.205f, 0.92f, 0.465f);
            list.name = "Vehicle list"; list.raycastTarget = true;
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 55;
            var viewport = Rect(list.transform, "Viewport", 0, 0, 0.95f, 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Vehicle entries", 0, 1, 1, 1);
            content.pivot = new Vector2(0.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            var track = Panel(list.transform, new Color(0.12f, 0.2f, 0.25f), 0.96f, 0, 1, 1);
            track.name = "Vehicle scrollbar"; track.raycastTarget = true;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            var area = Rect(track.transform, "Sliding area", 0, 0, 1, 1);
            var handle = Panel(area, ProceduralArt.Gold, 0, 0, 1, 1);
            handle.raycastTarget = true;
            scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;

            int focused = SaveService.Selected;
            var entries = new Button[vehicles.Length];
            Text equipText = null;
            Button equip = null;
            void RefreshSelection()
            {
                int selected = SaveService.Selected;
                for (int i = 0; i < entries.Length; i++)
                {
                    entries[i].GetComponent<Image>().color = i == focused ? new Color(0.17f, 0.37f, 0.43f) : new Color(0.08f, 0.14f, 0.2f);
                    entries[i].GetComponentInChildren<Text>().text = vehicles[i].displayName + (i == selected ? "   [EQUIPPED]" : "");
                }
                equipText.text = focused == selected ? "EQUIPPED" : "SELECT VEHICLE";
                equip.interactable = focused != selected;
            }
            void Focus(int index)
            {
                focused = index;
                preview.Show(vehicles[index]);
                name.text = vehicles[index].displayName;
                var size = preview.ModelBounds.size;
                dimensions.text = $"W {size.x:0.00} / H {size.y:0.00} / L {size.z:0.00} m   |   HP {vehicles[index].health:0}";
                RefreshSelection();
            }
            for (int i = 0; i < vehicles.Length; i++)
            {
                int index = i;
                entries[i] = Button(content, vehicles[i].displayName, 0, 0, 1, 1, () => Focus(index));
                entries[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
                var text = entries[i].GetComponentInChildren<Text>();
                text.resizeTextMaxSize = 27;
            }
            equip = Button(overlay, "SELECT VEHICLE", 0.08f, 0.125f, 0.92f, 0.188f, () =>
            {
                if (!SaveService.SelectVehicle(focused)) return;
                game.Player.ResetForRun();
                game.Chase.Snap();
                RefreshSelection();
            }, true);
            equipText = equip.GetComponentInChildren<Text>();
            Button(overlay, "BACK", 0.08f, 0.045f, 0.92f, 0.108f, ShowMenu);
            Canvas.ForceUpdateCanvases();
            Focus(focused);
            // Start with the equipped row visible, while allowing wheel, touch and handle scrolling.
            float overflow = Mathf.Max(0, content.rect.height - viewport.rect.height);
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(focused * 104f, 0, overflow));
        }
        public void ShowNameEntry()
        {
            ClearOverlay();
            Panel(overlay, panel, 0, 0, 1, 1);
            Label(overlay, "WELCOME, DRIVER", 0.08f, 0.76f, 0.92f, 0.88f, 64, Color.white);
            Label(overlay, "CHOOSE YOUR NAME", 0.08f, 0.66f, 0.92f, 0.72f, 37, ProceduralArt.Gold);
            Label(overlay, "Your name and personal best will appear on the leaderboard.\nYour name is saved on this device.", 0.08f, 0.53f, 0.92f, 0.65f, 29, muted);
            var background = Panel(overlay, new Color(0.12f, 0.21f, 0.27f), 0.08f, 0.42f, 0.92f, 0.50f);
            background.name = "Player name input"; background.raycastTarget = true;
            var input = background.gameObject.AddComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = Label(background.transform, "", 0.04f, 0.05f, 0.96f, 0.95f, 36, Color.white);
            input.placeholder = Label(background.transform, "Enter your name", 0.04f, 0.05f, 0.96f, 0.95f, 32, muted);
            input.characterLimit = 24;
            input.lineType = InputField.LineType.SingleLine;
            var error = Label(overlay, "1-24 characters. Letters, numbers, spaces, _ - . '", 0.08f, 0.33f, 0.92f, 0.40f, 25, muted);
            void Confirm()
            {
                if (!SaveService.TrySetPlayerName(input.text))
                { error.text = "Please enter a valid name (1-24 characters)."; error.color = new Color(1, 0.4f, 0.3f); return; }
                game.Leaderboard.QueueBest();
                ShowMenu();
            }
            Button(overlay, "CONTINUE", 0.08f, 0.23f, 0.92f, 0.31f, Confirm, true);
            input.onSubmit.AddListener(_ => Confirm());
        }

        public void ShowRecords()
        {
            ClearOverlay();
            Panel(overlay, panel, 0, 0, 1, 1);
            Label(overlay, "LEADERBOARD", 0.08f, 0.84f, 0.94f, 0.94f, 60, Color.white);
            Label(overlay, SaveService.PlayerName + "  /  BEST " + SaveService.Best.ToString("N0"), 0.08f, 0.78f, 0.92f, 0.84f, 31, ProceduralArt.Gold);
            var header = Panel(overlay, new Color(0.12f, 0.21f, 0.27f), 0.08f, 0.71f, 0.92f, 0.77f);
            Label(header.transform, "Top", 0.03f, 0, 0.18f, 1, 31, ProceduralArt.Gold);
            Label(header.transform, "Name", 0.20f, 0, 0.70f, 1, 31, ProceduralArt.Gold);
            Label(header.transform, "Score", 0.71f, 0, 0.97f, 1, 31, ProceduralArt.Gold, TextAnchor.MiddleRight);
            var list = Panel(overlay, new Color(0.04f, 0.08f, 0.12f), 0.08f, 0.25f, 0.92f, 0.71f);
            list.name = "Leaderboard rows"; list.raycastTarget = true;
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 55;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect(list.transform, "Viewport", 0, 0, 0.96f, 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            rankingContent = Rect(viewport, "Ranking content", 0, 1, 1, 1);
            rankingContent.pivot = new Vector2(0.5f, 1);
            var layout = rankingContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            rankingContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = rankingContent;
            var track = Panel(list.transform, new Color(0.12f, 0.2f, 0.25f), 0.97f, 0, 1, 1);
            track.raycastTarget = true;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            var area = Rect(track.transform, "Sliding area", 0, 0, 1, 1);
            var handle = Panel(area, ProceduralArt.Gold, 0, 0, 1, 1); handle.raycastTarget = true;
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar = bar;
            rankingStatus = Label(overlay, "", 0.08f, 0.18f, 0.92f, 0.24f, 23, muted);
            Button(overlay, "REFRESH", 0.08f, 0.10f, 0.48f, 0.17f, () => game.Leaderboard.Refresh());
            Button(overlay, "ADAFRUIT IO", 0.52f, 0.10f, 0.92f, 0.17f, () => Application.OpenURL(game.Leaderboard.Config.dashboardUrl));
            Button(overlay, "BACK", 0.08f, 0.025f, 0.92f, 0.09f, ShowMenu);
            UpdateRanking();
            game.Leaderboard.Refresh();
        }
        void UpdateRanking()
        {
            if (rankingContent == null) return;
            foreach (Transform child in rankingContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            rankingStatus.text = game.Leaderboard.Status;
            var entries = game.Leaderboard.Entries;
            if (entries.Length == 0)
            {
                var empty = Label(rankingContent, "No online scores yet. Play your first run!", 0, 0, 1, 1, 28, muted, TextAnchor.MiddleCenter);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
            }
            foreach (var entry in entries)
            {
                var row = Panel(rankingContent, entry.id == SaveService.PlayerId ? new Color(0.12f, 0.32f, 0.38f) : new Color(0.08f, 0.14f, 0.2f), 0, 0, 1, 1);
                row.name = "Rank " + entry.top; row.gameObject.AddComponent<LayoutElement>().preferredHeight = 82;
                Label(row.transform, entry.top.ToString(), 0.03f, 0, 0.18f, 1, 30, Color.white);
                Label(row.transform, entry.name, 0.20f, 0, 0.70f, 1, 30, Color.white);
                Label(row.transform, entry.score.ToString("N0"), 0.71f, 0, 0.97f, 1, 30, ProceduralArt.Gold, TextAnchor.MiddleRight);
            }
        }
        void OnDestroy()
        {
            if (game != null && game.Leaderboard != null) game.Leaderboard.Changed -= UpdateRanking;
        }
        void ShowHelp()
        {
            ClearOverlay(); Scrim();
            Label(overlay, "DRIVE. DODGE. ESCAPE.", 0.08f, 0.78f, 0.93f, 0.89f, 54, Color.white);
            Label(overlay, "SWIPE LEFT / RIGHT   Change lanes\nSWIPE DOWN   Emergency brake\nHOLD SCREEN   Nitro +50% speed\n\nKEYBOARD   A/D or arrows, S to brake\nSPACE   Nitro     ESC   Pause\n\nLEFT LANES   Oncoming traffic, score x2\nRIGHT LANES   Same direction\n\nM   Magnet for 10 seconds\nN   Protected boost for 8 seconds\nS   Shield against one impact\n+   Repair 30 HP     x2   Double score\n\nChoose any vehicle in the garage. Collect coins to build your record.", 0.08f, 0.22f, 0.93f, 0.75f, 32, Color.white);
            Button(overlay, "GOT IT", 0.08f, 0.09f, 0.92f, 0.17f, ShowMenu, true);
        }
        public void Toast(string message, float seconds)
        {
            toast.text = message; toastUntil = Time.unscaledTime + seconds; toast.gameObject.SetActive(true); toast.transform.SetAsLastSibling();
        }
        void ApplySafeArea()
        {
            screenSafe = Screen.safeArea;
            safe.anchorMin = new Vector2(screenSafe.xMin / Mathf.Max(1, Screen.width), screenSafe.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(screenSafe.xMax / Mathf.Max(1, Screen.width), screenSafe.yMax / Mathf.Max(1, Screen.height));
        }
        void Update()
        {
            if (Screen.safeArea != screenSafe) ApplySafeArea();
            if (toast.gameObject.activeSelf && Time.unscaledTime > toastUntil) toast.gameObject.SetActive(false);
            if (!game.IsPlaying || Time.unscaledTime < nextHud) return;
            nextHud = Time.unscaledTime + 0.08f; // HUD 12.5 Hz: giảm tạo chuỗi/GC trên Android.
            var player = game.Player;
            score.text = Mathf.FloorToInt(game.Score).ToString("000000"); coins.text = "$ " + game.Coins;
            distance.text = game.Distance.ToString("N0") + " m";
            speed.text = (player.Speed * 3.6f).ToString("0") + "\nKM/H";
            multiplier.text = (player.Lane < 2 ? "ONCOMING " : "SCORE ") + "x" + game.ScoreMultiplier;
            Fill(healthFill, player.Damage.Fraction); Fill(nitroFill, player.Nitro / 100);
            powerups.text = (player.MagnetActive ? "MAGNET " + Mathf.CeilToInt(player.MagnetTime) + "s\n" : "")
                + (player.BoostActive ? "BOOST " + Mathf.CeilToInt(player.BoostTime) + "s\n" : "")
                + (player.Damage.HasShield ? "SHIELD READY\n" : "")
                + (player.DoubleScoreActive ? "SCORE x2 " + Mathf.CeilToInt(player.DoubleTime) + "s" : "");
            playerDot.anchorMin = playerDot.anchorMax = new Vector2(Mathf.Clamp01((player.transform.position.x + 6) / 12), 0.30f);
            float closest = float.MaxValue, fraction = 0; int alive = 0;
            for (int i = 0; i < game.Police.Units.Length; i++)
            {
                var unit = game.Police.Units[i]; bool active = unit.gameObject.activeSelf && !unit.Damage.IsDead;
                policeDots[i].gameObject.SetActive(active);
                if (!active) continue;
                alive++; float dz = unit.transform.position.z - player.transform.position.z;
                policeDots[i].anchorMin = policeDots[i].anchorMax = new Vector2(Mathf.Clamp((unit.transform.position.x + 6) / 12, 0.08f, 0.92f), Mathf.Clamp(0.3f + dz / 130f, 0.03f, 0.97f));
                if (Mathf.Abs(dz) < closest) { closest = Mathf.Abs(dz); fraction = unit.Damage.Fraction; }
            }
            pursuit.text = alive == 0 ? "PURSUIT  /  CLEAR" : "PURSUIT  /  " + alive + " UNIT" + (alive > 1 ? "S" : "");
            Fill(policeFill, fraction);
        }
    }
}
