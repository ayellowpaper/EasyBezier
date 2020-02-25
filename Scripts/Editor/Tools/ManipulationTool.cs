using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyBezier
{
    public abstract class ManipulationTool
    {
        public virtual bool NeedsSelection { get => true; }

        public abstract void DoPoint(BezierPathComponentEditor in_Editor, int in_Index);
        public abstract void DoInTangent(BezierPathComponentEditor in_Editor, int in_Index);
        public abstract void DoOutTangent(BezierPathComponentEditor in_Editor, int in_Index);
    }
}