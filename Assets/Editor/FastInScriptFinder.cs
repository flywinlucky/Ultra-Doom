// This file MUST be in a folder named "Editor".
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using Object = UnityEngine.Object;

namespace FastInScriptFinder
{
    public class FastInScriptFinder : EditorWindow
    {
        #region Variables

        // Search Parameters
        private string searchText = "";
        private Dictionary<string, bool> fileTypeToggles;
        private SearchMode searchMode = SearchMode.SimpleText;
        private bool caseSensitive = false;
        private bool wholeWord = false;

        // Scope Control
        private List<Object> includeFolders = new List<Object>();
        private List<Object> excludeFolders = new List<Object>();
        private bool searchInPackages = false;

        // Results Data
        private List<SearchResult> searchResults = new List<SearchResult>();
        private Dictionary<string, bool> resultsFoldoutState = new Dictionary<string, bool>();
        private Vector2 resultsScrollPosition;
        private bool searchCompleted = false;

        // UI Styles & State
        private GUIStyle richTextLabelStyle;
        private GUIStyle headerBoxStyle;
        private GUIStyle titleStyle; // Style for the main title/logo
        private bool showSettings = true;
        private bool showScope = true;

        private enum SearchMode { SimpleText, Regex }

        #endregion

        [MenuItem("Tools/Fast InScript Finder")]
        public static void ShowWindow()
        {
            GetWindow<FastInScriptFinder>("Fast InScript Finder");
        }

        private void OnEnable()
        {
            InitializeStyles();
            InitializeFileTypeToggles();
        }

        private void InitializeFileTypeToggles()
        {
            // Only initialize if it's null, to preserve user's choices during editor reloads
            if (fileTypeToggles == null)
            {
                fileTypeToggles = new Dictionary<string, bool>
                {
                    { ".cs", true }, { ".shader", true }, { ".txt", true }, { ".json", true },
                    { ".xml", true }, { ".html", true }
                };
            }
        }

        private void InitializeStyles()
        {
            // Initialize styles that require access to GUI skins
            if (richTextLabelStyle == null)
            {
                richTextLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    alignment = TextAnchor.MiddleLeft
                };
            }
            if (headerBoxStyle == null)
            {
                headerBoxStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(5, 5, 2, 2)
                };
            }
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 5, 5)
                };
            }
        }


        #region OnGUI

        private void OnGUI()
        {
            // FIX: Ensure styles are initialized, preventing NullReferenceException on script reloads.
            InitializeStyles();

            DrawSearchPanel();
            DrawResultsPanel();
            DrawFooterPanel();
        }

        private void DrawSearchPanel()
        {
            GUILayout.Label("Fast InScript Finder v1.0", titleStyle);
            // EditorGUILayout.Space(); // Removed for tighter spacing

            // --- Search Input ---
            GUILayout.Label("Search Parameters", EditorStyles.largeLabel);
            searchText = EditorGUILayout.TextField(new GUIContent("Search For:", "The text or pattern to search for within the files."), searchText);

            showSettings = EditorGUILayout.Foldout(showSettings, "Search Settings", true);
            if (showSettings)
            {
                EditorGUI.indentLevel++;
                searchMode = (SearchMode)EditorGUILayout.EnumPopup(new GUIContent("Search Mode:", "Simple Text: Searches for the exact text.\nRegex: Allows using powerful Regular Expression patterns for complex searches."), searchMode);
                if (searchMode == SearchMode.Regex)
                {
                    EditorGUILayout.HelpBox("Using Regular Expressions. 'Whole Word' is controlled by the regex pattern (e.g., using \\b).", MessageType.Info);
                }
                else
                {
                    wholeWord = EditorGUILayout.Toggle(new GUIContent("Whole Word", "If enabled, the search will only match complete words. For example, searching for 'Color' will not find 'BackgroundColor'."), wholeWord);
                }
                caseSensitive = EditorGUILayout.Toggle(new GUIContent("Case Sensitive", "If enabled, the search will differentiate between uppercase and lowercase letters. For example, 'Player' will not match 'player'."), caseSensitive);

                DrawFileTypeToggles();

                EditorGUI.indentLevel--;
            }

            // --- Scope Control ---
            showScope = EditorGUILayout.Foldout(showScope, "Search Scope", true);
            if (showScope)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Drag and drop folders into the lists below. If 'Include Folders' is empty, the entire 'Assets' folder will be searched.", MessageType.Info);
                DrawFolderList("Include Folders:", includeFolders, "Drag folders here to limit the search ONLY to these folders and their subfolders.");
                DrawFolderList("Exclude Folders:", excludeFolders, "Drag folders here to exclude them from the search. This is useful for ignoring specific libraries or generated code.");
                searchInPackages = EditorGUILayout.Toggle(new GUIContent("Search in Packages", "If enabled, the search will also include files from the project's 'Packages' folder. This can significantly increase search time."), searchInPackages);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // --- Action Button ---
            if (GUILayout.Button(new GUIContent("🔍 Search", "Begin the search with the current parameters."), GUILayout.Height(40)))
            {
                if (!string.IsNullOrEmpty(searchText))
                {
                    ExecuteSearch();
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning", "Please enter a search term.", "OK");
                }
            }
            EditorGUILayout.Space();
        }

        private void DrawFileTypeToggles()
        {
            EditorGUILayout.LabelField(new GUIContent("File Extensions to Include", "Select the types of text-based files you want to include in your search."), EditorStyles.boldLabel);

            int itemsPerRow = 4;
            var keys = new List<string>(fileTypeToggles.Keys);

            for (int i = 0; i < keys.Count; i += itemsPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = 0; j < itemsPerRow; j++)
                {
                    int index = i + j;
                    if (index < keys.Count)
                    {
                        string key = keys[index];
                        fileTypeToggles[key] = EditorGUILayout.ToggleLeft(key, fileTypeToggles[key], GUILayout.Width(100));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFolderList(string label, List<Object> folderList, string tooltip)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), EditorStyles.boldLabel);

            for (int i = 0; i < folderList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                folderList[i] = EditorGUILayout.ObjectField(folderList[i], typeof(DefaultAsset), false);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    folderList.RemoveAt(i);
                    i--; // Adjust index after removal
                }
                EditorGUILayout.EndHorizontal();
            }

            Object newFolder = EditorGUILayout.ObjectField(new GUIContent("Add Folder:", "Drag a folder here to add it to the list."), null, typeof(DefaultAsset), false);
            if (newFolder != null)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(newFolder)))
                {
                    if (!folderList.Contains(newFolder))
                    {
                        folderList.Add(newFolder);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a valid folder.", "OK");
                }
            }
        }

        private void DrawResultsPanel()
        {
            EditorGUILayout.BeginHorizontal();
            string resultsHeader = "Search Results";
            if (searchCompleted && searchResults.Count > 0)
            {
                int scriptCount = searchResults.Count;
                int totalMatches = searchResults.Sum(r => r.matches.Count);
                resultsHeader = $"Search Results (Found matches in {scriptCount} scripts, {totalMatches} matches in total)";
            }
            GUILayout.Label(resultsHeader, EditorStyles.boldLabel);


            GUILayout.FlexibleSpace();

            bool canReset = !string.IsNullOrEmpty(searchText) || searchResults.Count > 0 || searchCompleted;
            GUI.enabled = canReset;
            if (GUILayout.Button(new GUIContent("Clear Results", "Clears the search text and all results from the window, but keeps your settings."), GUILayout.Width(100)))
            {
                ResetSearchState();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();


            resultsScrollPosition = EditorGUILayout.BeginScrollView(resultsScrollPosition, EditorStyles.helpBox);

            if (!searchCompleted)
            {
                GUILayout.Label("Ready to search...");
            }
            else if (searchResults.Count == 0)
            {
                GUILayout.Label($"No results found for '{searchText}'.");
            }
            else
            {
                foreach (var result in searchResults)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox); // Box for each file result

                    if (!resultsFoldoutState.ContainsKey(result.filePath))
                    {
                        resultsFoldoutState[result.filePath] = true;
                    }

                    // Custom Header Button
                    string headerText = $" {Path.GetFileName(result.filePath)} ({result.matches.Count} matches)";
                    if (GUILayout.Button(new GUIContent(headerText, "Left-click to expand/collapse. Right-click to ping the asset."), headerBoxStyle))
                    {
                        if (Event.current.button == 1) // Right-click to ping
                        {
                            EditorGUIUtility.PingObject(result.asset);
                        }
                        else // Left-click to toggle foldout
                        {
                            resultsFoldoutState[result.filePath] = !resultsFoldoutState[result.filePath];
                        }
                    }

                    bool state = resultsFoldoutState[result.filePath];

                    if (state)
                    {
                        // Display matches
                        foreach (var match in result.matches)
                        {
                            if (GUILayout.Button(new GUIContent($"{match.lineNumber}: {match.lineContent.Trim()}", "Click to open the file at this line."), richTextLabelStyle))
                            {
                                AssetDatabase.OpenAsset(result.asset, match.lineNumber);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawFooterPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Pushes content to the right
            GUILayout.Label("♥ Pls Leave A Review ♥", EditorStyles.miniLabel);
            if (GUILayout.Button(new GUIContent("⋆⭒ Visit Our Asset Store ⭒⋆", "Check out other assets from this publisher.")))
            {
                Application.OpenURL("https://assetstore.unity.com/publishers/56140");
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Search Logic

        private void ExecuteSearch()
        {
            searchResults.Clear();
            searchCompleted = false;
            GUI.FocusControl(null);

            string[] searchPaths = GetSearchPaths();
            List<string> excludedPaths = excludeFolders.Select(folder => AssetDatabase.GetAssetPath(folder)).ToList();

            string[] allAssetGUIDs = AssetDatabase.FindAssets("", searchPaths);
            float totalAssets = allAssetGUIDs.Length;

            try
            {
                for (int i = 0; i < totalAssets; i++)
                {
                    string guid = allAssetGUIDs[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    EditorUtility.DisplayProgressBar("Searching...", $"Checking: {assetPath}", (i + 1) / totalAssets);

                    if (IsPathExcluded(assetPath, excludedPaths) || !IsFileTypeMatch(assetPath))
                    {
                        continue;
                    }

                    string fileContent;
                    try
                    {
                        fileContent = File.ReadAllText(assetPath);
                    }
                    catch (Exception)
                    {
                        continue; // Skip files we can't read
                    }

                    var matches = FindMatchesInContent(fileContent);
                    if (matches.Count > 0)
                    {
                        searchResults.Add(new SearchResult
                        {
                            asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath),
                            filePath = assetPath,
                            matches = matches
                        });
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            searchCompleted = true;
            Debug.Log($"Search finished. Found {searchResults.Sum(r => r.matches.Count)} matches in {searchResults.Count} files.");
        }

        private List<MatchInfo> FindMatchesInContent(string content)
        {
            var foundMatches = new List<MatchInfo>();
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (DoesLineMatch(line))
                {
                    foundMatches.Add(new MatchInfo
                    {
                        lineNumber = i + 1,
                        lineContent = HighlightMatch(line)
                    });
                }
            }
            return foundMatches;
        }

        #endregion

        #region Helpers & Data Structures

        private void ResetSearchState()
        {
            searchText = "";
            searchResults.Clear();
            resultsFoldoutState.Clear();
            searchCompleted = false;
            GUI.FocusControl(null);
            Repaint();
        }

        private string[] GetSearchPaths()
        {
            string[] searchPaths;
            if (includeFolders.Count > 0)
            {
                searchPaths = includeFolders.Where(f => f != null).Select(folder => AssetDatabase.GetAssetPath(folder)).ToArray();
            }
            else
            {
                searchPaths = new string[] { "Assets" };
            }

            if (searchInPackages)
            {
                searchPaths = searchPaths.Concat(new string[] { "Packages" }).ToArray();
            }
            return searchPaths;
        }

        private bool DoesLineMatch(string line)
        {
            if (string.IsNullOrEmpty(searchText)) return false;

            if (searchMode == SearchMode.Regex)
            {
                RegexOptions options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                return Regex.IsMatch(line, searchText, options);
            }
            else // Simple Text
            {
                if (wholeWord)
                {
                    string pattern = $@"\b{Regex.Escape(searchText)}\b";
                    return Regex.IsMatch(line, pattern, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
                else
                {
                    StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    return line.IndexOf(searchText, comparison) >= 0;
                }
            }
        }

        private string HighlightMatch(string line)
        {
            string color = EditorGUIUtility.isProSkin ? "#f0c058" : "#0033cc";
            string highlightedText = $"<color={color}><b>$0</b></color>";

            if (string.IsNullOrEmpty(searchText)) return line;

            try
            {
                if (searchMode == SearchMode.Regex)
                {
                    RegexOptions options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    return Regex.Replace(line, searchText, highlightedText, options);
                }
                else
                {
                    string pattern = wholeWord ? $@"\b{Regex.Escape(searchText)}\b" : Regex.Escape(searchText);
                    return Regex.Replace(line, pattern, highlightedText, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
            }
            catch (Exception)
            {
                // Invalid regex might throw an exception, return original line
                return line;
            }
        }

        private bool IsPathExcluded(string path, List<string> excludedPaths)
        {
            return excludedPaths.Any(excludedPath => !string.IsNullOrEmpty(excludedPath) && path.StartsWith(excludedPath + "/"));
        }

        private bool IsFileTypeMatch(string path)
        {
            string extension = Path.GetExtension(path)?.ToLower();
            if (string.IsNullOrEmpty(extension)) return false;

            if (fileTypeToggles.TryGetValue(extension, out bool isEnabled))
            {
                return isEnabled;
            }
            return false;
        }

        private class SearchResult
        {
            public Object asset;
            public string filePath;
            public List<MatchInfo> matches;
        }

        private class MatchInfo
        {
            public int lineNumber;
            public string lineContent;
        }

        #endregion
    }
}

