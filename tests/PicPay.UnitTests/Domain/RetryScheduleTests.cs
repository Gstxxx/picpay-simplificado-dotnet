using PicPay.Domain.Notifications;

namespace PicPay.UnitTests.Domain;

public class RetryScheduleTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    [InlineData(10, 30)]
    public void Delay_doubles_each_attempt_up_to_the_cap(int attempt, int expectedSeconds)
    {
        var schedule = new RetrySchedule(20, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30));

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), schedule.DelayFor(attempt));
    }
}
