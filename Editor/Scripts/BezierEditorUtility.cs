using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace EasyBezier
{
    public static class BezierEditorUtility
    {
        public static UniqueList<bool> Booleans = new UniqueList<bool>(() => { return false; });

        public class UniqueList<T>
        {
            private Dictionary<string, T> _uniqueBooleans = new Dictionary<string, T>();
            private System.Func<T> _objectGenerator;

            public UniqueList(System.Func<T> objectGenerator)
            {
                _objectGenerator = objectGenerator;
            }

            public T this[string path] {
                get {
                    if (!_uniqueBooleans.ContainsKey(path))
                        _uniqueBooleans.Add(path, _objectGenerator());

                    return _uniqueBooleans[path];
                }
                set {
                    if (!_uniqueBooleans.ContainsKey(path))
                        _uniqueBooleans.Add(path, value);
                    else
                        _uniqueBooleans[path] = value;
                }
            }
        }

        public static Vector3 RoundVector3(Vector3 in_Vector, int in_Digits)
        {
            return new Vector3(
                (float)System.Math.Round(in_Vector.x, in_Digits, System.MidpointRounding.AwayFromZero),
                (float)System.Math.Round(in_Vector.y, in_Digits, System.MidpointRounding.AwayFromZero),
                (float)System.Math.Round(in_Vector.z, in_Digits, System.MidpointRounding.AwayFromZero)
           );
        }

        private static List<IEnumerator> _coroutines = new List<IEnumerator>();

        public static void StartEditorCoroutine(IEnumerator coroutine)
        {
            if (_coroutines.Count == 0)
                EditorApplication.update += CoroutineUpdate;

            _coroutines.Add(coroutine);
        }

        private static void CoroutineUpdate()
        {
            for (int i = _coroutines.Count - 1; i >= 0; i--)
            {
                if (!_coroutines[i].MoveNext())
                    _coroutines.RemoveAt(i);
            }

            if (_coroutines.Count == 0)
                EditorApplication.update -= CoroutineUpdate;
        }

        public static T GetObject<T>(this SerializedProperty in_Property)
        {
            object obj = in_Property.serializedObject.targetObject;
            string[] paths = in_Property.propertyPath.Split('.');
            System.Reflection.FieldInfo fi = null;
            foreach (string path in paths)
            {
                fi = obj.GetType().GetField(path, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                obj = fi.GetValue(obj);
            }
            return (T)obj;
        }

        //public static void RecordUndo(Object in_Target, string in_Name)
        //{
        //    Undo.RecordObject(in_Target, in_Name);
        //    PrefabUtility.RecordPrefabInstancePropertyModifications(in_Target);
        //}
    }
}