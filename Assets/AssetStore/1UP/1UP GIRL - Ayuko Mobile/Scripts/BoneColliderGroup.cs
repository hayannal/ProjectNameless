﻿using System;
using UnityEngine;

    #if UNITY_5_5_OR_NEWER
    [DefaultExecutionOrder(11001)]
    #endif
    public class BoneColliderGroup : MonoBehaviour
    {
        [Serializable]
        public class SphereCollider
        {
            public Vector3 Offset;

            [Range(0, 1.0f)]
            public float Radius;
        }

        [SerializeField]
        public SphereCollider[] Colliders = new SphereCollider[]{
            new SphereCollider
            {
                Radius=0.1f
            }
        };

        [SerializeField]
        Color m_gizmoColor = Color.magenta;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = m_gizmoColor;
            Matrix4x4 mat = transform.localToWorldMatrix;
            Gizmos.matrix = mat * Matrix4x4.Scale(new Vector3(
                1.0f / transform.lossyScale.x,
                1.0f / transform.lossyScale.y,
                1.0f / transform.lossyScale.z
                ));
            foreach (var y in Colliders)
            {
                Gizmos.DrawWireSphere(y.Offset, y.Radius);
            }
        }
    }
