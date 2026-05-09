using System.Collections.Generic;
using Bob.SharedMobility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityDebug = UnityEngine.Debug;
using UnityObject = UnityEngine.Object;

namespace Bob.SharedMobility.EditorTools
{
    public static class UIPageProductionValidator
    {
        private const string MenuPath = "Tools/Bob Shared Mobility/Validate UI Page Architecture";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            ValidationReport report = new ValidationReport();

            AppNavigationService navigationService = FindFirstSceneComponent<AppNavigationService>();
            if (!navigationService)
            {
                report.Error("No AppNavigationService found in the loaded scene.");
                report.Flush();
                return;
            }

            navigationService.ResolveReferences();
            navigationService.RebuildRegistry();
            navigationService.ValidateNavigationRoutes();

            ValidateRouteTable(navigationService, report);
            ValidateDockBindings(navigationService, report);
            ValidateScreenControllers(report);
            ValidateDirectNavigationBindings(report);
            ValidatePrototypeImageUsage(report);

            report.Flush();
        }

        private static void ValidateRouteTable(AppNavigationService navigationService, ValidationReport report)
        {
            AppNavigationRouteTable routeTable = navigationService.RouteTable;
            if (!routeTable)
            {
                report.Error("AppNavigationService.routeTable is not assigned.");
                return;
            }

            Selection.activeObject = routeTable;

            HashSet<AppScreenId> routeIds = new HashSet<AppScreenId>();
            foreach (AppNavigationRouteTable.Route route in routeTable.Routes)
            {
                if (route == null)
                {
                    report.Error("Route table contains a null route entry.");
                    continue;
                }

                if (route.screenId == AppScreenId.None)
                {
                    report.Error("Route table contains a route with ScreenId.None.");
                    continue;
                }

                if (!routeIds.Add(route.screenId))
                {
                    report.Error($"Route table contains duplicate route '{route.screenId}'.");
                }

                if (string.IsNullOrWhiteSpace(route.routeName))
                {
                    report.Warning($"Route '{route.screenId}' has no human-readable routeName.");
                }

                if (route.productionStatus == AppNavigationRouteTable.ProductionStatus.ProductionReady
                    && route.presentationMode == AppNavigationRouteTable.PresentationMode.PrototypeImage)
                {
                    report.Error($"Route '{route.screenId}' is ProductionReady but still marked as PrototypeImage.");
                }
            }

            report.Info($"Route table checked: {routeIds.Count} route(s).");
        }

        private static void ValidateDockBindings(AppNavigationService navigationService, ValidationReport report)
        {
            SerializedObject serializedService = new SerializedObject(navigationService);
            SerializedProperty dockScreens = serializedService.FindProperty("dockScreens");

            if (dockScreens == null || !dockScreens.isArray)
            {
                report.Error("AppNavigationService.dockScreens could not be inspected.");
                return;
            }

            HashSet<AppScreenId> dockIds = new HashSet<AppScreenId>();
            for (int i = 0; i < dockScreens.arraySize; i++)
            {
                SerializedProperty binding = dockScreens.GetArrayElementAtIndex(i);
                AppScreenId screenId = (AppScreenId)binding.FindPropertyRelative("screenId").intValue;
                UnityObject dockPanel = binding.FindPropertyRelative("dockPanel").objectReferenceValue;

                if (screenId == AppScreenId.None)
                {
                    report.Error($"Dock binding #{i} uses ScreenId.None.");
                }

                if (!dockPanel)
                {
                    report.Error($"Dock binding '{screenId}' has no DockPanelController reference.");
                }

                if (!dockIds.Add(screenId))
                {
                    report.Error($"Dock binding contains duplicate screen id '{screenId}'.");
                }

                if (navigationService.RouteTable && !navigationService.RouteTable.Contains(screenId))
                {
                    report.Error($"Dock binding '{screenId}' is missing from AppNavigationRouteTable.");
                }
            }

            report.Info($"Dock binding registry checked: {dockScreens.arraySize} binding(s).");
        }

        private static void ValidateScreenControllers(ValidationReport report)
        {
            int count = 0;
            foreach (AppScreenController screen in FindSceneComponents<AppScreenController>())
            {
                count++;

                if (screen.ScreenId == AppScreenId.None)
                {
                    report.Error($"AppScreenController '{screen.name}' uses ScreenId.None.");
                }

                if (!screen.CanvasGroup)
                {
                    report.Error($"AppScreenController '{screen.name}' has no CanvasGroup assigned.");
                }

                if (!screen.GetComponent<AppScreenLifecycleController>())
                {
                    report.Warning($"AppScreenController '{screen.name}' has no AppScreenLifecycleController. Add one before page logic grows.");
                }
            }

            report.Info($"Scene AppScreenController checked: {count} controller(s).");
        }

        private static void ValidateDirectNavigationBindings(ValidationReport report)
        {
            int directBindings = 0;

            foreach (Button button in FindSceneComponents<Button>())
            {
                int listenerCount = button.onClick.GetPersistentEventCount();
                for (int i = 0; i < listenerCount; i++)
                {
                    UnityObject target = button.onClick.GetPersistentTarget(i);
                    if (!target) continue;

                    string methodName = button.onClick.GetPersistentMethodName(i);
                    bool bypassesNavigationService = target is DockPanelController
                        || target is DockNavigationManager
                        || target is MapViewController;

                    if (!bypassesNavigationService) continue;

                    directBindings++;
                    report.Warning(
                        $"Button '{button.name}' calls {target.GetType().Name}.{methodName} directly. Route it through AppNavigationButton/AppNavigationService.");
                }
            }

            report.Info($"Direct navigation bindings found: {directBindings}.");
        }

        private static void ValidatePrototypeImageUsage(ValidationReport report)
        {
            int onboardingSpriteSteps = 0;

            foreach (OnboardingFlowManager onboarding in FindSceneComponents<OnboardingFlowManager>())
            {
                SerializedObject serializedOnboarding = new SerializedObject(onboarding);
                onboardingSpriteSteps += CountSequenceSprite(serializedOnboarding.FindProperty("introStep1"));
                onboardingSpriteSteps += CountSequenceSprite(serializedOnboarding.FindProperty("introStep2"));
                onboardingSpriteSteps += CountChapterSprites(serializedOnboarding.FindProperty("skipChapter"));
                onboardingSpriteSteps += CountChapterSprites(serializedOnboarding.FindProperty("yesChapter"));
            }

            if (onboardingSpriteSteps > 0)
            {
                report.Warning(
                    $"Onboarding still drives {onboardingSpriteSteps} step(s) through pageImage sprites. Keep this marked PrototypeImage until those pages become componentized prefabs.");
            }
        }

        private static int CountChapterSprites(SerializedProperty chapter)
        {
            if (chapter == null) return 0;

            SerializedProperty steps = chapter.FindPropertyRelative("steps");
            if (steps == null || !steps.isArray) return 0;

            int count = 0;
            for (int i = 0; i < steps.arraySize; i++)
            {
                count += CountSequenceSprite(steps.GetArrayElementAtIndex(i));
            }

            return count;
        }

        private static int CountSequenceSprite(SerializedProperty sequenceStep)
        {
            if (sequenceStep == null) return 0;

            SerializedProperty pageImage = sequenceStep.FindPropertyRelative("pageImage");
            return pageImage != null && pageImage.objectReferenceValue ? 1 : 0;
        }

        private static T FindFirstSceneComponent<T>() where T : Component
        {
            foreach (T component in FindSceneComponents<T>())
            {
                return component;
            }

            return null;
        }

        private static IEnumerable<T> FindSceneComponents<T>() where T : Component
        {
            foreach (T component in Resources.FindObjectsOfTypeAll<T>())
            {
                if (!component) continue;
                if (EditorUtility.IsPersistent(component)) continue;
                if (!component.gameObject.scene.IsValid()) continue;

                yield return component;
            }
        }

        private sealed class ValidationReport
        {
            private int _errors;
            private int _warnings;
            private readonly List<string> _messages = new List<string>();

            public void Error(string message)
            {
                _errors++;
                _messages.Add("[ERROR] " + message);
            }

            public void Warning(string message)
            {
                _warnings++;
                _messages.Add("[WARN] " + message);
            }

            public void Info(string message)
            {
                _messages.Add("[INFO] " + message);
            }

            public void Flush()
            {
                string summary = $"UI Page Architecture Validation completed: {_errors} error(s), {_warnings} warning(s).";
                string body = string.Join("\n", _messages);

                if (_errors > 0)
                {
                    UnityDebug.LogError(summary + "\n" + body);
                    return;
                }

                if (_warnings > 0)
                {
                    UnityDebug.LogWarning(summary + "\n" + body);
                    return;
                }

                UnityDebug.Log(summary + "\n" + body);
            }
        }
    }
}
