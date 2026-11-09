using System;
using System.IO;
using UnityEngine;

namespace CompletePeasant.ShortGame
{
    [Serializable]
    public class Progress
    {
        public int stage;
        public int bossesDefeated;
        public int checkpoint, campaignVersion;
        public bool sunbreak;
        public bool HasSkill(Skill skill)
        {
            if (skill == Skill.EmberDash)
                return true;
            if (skill == Skill.AshWings)
                return bossesDefeated >= 2;
            if (skill == Skill.Sunbreak)
                return sunbreak;
            return true;
        }

        public static string FilePath
        {
            get
            {
                string folder = Application.persistentDataPath;
#if UNITY_EDITOR
                string testFolder = Environment.GetEnvironmentVariable("COMPLETE_PEASANT_TEST_SAVE");
                if (!string.IsNullOrEmpty(testFolder))
                    folder = testFolder;
#endif
                return Path.Combine(folder, "complete-peasant-short.json");
            }
        }

        public static Progress Load()
        {
            Progress data = Read(FilePath);
            if (data != null)
                return data;
            data = Read(FilePath + ".bak");
            if (data != null)
                return data;
            return new Progress();
        }

        private static Progress Read(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;
                Progress data = JsonUtility.FromJson<Progress>(File.ReadAllText(path));
                if (data == null)
                    return null;
                if (data.campaignVersion < 2)
                {
                    if (data.stage == 2)
                    {
                        data.stage = 4;
                        data.sunbreak = true;
                    }

                    if (data.bossesDefeated >= 3)
                        data.bossesDefeated = 5;
                    data.campaignVersion = 2;
                }

                data.stage = Mathf.Clamp(data.stage, 0, 4);
                data.bossesDefeated = Mathf.Clamp(data.bossesDefeated, 0, 5);
                data.checkpoint = 0;
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool Save()
        {
            try
            {
                campaignVersion = 2;
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(this, true));
                if (Read(FilePath) != null)
                    File.Copy(FilePath, FilePath + ".bak", true);
                File.Copy(temporary, FilePath, true);
                File.Delete(temporary);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("Could not save: " + error.Message);
                return false;
            }
        }
    }
}
