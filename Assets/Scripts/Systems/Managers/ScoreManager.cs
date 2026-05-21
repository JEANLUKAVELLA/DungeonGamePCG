using UnityEngine;
using UnityEngine.Events;

namespace DungeonGame.Systems.Managers
{
    /// <summary>
    /// Persistent Singleton Manager that tracks the global game progression states, 
    /// including player scores, level time, registry counts for active enemies and keys, and current dungeon level indices.
    /// Supports a boss-level double score feature (every 5th level).
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager _instance;
        
        /// <summary>
        /// Global singleton access property. Auto-instantiates a ScoreManager GameObject 
        /// and marks it as DontDestroyOnLoad if one doesn't exist in the scene.
        /// </summary>
        public static ScoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ScoreManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ScoreManager");
                        _instance = go.AddComponent<ScoreManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private int score = 0;
        private int enemyCount = 0;
        private int keysCollected = 0;
        private int totalKeys = 0;
        private int currentLevel = 1;

        private float levelTimer = 0f;
        private bool isTimerRunning = false;

        public UnityEvent<int> OnScoreChanged;
        public UnityEvent<int> OnEnemyCountChanged;
        public UnityEvent<int, int> OnKeysChanged; // collected, total

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (isTimerRunning)
            {
                levelTimer += Time.deltaTime;
            }
        }

        /// <summary>
        /// Adds score points to the player's total. Points are doubled during Boss Levels (every 5th level).
        /// </summary>
        /// <param name="amount">The base score amount to add.</param>
        public void AddScore(int amount)
        {
            bool isBossLevel = currentLevel % 5 == 0;
            score += isBossLevel ? (amount * 2) : amount;
            OnScoreChanged?.Invoke(score);
        }

        /// <summary>
        /// Registers a spawned enemy, incrementing the level's total enemy count.
        /// </summary>
        public void RegisterEnemy()
        {
            enemyCount++;
            OnEnemyCountChanged?.Invoke(enemyCount);
        }

        /// <summary>
        /// Decrements the active enemy counter when an enemy is defeated or removed.
        /// </summary>
        public void UnregisterEnemy()
        {
            enemyCount--;
            if (enemyCount < 0) enemyCount = 0;
            OnEnemyCountChanged?.Invoke(enemyCount);
        }

        /// <summary>
        /// Registers a spawned key crystal, incrementing the level's total required keys.
        /// </summary>
        public void RegisterKey()
        {
            totalKeys++;
            OnKeysChanged?.Invoke(keysCollected, totalKeys);
        }

        /// <summary>
        /// Increments the collected key counter.
        /// </summary>
        public void CollectKey()
        {
            keysCollected++;
            OnKeysChanged?.Invoke(keysCollected, totalKeys);
        }

        /// <summary>
        /// Resets all global player stats, timers, and level counts back to initial states (e.g., on game restarts).
        /// </summary>
        public void ResetStats()
        {
            score = 0;
            enemyCount = 0;
            keysCollected = 0;
            totalKeys = 0;
            currentLevel = 1;
            levelTimer = 0f;
            isTimerRunning = false;
            OnScoreChanged?.Invoke(score);
            OnEnemyCountChanged?.Invoke(enemyCount);
            OnKeysChanged?.Invoke(keysCollected, totalKeys);
        }

        /// <summary>
        /// Clears level-specific targets (enemies and keys) in preparation for generating the next dungeon level.
        /// </summary>
        public void ResetForNextLevel()
        {
            enemyCount = 0;
            keysCollected = 0;
            totalKeys = 0;
            OnEnemyCountChanged?.Invoke(enemyCount);
            OnKeysChanged?.Invoke(keysCollected, totalKeys);
        }

        /// <summary>
        /// Increments the current level index.
        /// </summary>
        public void IncrementLevel()
        {
            currentLevel++;
            Debug.Log($"[ScoreManager] Level progressed to {currentLevel}"); // for testing purposes
        }

        public int GetCurrentLevel() => currentLevel;

        /// <summary>
        /// Starts the level stopwatch timer.
        /// </summary>
        public void StartLevelTimer()
        {
            levelTimer = 0f;
            isTimerRunning = true;
            Debug.Log("[ScoreManager] Level timer started!"); // for testing purposes
        }

        /// <summary>
        /// Stops the level stopwatch timer.
        /// </summary>
        public void StopLevelTimer()
        {
            isTimerRunning = false;
            Debug.Log($"[ScoreManager] Level timer stopped. Time elapsed: {levelTimer:F2} seconds."); // for testing purposes
        }

        public float GetLevelTime() => levelTimer;

        /// <summary>
        /// Resets elapsed time to zero and resumes the stopwatch timer.
        /// </summary>
        public void ResetLevelTime()
        {
            levelTimer = 0f;
            isTimerRunning = true;
        }
        
        public int GetScore() => score;
        public int GetEnemyCount() => enemyCount;
        public int GetKeysCollected() => keysCollected;
        public int GetTotalKeys() => totalKeys;
        
        /// <summary>
        /// Checks if all registered keys on this floor have been collected by the player.
        /// </summary>
        public bool AllKeysCollected() => keysCollected >= totalKeys && totalKeys > 0;

        /// <summary>
        /// Checks if all registered enemies on this floor have been defeated.
        /// </summary>
        public bool AllEnemiesDefeated() => enemyCount <= 0;
    }
}
