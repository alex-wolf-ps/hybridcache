namespace CachingDemo
{
    public class SlowDateService()
    {
        // Simulate a slow database or service call
        public async Task<string> GetSlowDateAsync()
        {
            await Task.Delay(3000);

            return DateTime.Now.TimeOfDay.ToString();
        }
    }
}
