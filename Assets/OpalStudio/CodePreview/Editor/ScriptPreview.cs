using System;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Helpers;
using OpalStudio.CodePreview.Editor.Highlighters;
using OpalStudio.CodePreview.Editor.Settings;
using OpalStudio.CodePreview.Editor.View;
using UnityEditor;
using UnityEngine.UIElements;

namespace OpalStudio.CodePreview.Editor
{
      [CustomEditor(typeof(MonoScript))]
      sealed internal class ScriptPreview : UnityEditor.Editor
      {
            private FileManager _fileManager;
            private UIToolkitRenderer _uiRenderer;
            private SearchManager _searchManager;
            private PreviewSettings _settings;
            private SyntaxHighlighter _syntaxHighlighter;

            private void OnEnable()
            {
                  InitializeComponents();
                  _settings.LoadPreferences();
            }

            private void OnDisable()
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

            public override VisualElement CreateInspectorGUI()
            {
                  var script = (MonoScript)target;

                  if (!script)
                  {
                        return new Label("No script selected.");
                  }

                  _uiRenderer = new UIToolkitRenderer(_settings, _searchManager, script);

                  RefreshContent(script);

                  VisualElement root = _uiRenderer.CreateRootElement(_syntaxHighlighter.GetProcessedLines(), _fileManager.GetFileInfo());

                  root.schedule.Execute(CheckForFileChanges).Every(1000);

                  return root;
            }

            private void CheckForFileChanges()
            {
                  var script = (MonoScript)target;

                  if (script == null || !script)
                  {
                        return;
                  }

                  if (_fileManager.CheckForChanges(script))
                  {
                        RefreshContent(script, false);
                  }
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

            private void RefreshContent(MonoScript script, bool forceFullRebuild = true)
            {
                  try
                  {
                        _fileManager.LoadScript(script);
                        string[] lines = _fileManager.GetLines();

                        if (lines != null && lines.Length > _settings.MaxLinesToDisplay)
                        {
                              string[] limited = new string[_settings.MaxLinesToDisplay];
                              Array.Copy(lines, limited, _settings.MaxLinesToDisplay);
                              _fileManager.SetLimitedLines(limited);
                        }

                        ScriptType scriptType = ScriptTypeDetector.DetectType(_fileManager.GetFilePath());
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);

                        _uiRenderer?.UpdateSearchableContent(_fileManager.GetDisplayLines());
                        _searchManager.PerformSearch(_fileManager.GetDisplayLines());

                        if (!forceFullRebuild && _uiRenderer != null)
                        {
                              _uiRenderer.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
                        }
                  }
                  catch (Exception e)
                  {
                        UnityEngine.Debug.LogError($"Error refreshing script content: {e.Message}");
                        _syntaxHighlighter.SetErrorContent($"Error loading file: {e.Message}");
                        _uiRenderer?.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
                  }
            }

            private void OnSearchResultsChanged()
            {
                  _syntaxHighlighter.UpdateSearchHighlighting(_searchManager.GetSearchQuery(), _searchManager.GetSearchResults());
                  _uiRenderer?.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
            }

            private void OnSettingsChanged()
            {
                  if (_fileManager.HasContent())
                  {
                        ScriptType scriptType = ScriptTypeDetector.DetectType(_fileManager.GetFilePath());
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);
                        _uiRenderer?.UpdateCodeContent(_syntaxHighlighter.GetProcessedLines());
                  }
            }
      }
}