using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    [CreateAssetMenu(
        fileName = "AppNavigationRouteTable",
        menuName = "Bob Shared Mobility/Navigation/Route Table")]
    public sealed class AppNavigationRouteTable : ScriptableObject
    {
        public enum RouteKind
        {
            Shell,
            DockScreen,
            Screen,
            Modal,
            Overlay
        }

        public enum PresentationMode
        {
            Componentized,
            PrototypeImage,
            Hybrid
        }

        public enum ProductionStatus
        {
            Prototype,
            InProduction,
            ProductionReady
        }

        [System.Serializable]
        public sealed class Route
        {
            public AppScreenId screenId = AppScreenId.None;
            public string routeName;
            public RouteKind routeKind = RouteKind.Screen;
            public AppNavigationLayer navigationLayer = AppNavigationLayer.DockPanel;
            public GameObject screenPrefab;
            public PresentationMode presentationMode = PresentationMode.Componentized;
            public ProductionStatus productionStatus = ProductionStatus.InProduction;
            [TextArea(2, 5)] public string notes;
        }

        [SerializeField] private List<Route> routes = new List<Route>();

        public IReadOnlyList<Route> Routes => routes;

        public bool TryGetRoute(AppScreenId screenId, out Route route)
        {
            foreach (Route candidate in routes)
            {
                if (candidate != null && candidate.screenId == screenId)
                {
                    route = candidate;
                    return true;
                }
            }

            route = null;
            return false;
        }

        public bool Contains(AppScreenId screenId)
        {
            return TryGetRoute(screenId, out _);
        }
    }
}
