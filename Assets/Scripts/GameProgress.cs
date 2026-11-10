using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Jiaozi
{
    [Serializable]
    public sealed class GameSettings
    {
        public float master = 0.8f, music = 0.25f, effects = 0.6f;
        public int resolution;
        public bool fullscreen;
    }

    [Serializable]
    public sealed class LevelRecord
    {
        public string id;
        public int score, stars;
    }

    [Serializable]
    public sealed class GameProgress
    {
        public int version = 1;
        public bool tutorialComplete;
        public List<string> collections = new List<string>();
        public List<LevelRecord> records = new List<LevelRecord>();
        public GameSettings settings = new GameSettings();
        public static string Combination(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b)
                throw new ArgumentException("请选择两名不同工匠");
            return string.CompareOrdinal(a, b) < 0 ? a + "+" + b : b + "+" + a;
        }

        public bool Complete(string level, string a, string b, int tickets, int score, int stars, bool tutorialLearned)
        {
            if (tickets < 0 || score < 0 || stars < 0 || stars > 3)
                throw new ArgumentOutOfRangeException();
            var key = Combination(a, b);
            bool added = tickets > 0 && !collections.Contains(key);
            if (added)
                collections.Add(key);
            var record = records.Find(r => r.id == level);
            if (record == null)
            {
                record = new LevelRecord
                {
                    id = level
                };
                records.Add(record);
            }

            record.score = Math.Max(record.score, score);
            record.stars = Math.Max(record.stars, stars);
            tutorialComplete |= tutorialLearned;
            return added;
        }
    }

    public sealed class ProgressStore
    {
        public readonly string PathName;
        public string Notice { get; private set; } = "";

        public ProgressStore(string path)
        {
            PathName = path;
        }

        GameProgress Read(string path)
        {
            if (!File.Exists(path))
                return null;
            string json = File.ReadAllText(path);
            // JsonUtility accepts incomplete objects; require our schema marker and fields.
            if (!json.Contains("\"version\"") || !json.Contains("\"collections\"") || !json.Contains("\"records\"") || !json.Contains("\"settings\""))
                return null;
            var data = JsonUtility.FromJson<GameProgress>(json);
            if (data == null || data.version != 1 || data.collections == null || data.records == null || data.settings == null)
                return null;
            foreach (var record in data.records)
                if (record == null || string.IsNullOrEmpty(record.id) || record.score < 0 || record.stars < 0 || record.stars > 3)
                    return null;
            data.settings.master = Mathf.Clamp01(data.settings.master);
            data.settings.music = Mathf.Clamp01(data.settings.music);
            data.settings.effects = Mathf.Clamp01(data.settings.effects);
            data.settings.resolution = Mathf.Clamp(data.settings.resolution, 0, 2);
            return data;
        }

        public GameProgress Load()
        {
            Notice = "";
            try
            {
                var data = Read(PathName);
                if (data != null)
                    return data;
            }
            catch (Exception)
            {
            }

            try
            {
                var backup = Read(PathName + ".bak");
                if (backup != null)
                {
                    Notice = "存档异常，已读取上一份备份。";
                    return backup;
                }
            }
            catch (Exception)
            {
            }

            if (File.Exists(PathName) || File.Exists(PathName + ".bak"))
                Notice = "存档无法读取，当前使用初始进度；原文件保留。";
            return new GameProgress();
        }

        public bool Save(GameProgress data)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));
                string temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(PathName))
                {
                    bool valid = false;
                    try
                    {
                        valid = Read(PathName) != null;
                    }
                    catch (Exception)
                    {
                    }

                    if (valid)
                        File.Replace(temp, PathName, PathName + ".bak");
                    else
                    {
                        File.Copy(PathName, PathName + ".corrupt", true);
                        File.Replace(temp, PathName, null);
                    }
                }
                else
                    File.Move(temp, PathName);
                Notice = "";
                return true;
            }
            catch (Exception e)
            {
                Notice = "保存失败，本次进度仅保留在内存中：" + e.Message;
                return false;
            }
        }
    }
}
