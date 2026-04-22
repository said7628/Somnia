using UnityEngine;
using System;
using System.Globalization;

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

        public string GetBirthDateRaw()
        {
            return CurrentUser?.fecha_nacimiento;
        }

        public bool TryGetBirthDate(out DateTime birthDate)
        {
            birthDate = default;

            var raw = GetBirthDateRaw();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            // intenta parsear formatos comunes
            string[] formats =
            {
                "yyyy-MM-dd",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ",
                "MM/dd/yyyy",
                "dd/MM/yyyy"
            };

            if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out birthDate))
            {
                return true;
            }

            if (DateTime.TryParse(raw, out birthDate))
            {
                return true;
            }

            return false;
        }

        public int GetPlayerAge()
        {
            if (!TryGetBirthDate(out DateTime birthDate))
                return -1;

            DateTime today = DateTime.Today;
            int age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age))
                age--;

            return age;
        }

        public int GetTextTypingMinimumScore()
        {
            int age = GetPlayerAge();

            if (age >= 7 && age <= 10)
                return 1000;

            if (age >= 11)
                return 1700;

            return 1700;
        }
    }
}