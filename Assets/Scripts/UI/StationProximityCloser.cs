using System;
using UnityEngine;
using VoidBound.Core;

namespace VoidBound.UI
{
    // Closes the open station panel when the player walks away from the
    // station that opened it. Single slot — only one station panel is ever
    // open at a time (CloseOtherHomesteadPanels). Station UIs call Track()
    // in Open() and Untrack() in Close(); no scene wiring needed.
    public class StationProximityCloser : MonoBehaviour
    {
        private const float ExtraRange = 2f; // walk-away slack beyond interact range

        private MonoBehaviour owner;
        private Transform station;
        private float closeDistance;
        private Action close;
        private Transform player;

        public static void Track(GameObject host, MonoBehaviour panelOwner, Interactable stationSource, Action closeAction)
        {
            var c = host.GetComponent<StationProximityCloser>();
            if (c == null) c = host.AddComponent<StationProximityCloser>();
            c.owner = panelOwner;
            c.station = stationSource.transform;
            c.closeDistance = stationSource.InteractRange + ExtraRange;
            c.close = closeAction;
        }

        public static void Untrack(GameObject host, MonoBehaviour panelOwner)
        {
            var c = host.GetComponent<StationProximityCloser>();
            if (c == null || c.owner != panelOwner) return;
            c.owner = null;
            c.station = null;
            c.close = null;
        }

        private void Update()
        {
            if (station == null || close == null) return;

            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go == null) return;
                player = go.transform;
            }

            if (Vector3.Distance(player.position, station.position) > closeDistance)
            {
                var action = close;
                owner = null;
                station = null;
                close = null;
                action();
            }
        }
    }
}
