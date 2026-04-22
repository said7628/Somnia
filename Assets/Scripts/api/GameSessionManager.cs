using UnityEngine;

namespace Somnia.UnityClient
{
    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        public PlayerIdentity CurrentUser { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public int PlayerAge { get; private set; }
        public int CurrentSlotNumber { get; private set; } = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetSession(PlayerIdentity user, TokenBundle tokens)
        {
            CurrentUser = user;
            AccessToken = tokens?.accessToken;
            RefreshToken = tokens?.refreshToken;
            SetPlayerAgeFromBackend(user?.edad_jugador ?? 0);
        }

        public void SetPlayerAgeFromBackend(int edadJugador)
        {
            PlayerAge = Mathf.Max(0, edadJugador);
            TextTypingSession.PlayerAge = PlayerAge;
        }

        public void SetCurrentSlot(int slotNumber)
        {
            if (slotNumber > 0)
            {
                CurrentSlotNumber = slotNumber;
            }
        }

        public void SetCurrentIslandSceneForSlot(int slotNumber, string islandSceneName)
        {
            if (slotNumber <= 0 || string.IsNullOrWhiteSpace(islandSceneName))
            {
                return;
            }

            PlayerPrefs.SetString($"slot_{slotNumber}_current_island_scene", islandSceneName);
            PlayerPrefs.Save();
        }

        public string GetCurrentIslandSceneForSlot(int slotNumber)
        {
            if (slotNumber <= 0)
            {
                return null;
            }

            string key = $"slot_{slotNumber}_current_island_scene";
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        }

        public bool HasSession()
        {
            return !string.IsNullOrEmpty(AccessToken);
        }

        public int GetTextTypingMinimumScore()
        {
            int age = PlayerAge;
            return TextTypingPlayerRules.ObtenerPuntajeMinimoPorEdad(age);
        }
    }
}