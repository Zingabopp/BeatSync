using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSync
{
    public class BeatSync : MonoBehaviour
    {
        public static BeatSync Instance { get; set; }

        public void Awake()
        {
            if (Instance != null)
                GameObject.DestroyImmediate(this);
            Instance = this;
        }
        public void Start() { }


    }
}
