using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reallusion.Import
{
    public class RLFollowCam : MonoBehaviour
    {
        public Transform target;

        // Start is called before the first frame update
        void Start()
        {
            if (target) enabled = true;
            else enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            Quaternion fromRotation = transform.rotation;
            transform.LookAt(target, Vector3.up);
            Quaternion toRotation = transform.rotation;
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, 0.065f);
        }
    }
}
