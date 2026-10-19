using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using WoguaShooter.Lua;

namespace WoguaShooter.UI
{
    [Serializable]
    public sealed class LeaderboardEntry
    {
        public string playerName;
        public int score;

        public LeaderboardEntry(string playerName, int score)
        {
            this.playerName = playerName;
            this.score = score;
        }
    }

    public static class LeaderboardService
    {
        private const int DefaultMaximumEntries = 5;
        private const int DefaultMaximumNameLength = 6;
        private const int DefaultScoreToUnlockNextLevel = 10000;

        public static int MaximumEntries => ReadRule("maximumEntries", DefaultMaximumEntries, 1);
        public static int MaximumNameLength => ReadRule("maximumNameLength", DefaultMaximumNameLength, 1);
        public static int ScoreToUnlockNextLevel => ReadRule(
            "scoreToUnlockNextLevel", DefaultScoreToUnlockNextLevel, 0);

        private const string SaveFileName = "game_data.json";
        private const string LegacySaveKey = "WoguaShooter.Leaderboards.V1";
        private static LeaderboardSaveData cachedData;

        private static int ReadRule(string fieldName, int fallback, int minimum)
        {
            int value = LuaHotUpdateRuntime.Instance != null
                ? LuaHotUpdateRuntime.Instance.GetInt("ProgressionRules", fieldName, fallback)
                : fallback;
            return Mathf.Max(minimum, value);
        }

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static List<LeaderboardEntry> GetEntries(int levelId)
        {
            LevelLeaderboardData level = GetLevel(levelId, false);
            List<LeaderboardEntry> result = new List<LeaderboardEntry>();
            if (level == null)
                return result;

            for (int i = 0; i < level.entries.Count; i++)
            {
                LeaderboardEntry entry = level.entries[i];
                result.Add(new LeaderboardEntry(entry.playerName, entry.score));
            }
            return result;
        }

        public static void InitializeSaveData()
        {
            EnsureLoaded();
            if (!File.Exists(SaveFilePath))
                Save();
        }

        public static bool Qualifies(int levelId, int score)
        {
            LevelLeaderboardData level = GetLevel(levelId, false);
            if (level == null || level.entries.Count < MaximumEntries)
                return true;

            return score > level.entries[level.entries.Count - 1].score;
        }

        public static int GetHighestScore(int levelId)
        {
            LevelLeaderboardData level = GetLevel(levelId, false);
            return level != null ? level.highestScore : 0;
        }

        public static bool IsLevelUnlocked(int levelId)
        {
            int safeLevelId = Mathf.Max(1, levelId);
            return safeLevelId == 1 || GetHighestScore(safeLevelId - 1) >= ScoreToUnlockNextLevel;
        }

        public static void RecordHighestScore(int levelId, int score)
        {
            LevelLeaderboardData level = GetLevel(levelId, true);
            int safeScore = Mathf.Max(0, score);
            if (safeScore <= level.highestScore)
                return;

            level.highestScore = safeScore;
            Save();
        }

        public static bool TryAddScore(int levelId, string rawName, int score, out string error)
        {
            string playerName = rawName != null ? rawName.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                error = "请输入名字";
                return false;
            }

            if (new StringInfo(playerName).LengthInTextElements > MaximumNameLength)
            {
                error = "名字最多6个字";
                return false;
            }

            LevelLeaderboardData level = GetLevel(levelId, true);
            for (int i = 0; i < level.entries.Count; i++)
            {
                if (string.Equals(level.entries[i].playerName, playerName, StringComparison.OrdinalIgnoreCase))
                {
                    error = "这个名字已经上榜，请换一个";
                    return false;
                }
            }

            if (!Qualifies(levelId, score))
            {
                error = "排行榜已更新，本次分数未进入前五";
                return false;
            }

            int safeScore = Mathf.Max(0, score);
            level.entries.Add(new LeaderboardEntry(playerName, safeScore));
            level.entries.Sort((left, right) => right.score.CompareTo(left.score));
            if (level.entries.Count > MaximumEntries)
                level.entries.RemoveRange(MaximumEntries, level.entries.Count - MaximumEntries);
            if (safeScore > level.highestScore)
                level.highestScore = safeScore;

            Save();
            error = string.Empty;
            return true;
        }

        private static LevelLeaderboardData GetLevel(int levelId, bool create)
        {
            EnsureLoaded();
            int safeLevelId = Mathf.Max(1, levelId);
            for (int i = 0; i < cachedData.levels.Count; i++)
            {
                if (cachedData.levels[i].levelId == safeLevelId)
                    return cachedData.levels[i];
            }

            if (!create)
                return null;

            LevelLeaderboardData level = new LevelLeaderboardData { levelId = safeLevelId };
            cachedData.levels.Add(level);
            return level;
        }

        private static void EnsureLoaded()
        {
            if (cachedData != null)
                return;

            bool migratedLegacyData = false;
            string json = string.Empty;
            try
            {
                if (File.Exists(SaveFilePath))
                    json = File.ReadAllText(SaveFilePath);
                else
                {
                    json = PlayerPrefs.GetString(LegacySaveKey, string.Empty);
                    migratedLegacyData = !string.IsNullOrEmpty(json);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"游戏存档读取失败，将创建新存档：{exception.Message}");
            }

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    cachedData = JsonUtility.FromJson<LeaderboardSaveData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"排行榜存档读取失败，将创建新存档：{exception.Message}");
                }
            }

            if (cachedData == null)
                cachedData = new LeaderboardSaveData();
            if (cachedData.levels == null)
                cachedData.levels = new List<LevelLeaderboardData>();

            for (int i = 0; i < cachedData.levels.Count; i++)
            {
                LevelLeaderboardData level = cachedData.levels[i];
                if (level.entries == null)
                    level.entries = new List<LeaderboardEntry>();
                level.entries.RemoveAll(entry => entry == null || string.IsNullOrWhiteSpace(entry.playerName));
                level.entries.Sort((left, right) => right.score.CompareTo(left.score));
                if (level.entries.Count > MaximumEntries)
                    level.entries.RemoveRange(MaximumEntries, level.entries.Count - MaximumEntries);
                for (int entryIndex = 0; entryIndex < level.entries.Count; entryIndex++)
                    level.highestScore = Mathf.Max(level.highestScore, level.entries[entryIndex].score);
            }

            if (migratedLegacyData)
                Save();
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporaryPath = SaveFilePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(cachedData, true));
                File.Copy(temporaryPath, SaveFilePath, true);
                File.Delete(temporaryPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"游戏存档写入失败：{exception.Message}");
            }
        }

        [Serializable]
        private sealed class LeaderboardSaveData
        {
            public List<LevelLeaderboardData> levels = new List<LevelLeaderboardData>();
        }

        [Serializable]
        private sealed class LevelLeaderboardData
        {
            public int levelId = 1;
            public int highestScore;
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        }
    }
}
