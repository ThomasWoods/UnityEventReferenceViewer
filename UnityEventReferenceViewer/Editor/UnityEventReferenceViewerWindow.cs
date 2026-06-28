using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;

namespace UnityEventReferenceViewer
{
    public class UnityEventReferenceViewerWindow : EditorWindow
    {
        private static EventReferenceInfoDictionary allDependencies;
        private static EventReferenceInfoDictionary filteredDependencies;
        private string gameObjectSearch = "";
        private string methodSearch = "";

        private Rect drawableRect;

        Vector2 scroll = Vector2.zero;
        int perPage = 100;
        int page = 0;


        [MenuItem("Window/UnityEvent Reference Viewer")]
        public static void OpenWindow()
        {
            UnityEventReferenceViewerWindow window = GetWindow<UnityEventReferenceViewerWindow>();
            window.titleContent = new GUIContent("UnityEvent Reference Viewer");
            window.minSize = new Vector2(250, 100);
        }

        private void OnDisable()
        {
            allDependencies = null;
            filteredDependencies = null;
        }

        private void OnGUI()
        {
            DrawWindow();
        }

        private void DrawWindow()
        {
            drawableRect = GetDrawableRect();
            var quarterLength = GUILayout.Width(drawableRect.width / 4);

            using (new GUILayout.HorizontalScope())
            {
                gameObjectSearch = EditorGUILayout.TextField("GameObject Filter: ", gameObjectSearch, GUILayout.Width(drawableRect.width/3));
                methodSearch = EditorGUILayout.TextField("Method Filter: ", methodSearch, GUILayout.Width(drawableRect.width/3));
                var maxPage = filteredDependencies == null ? 0 : filteredDependencies.dict.Count / perPage;
                EditorGUILayout.LabelField($"Page {page + 1} / {maxPage+1}");
                if (GUILayout.Button("<<") && page > 0) page--;
                if (GUILayout.Button(">>") && page < maxPage) page++;
                GUILayout.FlexibleSpace();
            }
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                {
                    FindDependencies(gameObjectSearch, methodSearch, true);
                    page = 0;
                }
                if (GUILayout.Button("Filter"))
                {
                    FindDependencies(gameObjectSearch, methodSearch, false);
                    page = 0;
                }
            }
            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("__PREFAB__", quarterLength);
                GUILayout.Label("__COMPONENT__", quarterLength);
                GUILayout.Label("__LISTENER__", quarterLength);
                GUILayout.Label("__METHOD__", quarterLength);
            }

            if (filteredDependencies == null) return;

            int count = 0;
            using (var scrollview = new GUILayout.ScrollViewScope(scroll, false, false))
            {
                scroll = scrollview.scrollPosition;
                foreach (var objPair in filteredDependencies.dict)
                {
                    count++;
                    if (count < page * perPage) continue;
                    if (count > (page + 1) * perPage) continue;
                    using (new GUILayout.HorizontalScope()) //Prefab Row
                    {
                        var parent = objPair.Key;
                        var behaviorList = objPair.Value;
                        EditorGUILayout.ObjectField(parent, typeof(GameObject), true, quarterLength);
                        using (new GUILayout.VerticalScope()) //Behavior List
                        {
                            foreach (var behaviorPair in behaviorList)
                            {
                                var behavior = behaviorPair.Key;
                                var infoList = behaviorPair.Value;
                                using (new GUILayout.HorizontalScope()) //Behavior Row
                                {
                                    EditorGUILayout.ObjectField(behavior, typeof(MonoBehaviour), true, quarterLength);

                                    using (new GUILayout.VerticalScope()) //Event Info List
                                    {
                                        foreach (var info in infoList)
                                        {
                                            using (new GUILayout.HorizontalScope()) //Event Info Row
                                            {
                                                EditorGUILayout.ObjectField(info.listener, typeof(MonoBehaviour), true, quarterLength);
                                                EditorGUILayout.LabelField(info.methodName, quarterLength);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    GUILayout.Space(5);
                }
            }
        }

        private static void FindDependencies(string gameObjectFilter, string methodNameFilter, bool refresh)
        {
            if(refresh || allDependencies == null) allDependencies = UnityEventReferenceFinder.FindAllUnityEventsReferences();
            filteredDependencies = null;
            gameObjectFilter = gameObjectFilter.ToLower();
            methodNameFilter = methodNameFilter.ToLower();
            bool matchObj = !string.IsNullOrEmpty(gameObjectFilter);
            bool matchFunc = !string.IsNullOrEmpty(methodNameFilter);
            if (!matchObj && !matchFunc)
            {
                filteredDependencies = allDependencies;
                return;
            }
            filteredDependencies = new EventReferenceInfoDictionary();
            foreach (var a in allDependencies.dict)
            {
                var prefab = a.Key;
                if (matchObj && !a.Key.name.ToLower().Contains(gameObjectFilter)) continue;
                foreach(var b in a.Value) 
                {
                    var behavior = b.Key;
                    var infos = b.Value;
                    foreach (var info in infos)
                    {
                        if (matchFunc && !info.methodName.ToLower().Contains(methodNameFilter)) continue;
                        if (!filteredDependencies.dict.ContainsKey(prefab)) filteredDependencies.dict.Add(prefab, new Dictionary<MonoBehaviour, List<EventReferenceInfo>>());
                        if (!filteredDependencies.dict[prefab].ContainsKey(behavior)) filteredDependencies.dict[prefab].Add(behavior, new List<EventReferenceInfo>());
                        filteredDependencies.dict[prefab][behavior].Add(info);
                    }
                }
            }
        }
        
        private Rect GetDrawableRect()
        {
            return new Rect(Vector2.one * 30f, position.size - Vector2.one * 60f);
        }
    }
}
