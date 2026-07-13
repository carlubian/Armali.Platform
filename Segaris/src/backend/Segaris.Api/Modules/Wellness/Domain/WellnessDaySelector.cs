namespace Segaris.Api.Modules.Wellness.Domain;

internal sealed record WellnessSelectableTask(int Id, string Name, WellnessCategory Category);

internal sealed record WellnessSelectedTask(string Name, WellnessCategory Category);

internal sealed class WellnessDaySelector
{
    private readonly Random random;

    public WellnessDaySelector()
        : this(Random.Shared)
    {
    }

    internal WellnessDaySelector(Random random)
    {
        this.random = random;
    }

    public IReadOnlyList<WellnessSelectedTask> Select(IReadOnlyList<WellnessSelectableTask> catalogue)
    {
        if (catalogue.Count <= WellnessDefaults.DailyTaskCount)
        {
            return Shuffle(catalogue)
                .Select(ToSelected)
                .ToArray();
        }

        var selected = new List<WellnessSelectableTask>(WellnessDefaults.DailyTaskCount);
        var categories = catalogue
            .GroupBy(task => task.Category)
            .Select(group => new CategoryBucket(group.Key, group.ToArray()))
            .ToArray();

        if (categories.Length <= WellnessDefaults.DailyTaskCount)
        {
            foreach (var category in Shuffle(categories))
            {
                selected.Add(PickOne(category.Tasks));
            }

            var selectedIds = selected.Select(task => task.Id).ToHashSet();
            var remaining = catalogue
                .Where(task => !selectedIds.Contains(task.Id))
                .ToArray();

            selected.AddRange(Shuffle(remaining).Take(WellnessDefaults.DailyTaskCount - selected.Count));
        }
        else
        {
            foreach (var category in Shuffle(categories).Take(WellnessDefaults.DailyTaskCount))
            {
                selected.Add(PickOne(category.Tasks));
            }
        }

        return selected
            .Select(ToSelected)
            .ToArray();
    }

    private T PickOne<T>(IReadOnlyList<T> values) => values[random.Next(values.Count)];

    private IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> values)
    {
        var shuffled = values.ToArray();
        random.Shuffle(shuffled);
        return shuffled;
    }

    private static WellnessSelectedTask ToSelected(WellnessSelectableTask task) =>
        new(task.Name, task.Category);

    private sealed record CategoryBucket(WellnessCategory Category, IReadOnlyList<WellnessSelectableTask> Tasks);
}
