namespace Magnus
{
    static class GameStateExtension
    {
        public static bool IsOneOf(this GameState gameState, GameState mask)
        {
            return mask.HasFlag(gameState);
        }
    }
}
