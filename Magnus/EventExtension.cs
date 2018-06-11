namespace Magnus
{
    static class EventExtension
    {
        public static bool HasOneOfEvents(this Event events, Event mask)
        {
            return (events & mask) != 0;
        }
    }
}
