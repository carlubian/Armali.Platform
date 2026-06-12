using Segaris.Api.Platform.Jobs;

namespace Segaris.UnitTests;

public sealed class JobStateMachineTests
{
    [Fact]
    public void Allows_documented_transitions()
    {
        (JobState From, JobState To)[] allowed =
        [
            (JobState.Queued, JobState.Running),
            (JobState.Queued, JobState.CancellationRequested),
            (JobState.Queued, JobState.Cancelled),
            (JobState.Queued, JobState.Interrupted),
            (JobState.Running, JobState.Succeeded),
            (JobState.Running, JobState.Failed),
            (JobState.Running, JobState.CancellationRequested),
            (JobState.Running, JobState.Cancelled),
            (JobState.Running, JobState.Interrupted),
            (JobState.CancellationRequested, JobState.Cancelled),
            (JobState.CancellationRequested, JobState.Succeeded),
            (JobState.CancellationRequested, JobState.Failed),
            (JobState.CancellationRequested, JobState.Interrupted),
        ];

        Assert.All(allowed, transition =>
            Assert.True(JobStateMachine.CanTransition(transition.From, transition.To)));
    }

    [Fact]
    public void Rejects_impossible_transitions()
    {
        (JobState From, JobState To)[] rejected =
        [
            (JobState.Queued, JobState.Succeeded),
            (JobState.Queued, JobState.Failed),
            (JobState.Running, JobState.Queued),
            (JobState.Succeeded, JobState.Running),
            (JobState.Failed, JobState.Running),
            (JobState.Cancelled, JobState.Running),
            (JobState.Interrupted, JobState.Running),
        ];

        Assert.All(rejected, transition =>
        {
            Assert.False(JobStateMachine.CanTransition(transition.From, transition.To));
            Assert.Throws<InvalidOperationException>(() =>
                JobStateMachine.EnsureCanTransition(transition.From, transition.To));
        });
    }

    [Fact]
    public void Terminal_states_are_recognized()
    {
        JobState[] terminal =
        [
            JobState.Succeeded,
            JobState.Failed,
            JobState.Cancelled,
            JobState.Interrupted,
        ];
        JobState[] active =
        [
            JobState.Queued,
            JobState.Running,
            JobState.CancellationRequested,
        ];

        Assert.All(terminal, state => Assert.True(JobStates.IsTerminal(state)));
        Assert.All(active, state => Assert.False(JobStates.IsTerminal(state)));
    }
}
