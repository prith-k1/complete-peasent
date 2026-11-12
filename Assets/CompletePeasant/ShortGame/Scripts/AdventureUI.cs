using UnityEngine;
using UnityEngine.Events;
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
        public bool IsOpen
        {
            get
            {
                return panel.activeSelf;
            }
        }

        private int buttonCount;
        private Interactable[] people;
        private Angel boss;
        private float toastTimer, stageTimer;
        private const string SaveError = "Save failed. Check disk space.";
        private bool showedSaveError;
        private bool locked;
        private int developerStage;
        private Adventure Game
        {
            get
            {
                return Adventure.Instance;
            }
        }

        private void Awake()
        {
            StageRoot stage = FindObjectOfType<StageRoot>();
            if (stage != null && stage.data != null)
                developerStage = stage.data.index;
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
            if (Game == null || Game.Player == null)
                return;
            UpdateHealth();
            UpdateBoss();
            UpdateMessages();

            promptText.text = "";
            promptText.gameObject.SetActive(!IsOpen);
            if (Game.Loading || Game.Player.Dead)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!IsOpen)
                    Pause();
                else if (!locked)
                    Close();
                return;
            }

            if (!IsOpen)
                UpdateInteraction();
        }

        private void UpdateHealth()
        {
            bool usePips = healthPips != null && healthPips.Length > 0;
            healthText.gameObject.SetActive(!IsOpen && !usePips);
            healthBar.gameObject.SetActive(!IsOpen && !usePips);
            healthText.text = Game.Player.Health + " / " + Game.Player.maxHealth;
            healthBar.value = Game.Player.Health / (float)Game.Player.maxHealth;

            if (usePips)
            {
                for (int i = 0; i < healthPips.Length; i++)
                {
                    healthPips[i].gameObject.SetActive(!IsOpen);
                    if (i < Game.Player.Health)
                        healthPips[i].color = new Color(.94f, .91f, .80f);
                    else
                        healthPips[i].color = new Color(.25f, .28f, .30f, .8f);
                }
            }
        }

        private void UpdateBoss()
        {
            bossGroup.SetActive(!IsOpen && boss != null && boss.Active);
            if (boss != null && boss.Active)
            {
                bossText.text = Game.Stage.data.bossName;
                bossBar.value = boss.Health / (float)boss.maxHealth;
            }
        }

        private void UpdateMessages()
        {
            stageTimer -= Time.deltaTime;
            stageText.gameObject.SetActive(!IsOpen && stageTimer > 0);
            if (stageCaption != null)
                stageCaption.alpha = Mathf.Clamp01(stageTimer);

            toastTimer -= Time.unscaledDeltaTime;
            if (Game.SaveFailed)
            {
                toastText.text = SaveError;
                showedSaveError = true;
            }
            else if (showedSaveError)
            {
                if (toastText.text == SaveError)
                    toastTimer = 0;
                showedSaveError = false;
            }

            toastText.gameObject.SetActive(Game.SaveFailed || toastTimer > 0);
        }

        private void UpdateInteraction()
        {
            Interactable nearest = null;
            float best = 2.5f;
            foreach (Interactable person in people)
            {
                if (person == null)
                    continue;
                float distance = Vector2.Distance(person.transform.position, Game.Player.transform.position);
                if (distance < best)
                {
                    best = distance;
                    nearest = person;
                }
            }

            if (nearest == null)
                return;
            promptText.text = "[E] " + nearest.label;
            if (Input.GetKeyDown(KeyCode.E))
                nearest.Use();
        }

        private void Open(string title, string message, bool keepOpen = false)
        {
            panel.SetActive(true);
            heading.text = title;
            body.text = message;
            if (Game == null || !Game.SaveFailed)
                toastTimer = 0;
            toastText.transform.SetAsLastSibling();
            bool hasBody = !string.IsNullOrEmpty(message);
            body.gameObject.SetActive(hasBody);
            RectTransform backdrop = panel.GetComponent<RectTransform>();
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            if (hasBody)
            {
                heading.rectTransform.anchoredPosition = new Vector2(0, 300);
                heading.fontSize = 54;
            }
            else
            {
                heading.rectTransform.anchoredPosition = new Vector2(0, 200);
                heading.fontSize = 64;
            }
            body.rectTransform.anchoredPosition = new Vector2(0, 50);
            locked = keepOpen;
            Time.timeScale = 0;
            buttonCount = 0;
            foreach (Button button in buttons)
            {
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        private void Add(string label, UnityAction action)
        {
            Button button = buttons[buttonCount];
            float top = 35;
            if (body.gameObject.activeSelf)
                top = -245;
            RectTransform position = button.GetComponent<RectTransform>();
            position.anchoredPosition = new Vector2(0, top - buttonCount * 70);
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.AddListener(action);
            button.gameObject.SetActive(true);
            buttonCount++;
        }

        public void Close()
        {
            panel.SetActive(false);
            locked = false;
            Time.timeScale = 1;
        }

        public void Toast(string message)
        {
            if (Game != null && Game.SaveFailed)
            {
                toastText.text = SaveError;
                showedSaveError = true;
            }
            else
            {
                toastText.text = message;
                toastTimer = 3;
            }
        }

        public void Dialogue(string message)
        {
            Open(message, "");
            Add("Close", Close);
        }

        public void Story(string title, string message)
        {
            Open(title, message);
            Add("Close", Close);
        }

        public void Title()
        {
            if (Game.Loading || (Game.Player != null && Game.Player.Dead))
                return;
            Open("COMPLETE-PEASANT", "", true);
            Add("Play", Play);
            Add("New game", ConfirmNewGame);
            Add("Controls", TitleControls);
            if (Application.isEditor)
                Add("Developer", Developer);
            else
                Add("Quit", Quit);
        }

        private void Play()
        {
            if (Application.isEditor)
                Game.LoadStage(developerStage);
            else
                Game.Begin(false);
        }

        private void Developer()
        {
            Open("Developer", "", true);
            Add("Scene: " + Game.stages[developerStage].title, NextDeveloperScene);
            Add("Play", Play);
            Add("Back", Title);
        }

        private void NextDeveloperScene()
        {
            developerStage++;
            if (developerStage >= Game.stages.Length)
                developerStage = 0;
            Developer();
        }
        private void ConfirmNewGame()
        {
            Open("Restart game?", "", true);
            Add("Restart", Restart);
            Add("Back", Title);
        }

        public void Pause()
        {
            if (Game.Loading || Game.Player.Dead)
                return;
            Open("Paused", "");
            Add("Resume", Close);
            Add("Controls", PauseControls);
            Add("Title", Title);
        }

        private void Controls(bool fromTitle)
        {
            string controls = "Move   A / D or arrows\nJump   Space\nAttack   Left click\nParry   Right click\nDown slash   Down + Left click (air)";
            if (Game.Data.HasSkill(Skill.EmberDash))
                controls += "\nDash   C";
            if (Game.Data.HasSkill(Skill.AshWings))
                controls += "\nDouble jump   Space twice";
            if (Game.Data.HasSkill(Skill.Sunbreak))
                controls += "\nSunbreak   F";
            controls += "\nUse   E\nPause   Esc";
            Open("Controls", controls, fromTitle);
            if (fromTitle)
                Add("Back", Title);
            else
                Add("Back", Pause);
        }

        private void TitleControls()
        {
            Controls(true);
        }

        private void PauseControls()
        {
            Controls(false);
        }

        private void Restart()
        {
            Game.Begin(true);
        }

        private void Quit()
        {
            Application.Quit();
        }

        public void Ending()
        {
            Open("Dawn restored", "The Desolate Sun is free.", true);
            Add("Title", Title);
            Add("Replay", ConfirmNewGame);
        }
    }
}
