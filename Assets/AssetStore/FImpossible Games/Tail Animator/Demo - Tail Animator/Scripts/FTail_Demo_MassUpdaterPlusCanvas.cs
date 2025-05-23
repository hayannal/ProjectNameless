﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FIMSpace.FTail
{
    public class FTail_Demo_MassUpdaterPlusCanvas : FTail_Animator_MassUpdater
    {
        public Text info;
        private int all;
        private int allBones;

        protected override IEnumerator Start()
        {
            yield return base.Start();
            StartCoroutine(DelayedSummary());
        }

        IEnumerator DelayedSummary()
        {
            yield return new WaitForSeconds(0.25f);

            FTail_Animator[] ts = FindObjectsOfType<FTail_Animator>();
            all = ts.Length;

            for (int i = 0; i < all; i++)
            {
                allBones += ts[i].TailTransforms.Count;
            }
        }

        protected override void Update()
        {
            info.text = "Tail Animators on scene = " + all+"\n"+"Bones animated by Tail Animators count = " + allBones;
            base.Update();
        }
    }
}