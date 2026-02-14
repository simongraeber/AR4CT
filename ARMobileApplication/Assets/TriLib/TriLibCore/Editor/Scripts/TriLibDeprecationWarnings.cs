using UnityEditor;

namespace TriLibCore.Editor
{
    [InitializeOnLoad]
    public static class TriLibDeprecationWarnings
    {
        private const string Define = "TRILIB_USE_SOURCE_LIBS";
        private const string DecisionPrefKey = "TriLib.SourceLibs.UserDecisionMade";

        static TriLibDeprecationWarnings()
        {
            EditorApplication.delayCall += TryPrompt;
        }

        private static void TryPrompt()
        {
            if (TriLibDefineSymbolsHelper.IsSymbolDefined(Define))
            {
                RememberDecision();
                return;
            }

#if UNITY_6000_0_OR_NEWER
            const bool isUnity6OrNewer = true;
#else
            const bool isUnity6OrNewer = false;
#endif

            if (!isUnity6OrNewer && EditorPrefs.GetBool(DecisionPrefKey, false))
            {
                return;
            }

            var enable = EditorUtility.DisplayDialog(
                "TriLib – Source Libraries (Required on Unity 6+)",
                "TriLib detected that the Source Libraries mode is not enabled.\n\n" +
                "On Unity 6 and newer versions, Source Libraries are REQUIRED due to\n" +
                "frequent managed ABI changes in Unity.\n\n" +
                "Using precompiled libraries may lead to build errors or editor crashes.\n\n" +
                "You can manage this option later at:\n" +
                "Edit → Project Settings → TriLib\n\n" +
                "Do you want to enable Source Libraries now?",
                "Enable (Recommended)",
                "Not now"
            );

            if (enable)
            {
                TriLibDefineSymbolsHelper.UpdateSymbol(Define, true);

                EditorUtility.DisplayDialog(
                    "TriLib – Source Libraries Enabled",
                    "Source Libraries mode has been enabled.\n\n" +
                    "Unity will now recompile scripts.\n\n" +
                    "You can change this option later at:\n" +
                    "Edit → Project Settings → TriLib",
                    "OK"
                );
            }

            if (!isUnity6OrNewer)
            {
                RememberDecision();
            }
        }

        private static void RememberDecision()
        {
            EditorPrefs.SetBool(DecisionPrefKey, true);
        }
    }
}
