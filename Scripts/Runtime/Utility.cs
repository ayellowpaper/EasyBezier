using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyBezier
{
    public static class Utility
    {
        /// <summary>
        /// Returns the given component by axis.
        /// </summary>
        public static float GetComponentByAxis(Vector3 in_Vector, Axis in_Axis)
        {
            switch (in_Axis)
            {
                case Axis.X:
                    return in_Vector.x;
                case Axis.Y:
                    return in_Vector.y;
                case Axis.Z:
                    return in_Vector.z;
            }

            return in_Vector.z;
        }

        /// <summary>
        /// Switch Components by axis. This assumes Z to be default forward, thus not
        /// switching anything when axis is Z.
        /// </summary>
        public static Vector3 SwitchComponentsByAxis(Vector3 in_Vector, Axis in_Axis)
        {
            switch (in_Axis)
            {
                case Axis.X:
                    return new Vector3(-in_Vector.z, in_Vector.y, in_Vector.x);
                case Axis.Y:
                    return new Vector3(in_Vector.x, -in_Vector.z, in_Vector.y);
                case Axis.Z:
                    return in_Vector;
            }

            return in_Vector;
        }
    }
}