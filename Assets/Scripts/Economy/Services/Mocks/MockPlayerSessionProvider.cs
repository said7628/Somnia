using Somnia.Economy.Interfaces;

namespace Somnia.Economy.Services.Mocks
{
    public class MockPlayerSessionProvider : IPlayerSessionProvider
    {
        public int CurrentUserId { get; set; } = 1;
        public int CurrentPlayerId { get; set; } = 1;
        public string Token { get; set; } = "mock-token";
        public bool IsAuthenticated { get; set; } = true;
        public int CurrentSlotNumber { get; set; } = 1;
    }
}
