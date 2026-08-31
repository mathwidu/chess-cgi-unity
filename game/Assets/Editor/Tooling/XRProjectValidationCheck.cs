using System.Collections.Generic;
using System.Reflection;
using Unity.XR.CoreUtils.Editor;
using UnityEditor;
using UnityEngine;

public static class XRProjectValidationCheck
{
    [MenuItem("Chess CGI/VR/Run XR Project Validation Check (Standalone)")]
    public static void RunStandaloneCheck()
    {
        MethodInfo getIssues = typeof(BuildValidator).GetMethod(
            "GetCurrentValidationIssues", BindingFlags.NonPublic | BindingFlags.Static);
        if (getIssues == null)
        {
            Debug.LogError("CHESS_CGI_XR_VALIDATION_CHECK FAILED reason=\"BuildValidator.GetCurrentValidationIssues not found\"");
            EditorApplication.Exit(1);
            return;
        }

        HashSet<BuildValidationRule> failures = new HashSet<BuildValidationRule>();
        getIssues.Invoke(null, new object[] { failures, BuildTargetGroup.Standalone });

        Debug.Log($"CHESS_CGI_XR_VALIDATION_CHECK platform=Standalone issueCount={failures.Count}");

        XRVerificationResult result = new XRVerificationResult();
        foreach (BuildValidationRule failure in failures)
        {
            if (failure.Error)
            {
                result.Check(false, $"blocking validation issue: category=\"{failure.Category}\" message=\"{failure.Message}\"");
            }
            else
            {
                Debug.LogWarning($"CHESS_CGI_XR_VALIDATION_CHECK issue category=\"{failure.Category}\" message=\"{failure.Message}\" error={failure.Error}");
            }
        }

        result.LogSummary("CHESS_CGI_XR_VALIDATION_CHECK");
        EditorApplication.Exit(result.Passed ? 0 : 1);
    }
}
