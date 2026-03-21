using System;
using System.Collections.Generic;
using System.Linq;

namespace OpalStudio.CodePreview.Editor.Helpers
{
      sealed internal class SearchManager
      {
            // Events
            internal event Action OnSearchResultsChanged;

            // Search state
            private string _searchQuery = "";
            private string _lastProcessedSearchQuery = "";
            private bool _caseSensitiveSearch;
            private readonly HashSet<int> _searchResults = new();
            private int _currentSearchIndex = -1;

            // Navigation
            private int _goToLine = 1;

            // Properties
            internal string SearchQuery
            {
                  get => _searchQuery;
                  set
                  {
                        if (_searchQuery != null && _searchQuery != value)
                        {
                              _searchQuery = value;
                        }
                  }
            }

            internal bool CaseSensitiveSearch
            {
                  get => _caseSensitiveSearch;
                  set
                  {
                        if (_caseSensitiveSearch == value)
                        {
                              return;
                        }

                        _caseSensitiveSearch = value;

                        if (!string.IsNullOrEmpty(_searchQuery))
                        {
                              _lastProcessedSearchQuery = "";
                        }
                  }
            }

            internal int GoToLine
            {
                  get => _goToLine;
                  set => _goToLine = Math.Max(1, value);
            }

            internal int CurrentSearchIndex => _currentSearchIndex;
            internal int SearchResultsCount => _searchResults.Count;
            internal HashSet<int> SearchResults => _searchResults;
            internal bool HasSearchResults => _searchResults.Count > 0;
            internal bool HasSearchQuery => !string.IsNullOrEmpty(_searchQuery);

            internal bool HasSearchQueryChanged()
            {
                  return _searchQuery != _lastProcessedSearchQuery;
            }

            internal string GetSearchQuery() => _searchQuery;

            internal HashSet<int> GetSearchResults() => _searchResults;

            internal void PerformSearch(string[] lines)
            {
                  if (lines == null)
                  {
                        ClearSearch();

                        return;
                  }

                  _searchResults.Clear();
                  _currentSearchIndex = -1;

                  if (string.IsNullOrEmpty(_searchQuery))
                  {
                        _lastProcessedSearchQuery = _searchQuery;
                        OnSearchResultsChanged?.Invoke();

                        return;
                  }

                  StringComparison comparison = _caseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                  for (int i = 0; i < lines.Length; i++)
                  {
                        if (lines[i] != null && lines[i].IndexOf(_searchQuery, comparison) >= 0)
                        {
                              _searchResults.Add(i);
                        }
                  }

                  if (_searchResults.Count > 0)
                  {
                        _currentSearchIndex = 0;
                  }

                  _lastProcessedSearchQuery = _searchQuery;
                  OnSearchResultsChanged?.Invoke();
            }

            internal void ClearSearch()
            {
                  _searchQuery = "";
                  _lastProcessedSearchQuery = "";
                  _searchResults.Clear();
                  _currentSearchIndex = -1;
                  OnSearchResultsChanged?.Invoke();
            }

            internal int GetCurrentResultLine()
            {
                  if (_currentSearchIndex >= 0 && _currentSearchIndex < _searchResults.Count)
                  {
                        return _searchResults.ToArray()[_currentSearchIndex];
                  }

                  return -1;
            }

            internal bool GoToNextResult()
            {
                  if (_searchResults.Count == 0)
                  {
                        return false;
                  }

                  _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
                  OnSearchResultsChanged?.Invoke();

                  return true;
            }

            internal bool GoToPreviousResult()
            {
                  if (_searchResults.Count == 0)
                  {
                        return false;
                  }

                  _currentSearchIndex = (_currentSearchIndex - 1 + _searchResults.Count) % _searchResults.Count;
                  OnSearchResultsChanged?.Invoke();

                  return true;
            }

            internal string GetSearchStatusText()
            {
                  if (!HasSearchQuery)
                  {
                        return "";
                  }

                  if (!HasSearchResults)
                  {
                        return "(0/0)";
                  }

                  return $"({_currentSearchIndex + 1}/{_searchResults.Count})";
            }

            internal bool IsCurrentResult(int lineIndex)
            {
                  return _currentSearchIndex >= 0 && _currentSearchIndex < _searchResults.Count && _searchResults.ToArray()[_currentSearchIndex] == lineIndex;
            }

            internal int GetGoToLineZeroBased() => Math.Max(0, _goToLine - 1);

            internal void SetGoToLine(int lineNumber) => _goToLine = Math.Max(1, lineNumber);
      }
}