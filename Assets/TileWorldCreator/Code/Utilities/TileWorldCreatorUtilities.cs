
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

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GiantGrey.TileWorldCreator.Utilities
{
    public static class TileWorldCreatorUtilities
    {
		private static Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>();

        public static string GetRelativeResPath()
		{
		
			var _res = System.IO.Directory.EnumerateFiles("Assets/", "TileWorldCreator4_ResPath.cs", System.IO.SearchOption.AllDirectories);
				
			var _path = "";
				
			var _found = _res.FirstOrDefault();
			if (!string.IsNullOrEmpty(_found))
			{
				_path = _found.Replace("TileWorldCreator4_ResPath.cs", "").Replace("\\", "/");
			}
			
			return _path;
				
				
		}

        public static Texture2D LoadImage(string _name)
		{
			#if UNITY_EDITOR
			if (_icons.ContainsKey(_name))
			{
				if (_icons[_name] == null)
				{
					var _icon = (Texture2D)(AssetDatabase.LoadAssetAtPath<Texture2D>(GetRelativeResPath() + "/" + _name));
					if (_icon == null)
					{
						byte[] textureData = System.IO.File.ReadAllBytes(GetRelativeResPath() + "/" + _name);
						_icon = new Texture2D(2, 2);
						_icon.LoadImage(textureData);
					
					}
					
					_icons[_name] = _icon;
				}

				return _icons[_name];
			}
			else
			{
				var _icon = (Texture2D)(AssetDatabase.LoadAssetAtPath<Texture2D>(GetRelativeResPath() + "/" + _name));
				if (_icon == null)
				{
					byte[] textureData = System.IO.File.ReadAllBytes(GetRelativeResPath() + "/" + _name);
					_icon = new Texture2D(2, 2);
					_icon.LoadImage(textureData);
				
				}
				_icons.Add(_name, _icon);
				
				return _icon;
			}

			#else
			return null;
			#endif
		}

		public static string GetIconResPath(string _iconName)
		{
			return GetRelativeResPath() + "/" + _iconName;
		}
    }
}

#endif
