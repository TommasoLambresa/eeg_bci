using System;
using System.Collections.Generic;
using UnityEngine;

public static class GrazTrialPlanGenerator
{
    public static List<GrazTrialDefinition> Generate(
        ExperimentCuePreset preset,
        int? seed = null,
        bool shuffle = true)
    {
        if (preset == null)
        {
            throw new ArgumentNullException(nameof(preset));
        }

        preset.EnsureClassDefaults();

        var pool = new List<ExperimentCueClass>();
        foreach (var cueClass in preset.EnabledClasses)
        {
            for (int i = 0; i < preset.RepetitionsPerClass; i++)
            {
                pool.Add(cueClass);
            }
        }

        if (shuffle)
        {
            Shuffle(pool, seed ?? Environment.TickCount);
        }

        var plan = new List<GrazTrialDefinition>(pool.Count);
        for (int i = 0; i < pool.Count; i++)
        {
            plan.Add(new GrazTrialDefinition(i, pool[i]));
        }

        return plan;
    }

    private static void Shuffle<T>(IList<T> items, int seed)
    {
        var random = new System.Random(seed);

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = items[i];
            items[i] = items[j];
            items[j] = temp;
        }
    }
}
