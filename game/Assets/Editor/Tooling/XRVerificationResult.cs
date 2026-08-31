using System.Collections.Generic;
using UnityEngine;

public sealed class XRVerificationResult
{
    private readonly List<string> failures = new List<string>();

    public bool Passed => failures.Count == 0;

    public void Check(bool condition, string description)
    {
        if (!condition)
        {
            failures.Add(description);
        }
    }

    public void LogSummary(string checkName)
    {
        if (Passed)
        {
            Debug.Log($"{checkName} PASSED");
            return;
        }

        foreach (string failure in failures)
        {
            Debug.LogError($"{checkName} FAILED: {failure}");
        }
    }
}
