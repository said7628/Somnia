using UnityEngine;

namespace Somnia.UnityClient
{
    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        public PlayerIdentity CurrentUser { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
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
        }

        public void SetCurrentSlot(int slotNumber)
        {
            if (slotNumber > 0)
            {
                CurrentSlotNumber = slotNumber;
            }
        }

        public bool HasSession()
        {
            return !string.IsNullOrEmpty(AccessToken);
        }
    }
}
