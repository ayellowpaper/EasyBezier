using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EasyBezier;

namespace Tests
{
    public class PerformanceTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void QuaternionSpeed()
        {
            int capacity = 100000;
            Quaternion[] quats = new Quaternion[capacity];
            Vector3[] upVectors = new Vector3[capacity];
            float[] interps = new float[capacity];
            for (int i = 0; i < capacity; i++)
            {
                quats[i] = Quaternion.Euler(Random.Range(0, 360f), Random.Range(0, 360f), Random.Range(0f, 360f));
                upVectors[i] = new Vector3(Random.value, Random.value, Random.value).normalized;
                interps[i] = Random.value;
            }

            Vector3[] newUpVectors = new Vector3[capacity];

            Stopwatch sw = new Stopwatch();

            sw.Start();
            for (int i = 0; i < capacity - 1; i++)
            {
                newUpVectors[i] = Quaternion.Lerp(quats[i], quats[i + 1], interps[i]) * upVectors[i];
            }
            sw.Stop();
            UnityEngine.Debug.Log($"Quaternion Lerp: {sw.ElapsedTicks}");

            sw.Restart();
            for (int i = 0; i < capacity - 1; i++)
            {
                newUpVectors[i] = Vector3.Lerp(upVectors[i], upVectors[i + 1], interps[i]);
            }
            sw.Stop();
            UnityEngine.Debug.Log($"Vector Lerp: {sw.ElapsedTicks}");
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator PerformanceTestWithEnumeratorPasses()
        {
            BezierPathComponent component = new GameObject().AddComponent<BezierPathComponent>();
            component.AddPoint(Vector3.zero);
            component.AddPoint(Vector3.one);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            int steps = 10000;
            for (int i = 0; i < steps; i++)
            {
                float t = (float) i / steps;
                component.GetPositionAtTime(t);
            }
            sw.Stop();

            yield return null;

            string nm = $"{steps} Steps took:\n{sw.ElapsedMilliseconds} Miliseconds\n{sw.ElapsedTicks} Ticks";
            UnityEditor.EditorUtility.DisplayDialog("Results", nm, "Ok");
            UnityEngine.Debug.Log(nm);
        }
    }
}
