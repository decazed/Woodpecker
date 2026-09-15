using FluentAssertions;
using Woodpecker.Domain;

namespace Woodpecker.Domain.Tests;

public class EntityTests
{
    // TrainingCycle sert de support concret pour tester le comportement générique
    // hérité de la classe de base Entity (égalité par Id).
    [Fact]
    public void SameReference_IsEqualToItself()
    {
        var cycle = TrainingCycle.Start(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);
        var sameCycle = cycle;

        (cycle == sameCycle).Should().BeTrue();
        cycle.Equals(sameCycle).Should().BeTrue();
    }

    [Fact]
    public void TwoDifferentCycles_AreNotEqual()
    {
        var cycle1 = TrainingCycle.Start(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);
        var cycle2 = TrainingCycle.Start(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

        (cycle1 == cycle2).Should().BeFalse();
    }
}
