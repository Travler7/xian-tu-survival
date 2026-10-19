#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class Stage3PlayModeAcceptanceTests
{
    [Serializable]
    private sealed class SmokeReport
    {
        public bool passed;
        public List<string> checks;
    }

    [UnityTest]
    public IEnumerator IsolatedStage3SmokeCanRunFromAPlayModeTest()
    {
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "Stage3PlayModeSmoke.json"));
        if (File.Exists(path)) File.Delete(path);

        var runner = new GameObject("Stage3PlayModeAcceptanceRunner");
        var smokeType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => type.Name == "Stage3PlayModeSmokeRunner");
        Assert.That(smokeType, Is.Not.Null, "Stage3PlayModeSmokeRunner type is unavailable");
        runner.AddComponent(smokeType);

        var deadline = Time.realtimeSinceStartup + 15f;
        while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(File.Exists(path), Is.True, "Stage3 PlayMode smoke did not write its report before timeout");
        var report = JsonUtility.FromJson<SmokeReport>(File.ReadAllText(path));
        Assert.That(report, Is.Not.Null);
        Assert.That(report.passed, Is.True);
        Assert.That(report.checks, Is.Not.Null);
        Assert.That(report.checks.Count, Is.GreaterThanOrEqualTo(21));

        UnityEngine.Object.Destroy(runner);
        yield return null;
    }
}
#endif
