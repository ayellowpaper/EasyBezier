using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using System;
using EasyBezier;

namespace EasyBezier.UIElements
{
    public class FoldoutContainer : VisualElement
    {
        public Toolbar Toolbar { get; private set; }
        public Foldout Foldout { get; private set; }
        public VisualElement Content { get; private set; }

        public FoldoutContainer(string in_Name, params VisualElement[] in_ContentElements) : this(in_Name, in_Name, in_ContentElements) { }

        public FoldoutContainer(string in_Name, string in_FoldoutLookupID, params VisualElement[] in_ContentElements) : base()
        {
            this.AddToClassList("eb-container");

            Toolbar = new Toolbar();
            Foldout = new Foldout();
            Foldout.text = in_Name;
            Toolbar.Add(Foldout);
            Toolbar.AddToClassList("eb-container__header");

            Content = new VisualElement();
            Content.AddToClassList("eb-container__content");

            if (!string.IsNullOrEmpty(in_FoldoutLookupID))
            {
                Foldout.value = BezierEditorUtility.Booleans[in_FoldoutLookupID];
                ShowContent(Foldout.value);
            }

            Foldout.RegisterValueChangedCallback(evt => { if (!string.IsNullOrEmpty(in_FoldoutLookupID)) BezierEditorUtility.Booleans[in_FoldoutLookupID] = evt.newValue; ShowContent(evt.newValue); });

            Add(Toolbar);
            Add(Content);

            foreach (var element in in_ContentElements)
                Content.Add(element);
        }

        private void ShowContent(bool in_Show)
        {
            Content.style.display = UIElementsExtensions.FromBool(in_Show);
        }
    }
}