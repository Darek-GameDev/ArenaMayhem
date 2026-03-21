/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	TileWorldCreator (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/
using System.Collections.Generic;
using GiantGrey.TileWorldCreator.Attributes;
using GiantGrey.TileWorldCreator.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Select By Rule", "")]
    public class SelectByRule : BlueprintModifier
    {
        [System.Serializable]
        public class TileRule
        {
            public enum CellState { DontCare, Unoccupied, Occupied }
            public List<CellState> pattern = new List<CellState>(new CellState[9]); // 3x3 stored as a list

            public CellState GetCell(int x, int y) => pattern[y * 3 + x];
            public void SetCell(int x, int y, CellState state) => pattern[y * 3 + x] = state;
        }
 
        [HideInInspector]
        public List<TileRule> rules = new List<TileRule>();
        private VisualElement uiRoot;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            HashSet<Vector2> selectedCells = new HashSet<Vector2>();
            foreach (Vector2 cell in _positions)
            {
                foreach (TileRule rule in rules)
                {
                    if (MatchesRule(cell, rule, _positions))
                    {
                        selectedCells.Add(cell);
                        break;
                    }
                }
            }

            return selectedCells;
        }

        private bool MatchesRule(Vector2 cell, TileRule rule, HashSet<Vector2> _positions)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    Vector2 neighborPos = cell + new Vector2(x, y);
                    bool isOccupied = _positions.Contains(neighborPos);
                    
                    // TileRule.CellState expected = rule.pattern[x + 1, y + 1];
                    TileRule.CellState expected = rule.GetCell(x + 1, y + 1);
                    if (expected == TileRule.CellState.DontCare) continue;
                    if ((expected == TileRule.CellState.Occupied && !isOccupied) ||
                        (expected == TileRule.CellState.Unoccupied && isOccupied))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

#if UNITY_EDITOR
        public override VisualElement BuildInspector(Configuration _asset)
        {
            uiRoot = new VisualElement();
            BuildUI();

            return uiRoot;
        }
        void BuildUI()
        {
            uiRoot.Clear();

            var _so = new SerializedObject(this);

            Button addRuleButton = new Button(() => { rules.Add(new TileRule()); BuildUI(); })
            { text = "Add Rule" };

            uiRoot.Add(addRuleButton);
            
            for (int i = 0; i < rules.Count; i++)
            {
                var _index = i;
                var _ruleItem = new VisualElement();
                var _ruleHeader = new VisualElement();
                _ruleHeader.style.flexDirection = FlexDirection.Row;
                _ruleHeader.style.backgroundColor = new Color(60f/255f, 60f/255f, 60f/255f);

                var _ruleContent = new VisualElement();

                var _removeRule = new Button(() => { rules.RemoveAt(_index); BuildUI(); });
                _removeRule.text = "-";

                var _space = new VisualElement();
                _space.style.flexGrow = 1;
                _space.style.flexDirection = FlexDirection.Row;

                _ruleHeader.Add(_space);
                _ruleHeader.Add(_removeRule);

                _ruleItem.Add(_ruleHeader);
                _ruleItem.Add(_ruleContent);
                _ruleItem.SetBorder(1);
            
                for (int y = 2; y >= 0; y--)
                {
                    var _horizontal = new VisualElement();
                    _horizontal.style.flexDirection = FlexDirection.Row;
                    _ruleContent.Add(_horizontal);

                    for (int x = 0; x < 3; x++)
                    {
                    
                        if (x == 1 && y == 1)
                        {
                            var _center = new VisualElement();
                            _center.style.width = 26;
                            _center.style.height = 20;
                            _center.style.borderTopLeftRadius = 10;
                            _center.style.borderTopRightRadius = 10;
                            _center.style.borderBottomLeftRadius = 10;
                            _center.style.borderBottomRightRadius = 10;
                            _center.style.color = Color.black;
                            
                            _horizontal.Add(_center);
                        }
                        else
                        {
                            int cx = x, cy = y;
                            Button cellButton = new Button();
                            cellButton.style.width = 20;
                            cellButton.style.height = 20;
                            cellButton.RegisterCallback<ClickEvent>(e => 
                            {
                                CycleState(cx, cy, rules[_index]);
                                _so.Update();
                                _so.ApplyModifiedProperties();
                                
                                BuildUI();
                            });

                            switch (rules[i].GetCell(x, y))
                            {
                                case TileRule.CellState.Occupied:
                                cellButton.style.color = Color.green;
                                break;
                                case TileRule.CellState.Unoccupied:
                                cellButton.style.color = Color.red;
                                break;
                                case TileRule.CellState.DontCare:
                                cellButton.style.color = Color.white;
                                break;
                            }
                            
                             
                            cellButton.text = GetSymbol (rules[i].GetCell(x, y));
                            _horizontal.Add(cellButton);
                    }
                    }
                }

                uiRoot.Add(_ruleItem);
            }
        }

        private void CycleState(int x, int y, TileRule _rule)
        {
            _rule.SetCell(x, y, (TileRule.CellState)(((int)_rule.GetCell(x, y) + 1) % 3));
            EditorUtility.SetDirty(this);
        }

        private string GetSymbol(TileRule.CellState state)
        {
            return state switch
            {
                TileRule.CellState.Occupied => "O",
                TileRule.CellState.Unoccupied => "X",
                _ => "-"
            };
        }
    #endif
    }
  
}