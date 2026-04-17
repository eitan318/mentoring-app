using FluentAssertions;
using MentoringApp.Model;
using Xunit;

namespace MentoringApp.Tests.Model
{
    public class AvailabilitySlotTests
    {
        [Fact]
        public void TimeLabel_FormatsStartAndEndTime()
        {
            var start = new DateTime(2026, 4, 12, 10, 0, 0);
            var end = new DateTime(2026, 4, 12, 10, 30, 0);
            var slot = new AvailabilitySlot { StartTime = start, EndTime = end };

            var expectedLabel = $"{start:t} - {end:t}";
            slot.TimeLabel.Should().Be(expectedLabel);
        }

        [Fact]
        public void IsBooked_DefaultsFalse()
        {
            var slot = new AvailabilitySlot
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMinutes(30)
            };

            slot.IsBooked.Should().BeFalse();
        }
    }
}
