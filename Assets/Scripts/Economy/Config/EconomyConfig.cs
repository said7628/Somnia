using System;
using System.Collections.Generic;
using UnityEngine;

namespace Somnia.Economy.Config
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Somnia/Economy/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Math Rewards")]
        [Min(0f)] public float scoreToYatzisFactor = 0.1f;
        [Min(1f)] public float minAgeMultiplier = 1f;
        [Min(1f)] public float maxAgeMultiplier = 1.6f;

        [Header("Platform Rewards")]
        [Min(0)] public int defaultPlatformSublevelReward = 25;
        public List<LevelRewardEntry> levelRewards = new();

        [Header("Island Bonuses")]
        public List<IslandBonusEntry> islandBonuses = new();

        [Header("Change System")]
        [Range(0f, 1f)] public float wrongChangeProbability = 0.30f;
        [Range(0.01f, 0.35f)] public float wrongChangeMinPercent = 0.01f;
        [Range(0.01f, 0.35f)] public float wrongChangeMaxPercent = 0.35f;
        [Min(0)] public int changeSuccessReward = 50;
        [Min(0)] public int changeFailurePenalty = 50;

        public int GetPlatformRewardOrDefault(int levelId)
        {
            var found = levelRewards.Find(r => r.levelId == levelId);
            return found != null ? found.fixedReward : defaultPlatformSublevelReward;
        }

        public float GetIslandBonus(int islandId)
        {
            var found = islandBonuses.Find(b => b.islandId == islandId);
            return found != null ? found.bonusMultiplier : 1f;
        }

        [Serializable]
        public class LevelRewardEntry
        {
            public int levelId;
            [Min(0)] public int fixedReward = 25;
        }

        [Serializable]
        public class IslandBonusEntry
        {
            public int islandId;
            [Min(1f)] public float bonusMultiplier = 1f;
        }
    }
}
