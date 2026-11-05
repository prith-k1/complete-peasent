using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CompletePeasant.ShortGame
{
    public class AdventureUI : MonoBehaviour
    {
        public Text healthText, stageText, promptText, toastText, bossText;
        public Slider healthBar, bossBar;
        public Image[] healthPips;
        public CanvasGroup stageCaption;
        public GameObject panel, bossGroup;
        public Text heading, body;
        public Button[] buttons;
        public bool IsOpen { get { return panel.activeSelf; } }
        private readonly List<Action> actions = new List<Action>();
        private Interactable[] people;
        private Angel boss;
        private float toastTimer, stageTimer;
        private const string SaveError = "Save failed. Check disk space.";
        private bool showedSaveError;
        private bool locked;
        private Adventure Game { get { return Adventure.Instance; } }

        private void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            { int index = i; buttons[i].onClick.AddListener(() => { if (index < actions.Count) actions[index](); }); }
        }

        public void Bind(StageData stage)
        {
            stageText.text = stage.title;
            stageTimer = 4.5f;
            people = FindObjectsOfType<Interactable>();
            boss = FindObjectOfType<Angel>();
            toastTimer = 0;
        }

        private void Update()
        {
            if (Game == null || Game.Player == null) return;
            bool usePips = healthPips != null && healthPips.Length > 0;
            healthText.gameObject.SetActive(!IsOpen && !usePips);
            healthBar.gameObject.SetActive(!IsOpen && !usePips);
            if (usePips)
                for (int i = 0; i < healthPips.Length; i++)
                {
                    healthPips[i].gameObject.SetActive(!IsOpen);
                    healthPips[i].color = i < Game.Player.Health ? new Color(.94f, .91f, .80f) : new Color(.25f, .28f, .30f, .8f);
                }
            stageTimer -= Time.deltaTime;
            stageText.gameObject.SetActive(!IsOpen && stageTimer > 0);
            if (stageCaption != null) stageCaption.alpha = Mathf.Clamp01(stageTimer);
            promptText.gameObject.SetActive(!IsOpen);
            healthText.text = Game.Player.Health + " / " + Game.Player.maxHealth;
            healthBar.value = Game.Player.Health / (float)Game.Player.maxHealth;
            bossGroup.SetActive(!IsOpen && boss != null && boss.Active);
            if (boss != null && boss.Active) { bossText.text = Game.Stage.data.bossName; bossBar.value = boss.Health / (float)boss.maxHealth; }
            toastTimer -= Time.unscaledDeltaTime;
            if (Game.SaveFailed) { toastText.text = SaveError; showedSaveError = true; }
            else if (showedSaveError)
            {
                if (toastText.text == SaveError) toastTimer = 0;
                showedSaveError = false;
            }
            toastText.gameObject.SetActive(Game.SaveFailed || toastTimer > 0);
            promptText.text = "";
            if (Game.Loading || Game.Player.Dead) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            { if (IsOpen) { if (!locked) Close(); } else Pause(); return; }
            if (IsOpen) return;
            Interactable nearest = null;
            float best = 2.5f;
            foreach (Interactable person in people)
            {
                if (person == null) continue;
                float distance = Vector2.Distance(person.transform.position, Game.Player.transform.position);
                if (distance < best) { best = distance; nearest = person; }
            }
            if (nearest == null) return;
            promptText.text = "[E] " + nearest.label;
            if (Input.GetKeyDown(KeyCode.E)) nearest.Use();
        }

        private void Open(string title, string message, bool keepOpen = false)
        {
            panel.SetActive(true); heading.text = title; body.text = message;
            if (Game == null || !Game.SaveFailed) toastTimer = 0;
            toastText.transform.SetAsLastSibling();
            bool hasBody = !string.IsNullOrEmpty(message);
            body.gameObject.SetActive(hasBody);
            RectTransform backdrop = panel.GetComponent<RectTransform>();
            backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            heading.rectTransform.anchoredPosition = new Vector2(0, hasBody ? 300 : 200);
            heading.fontSize = hasBody ? 54 : 64;
            body.rectTransform.anchoredPosition = new Vector2(0, 50);
            locked = keepOpen; Time.timeScale = 0; actions.Clear();
            foreach (Button button in buttons) button.gameObject.SetActive(false);
        }

        private void Add(string label, Action action)
        {
            Button button = buttons[actions.Count];
            button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (body.gameObject.activeSelf ? -245 : 35) - actions.Count * 70);
            button.GetComponentInChildren<Text>().text = label;
            button.gameObject.SetActive(true); actions.Add(action);
        }
        public void Close() { panel.SetActive(false); locked = false; Time.timeScale = 1; }
        public void Toast(string message)
        {
            if (Game != null && Game.SaveFailed) { toastText.text = SaveError; showedSaveError = true; }
            else { toastText.text = message; toastTimer = 3; }
        }
        public void Dialogue(string message) { Open(message, ""); Add("Close", Close); }
        public void Story(string title, string message) { Open(title, message); Add("Close", Close); }

        public void Title()
        {
            if (Game.Loading || (Game.Player != null && Game.Player.Dead)) return;
            Open("COMPLETE-PEASANT", "", true);
            Add("Play", () => Game.Begin(false));
            Add("New game", ConfirmNewGame);
            Add("Controls", () => Controls(true));
            Add("Quit", () => Application.Quit());
        }

        private void ConfirmNewGame()
        {
            Open("Restart game?", "", true);
            Add("Restart", () => Game.Begin(true)); Add("Back", Title);
        }
        public void Pause()
        {
            if (Game.Loading || Game.Player.Dead) return;
            Open("Paused", "");
            Add("Resume", Close); Add("Controls", () => Controls(false)); Add("Title", Title);
        }
        private void Controls(bool fromTitle)
        {
            string controls = "Move   A / D or arrows\nJump   Space\nAttack   Left click\nParry   Right click\nDown slash   Down + Left click (air)";
            if (Game.Data.HasSkill(Skill.EmberDash)) controls += "\nDash   C";
            if (Game.Data.HasSkill(Skill.AshWings)) controls += "\nDouble jump   Space twice";
            if (Game.Data.HasSkill(Skill.Sunbreak)) controls += "\nSunbreak   F";
            controls += "\nUse   E\nPause   Esc";
            Open("Controls", controls, fromTitle);
            Add("Back", fromTitle ? (Action)Title : Pause);
        }
        public void Ending()
        {
            Open("Dawn restored", "The Desolate Sun is free.", true);
            Add("Title", Title); Add("Replay", ConfirmNewGame);
        }
    }
}
