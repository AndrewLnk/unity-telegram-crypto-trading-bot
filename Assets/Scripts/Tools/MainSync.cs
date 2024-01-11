using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public class MainSync : MonoBehaviour
    {
        public static MainSync Instance;

        private readonly List<Sync> syncs = new List<Sync>();

        public void Awake()
        {
            Instance = this;
        }

        public void Add(Sync sync)
        {
            if (syncs.Contains(sync))
                return;
            
            syncs.Add(sync);
        }

        private void Update()
        {
            foreach (var sync in syncs)
            {
                sync.Update();
            }
        }
    }
}
