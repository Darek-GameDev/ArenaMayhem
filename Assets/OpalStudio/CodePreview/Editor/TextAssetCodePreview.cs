using System;
using System.IO;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Helpers;
using OpalStudio.CodePreview.Editor.Highlighters;
using OpalStudio.CodePreview.Editor.Settings;
using OpalStudio.CodePreview.Editor.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using FileInfo = OpalStudio.CodePreview.Editor.Data.FileInfo;

namespace OpalStudio.CodePreview.Editor
{
      [CustomEditor(typeof(TextAsset), true)]
      sealed internal class TextAssetCodePreview : UnityEditor.Editor
      {
            private UnityEditor.Editor _defaultEditor;
            private VisualElement _rootElement;

            private FileManager _fileManager;
            private UIToolkitRenderer _uiRenderer;
            private SearchManager _searchManager;
            private PreviewSettings _settings;
            private SyntaxHighlighter _syntaxHighlighter;

            private TextAsset _lastAsset;

            private void OnEnable()
            {
                  var textAsset = (TextAsset)target;

                  if (!IsFileHandled(textAsset))
                  {
                        _defaultEditor = CreateEditor(target, Type.GetType("UnityEditor.TextAssetInspector, UnityEditor"));
                  }
                  else
                  {
                        InitializeComponents();
                        _settings.LoadPreferences();
                  }
            }

            private void OnDisable()
            {
                  if (_defaultEditor != null)
                  {
                        DestroyImmediate(_defaultEditor);
                  }
                  else
                  {
                        _settings?.SavePreferences();

                        if (_searchManager != null)
                        {
                              _searchManager.OnSearchResultsChanged -= OnSearchResultsChanged;
                        }

                        if (_settings != null)
                        {
                              _settings.OnSettingsChanged -= OnSettingsChanged;
                        }
                  }
            }

            public override VisualElement CreateInspectorGUI()
            {
                  var textAsset = (TextAsset)target;

                  if (!IsFileHandled(textAsset))
                  {
                        if (_defaultEditor != null)
                        {
                              var root = new VisualElement();
                              root.Add(new IMGUIContainer(() => _defaultEditor.OnInspectorGUI()));

                              return root;
                        }

                        return new Label("This text asset type is not supported by CodePreview.");
                  }

                  _lastAsset = textAsset;
                  _uiRenderer = new UIToolkitRenderer(_settings, _searchManager, textAsset);

                  RefreshContent(textAsset);

                  _rootElement = _uiRenderer.CreateRootElement(_syntaxHighlighter.GetProcessedLines(), _fileManager.GetFileInfo());

                  _rootElement.schedule.Execute(CheckForFileChanges).Every(1000);

                  return _rootElement;
            }

            private void CheckForFileChanges()
            {
                  var textAsset = (TextAsset)target;

                  if (textAsset == null || !textAsset)
                  {
                        return;
                  }

                  if (HasFileChanged(AssetDatabase.GetAssetPath(textAsset)))
                  {
                        RefreshContent(textAsset);
                  }
            }

            private static bool IsFileHandled(TextAsset textAsset)
            {
                  string filePath = AssetDatabase.GetAssetPath(textAsset);
                  ScriptType type = ScriptTypeDetector.DetectType(filePath);

                  return type != ScriptType.Unknown;
            }

            private void InitializeComponents()
            {
                  _settings = new PreviewSettings();
                  _fileManager = new FileManager();
                  _searchManager = new SearchManager();
                  _syntaxHighlighter = new SyntaxHighlighter();

                  _searchManager.OnSearchResultsChanged += OnSearchResultsChanged;
                  _settings.OnSettingsChanged += OnSettingsChanged;
            }

            private void RefreshContent(TextAsset textAsset)
            {
                  try
                  {
                        string filePath = AssetDatabase.GetAssetPath(textAsset);
                        string[] lines = textAsset.text.Split('\n');

                        if (lines.Length > _settings.MaxLinesToDisplay)
                        {
                              string[] limited = new string[_settings.MaxLinesToDisplay];
                              Array.Copy(lines, limited, _settings.MaxLinesToDisplay);
                              _fileManager.LoadFromContent(limited, filePath);
                              _fileManager.SetLimitedLines(limited);
                        }
                        else
                        {
                              _fileManager.LoadFromContent(lines, filePath);
                        }

                        ScriptType scriptType = ScriptTypeDetector.DetectType(filePath);
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);

                        _uiRenderer?.UpdateSearchableContent(_fileManager.GetDisplayLines());
                        _searchManager.PerformSearch(_fileManager.GetDisplayLines());
                        _uiRenderer?.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Error refreshing content: {e.Message}");
                        _syntaxHighlighter.SetErrorContent($"Error loading file: {e.Message}");
                        _uiRenderer?.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
                  }
            }

            private bool HasFileChanged(string filePath)
            {
                  if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || _fileManager == null)
                  {
                        return false;
                  }

                  FileInfo fileInfo = _fileManager.GetFileInfo();

                  if (fileInfo == null)
                  {
                        return true;
                  }

                  return File.GetLastWriteTime(filePath) != fileInfo.LastModifiedTime;
            }

            private void OnSearchResultsChanged()
            {
                  _syntaxHighlighter.UpdateSearchHighlighting(_searchManager.GetSearchQuery(), _searchManager.GetSearchResults());
                  _uiRenderer?.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
            }

            private void OnSettingsChanged()
            {
                  if (_fileManager != null && _fileManager.HasContent())
                  {
                        string filePath = AssetDatabase.GetAssetPath(_lastAsset);
                        ScriptType scriptType = ScriptTypeDetector.DetectType(filePath);
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);
                        _uiRenderer?.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
                  }
            }
      }
}