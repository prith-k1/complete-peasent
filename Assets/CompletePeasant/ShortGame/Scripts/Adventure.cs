using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CompletePeasant.ShortGame
{
    public class Adventure : MonoBehaviour
    {
        public static Adventure Instance { get; private set; }

        public StageData[] stages;
        public AdventureUI ui;
        public AudioSource music, effects;
        public AudioClip attackSound, hurtSound, restSound;
        public Progress Data { get; private set; }
        public Peasant Player { get; private set; }
        public StageRoot Stage { get; private set; }
        public bool Loading { get; private set; }
        public bool SaveFailed { get; private set; }

        private int boundScene = -1;
        private void Awake()
        {
            if (Instance != null)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GetComponentInChildren<EventSystem>(true).enabled = true;
            Data = Progress.Load();
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void Start()
        {
            Bind();
            ui.Title();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;
            SceneManager.sceneLoaded -= SceneLoaded;
            Instance = null;
            Time.timeScale = 1;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Bind();
            Loading = false;
        }

        private void Bind()
        {
            int handle = SceneManager.GetActiveScene().handle;
            if (boundScene == handle)
                return;
            boundScene = handle;
            Stage = FindObjectOfType<StageRoot>();
            Player = FindObjectOfType<Peasant>();
            if (Stage == null || Player == null)
                return;
            Vector3 spawn = Stage.CheckpointPosition(Data.stage == Stage.data.index ? Data.checkpoint : 0);
            Player.transform.position = spawn;
            Player.GetComponent<Rigidbody2D>().position = spawn;
            Player.Restore();
            music.clip = Stage.data.music;
            music.Play();
            ui.Bind(Stage.data);
        }

        public void Begin(bool newGame)
        {
            if (newGame)
            {
                Data = new Progress();
                Save();
            }

            LoadStage(Data.stage);
        }

        public void LoadStage(int index)
        {
            if (Loading || index < 0 || index >= stages.Length)
                return;
            Loading = true;
            ui.Close();
            if (Data.stage != index)
                Data.checkpoint = 0;
            Data.stage = index;
            Save();
            SceneManager.LoadScene(stages[index].sceneName);
        }

        public void Rest()
        {
            Data.stage = Stage.data.index;
            Data.checkpoint = 0;
            Player.Heal(2);
            bool saved = Save();
            Sound(restSound);
            if (saved)
                ui.Toast("Saved");
        }

        public void BossDefeated()
        {
            Data.bossesDefeated = Mathf.Max(Data.bossesDefeated, Stage.data.index + 1);
            bool saved = Save();
            Player.Restore();
            music.clip = Stage.data.music;
            music.Play();
            if (Stage.data.index == stages.Length - 1)
                ui.Ending();
            else if (Stage.data.reward != Skill.None)
                ShowSkill(Stage.data.reward);
            else if (saved)
                ui.Toast("Path opened");
        }

        public void GrantSkill(Skill skill)
        {
            if (skill == Skill.None)
                return;
            if (Data.HasSkill(skill))
            {
                ui.Story("The Desolate Sun", "Its last ember lives within you.");
                return;
            }

            if (skill == Skill.Sunbreak)
                Data.sunbreak = true;
            Save();
            ShowSkill(skill);
        }

        private void ShowSkill(Skill skill)
        {
            if (skill == Skill.EmberDash)
                ui.Story("Ember Dash", "C · Dash\nThe first ember breaks the dark.");
            else if (skill == Skill.AshWings)
                ui.Story("Ash Wings", "Space · Jump again in the air\nCarry the lost sun's warmth below.");
            else if (skill == Skill.Sunbreak)
                ui.Story("Sunbreak", "F · Cast sunlight\nBreak the Kindly Sun's seal.");
        }

        public void Proceed()
        {
            if (Stage.data.hasBoss && Data.bossesDefeated <= Stage.data.index)
            {
                ui.Toast("Defeat the boss first");
                return;
            }

            if (Stage.data.reward == Skill.Sunbreak && !Data.HasSkill(Skill.Sunbreak))
            {
                ui.Toast("Rekindle the Desolate Sun");
                return;
            }

            if (Stage.data.index == stages.Length - 1)
                ui.Ending();
            else
                LoadStage(Stage.data.index + 1);
        }

        public IEnumerator Respawn()
        {
            if (Loading)
                yield break;
            Loading = true;
            ui.Toast("Respawning");
            yield return new WaitForSeconds(0.9f);
            Loading = false;
            LoadStage(Data.stage);
        }

        public bool Save()
        {
            SaveFailed = !Data.Save();
            return !SaveFailed;
        }

        public void Sound(AudioClip clip)
        {
            if (clip != null)
                effects.PlayOneShot(clip, 0.55f);
        }
    }
}
