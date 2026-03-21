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
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
	[Modifier(ModifierAttribute.Category.Generators, "Height Texture", "")]
    public class HeightTexture : BlueprintModifier
    {
        [SerializeField]
		[HideInInspector]
		public Texture2D originalTexture;
		[SerializeField]
		[HideInInspector]
		public Texture2D modifiedTexture;

		[SerializeField]
		[HideInInspector]
		private Vector2 grayscaleRange;
	
        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
			var _asset = _layer.GetAsset();
			
			if (originalTexture != null)
			{
				modifiedTexture = new Texture2D (_asset.width, _asset.height);

				if (originalTexture.width != _asset.width || originalTexture.height != _asset.height)
				{
					modifiedTexture = Resize(originalTexture, _asset.width, _asset.height);
				}
				else
				{
					modifiedTexture = originalTexture;
				}

				Color[] _pixels = modifiedTexture.GetPixels();

				for (int x = 0; x < _asset.width; x++)
				{
					for (int y = 0; y < _asset.height; y++)
					{
						try
						{
							var _pixel = _pixels[(y * _asset.width + x)].grayscale;
						
							if (_pixel >= grayscaleRange.x && _pixel <= grayscaleRange.y)
							{
								_positions.Add(new Vector2(x, y));
							}

						}
						catch{}
					}
				}
			}

			return _positions;
        }

		public void SetTexture(Texture2D _texture)
		{
			originalTexture = _texture;
		}
		
		public void SetGrayscaleRange(float min, float max)
		{
			grayscaleRange = new Vector2(min, max);
		}

		Texture2D Resize(Texture2D texture2D,int targetX,int targetY)
		{
			RenderTexture rt=new RenderTexture(targetX, targetY,24);
			RenderTexture.active = rt;
			Graphics.Blit(texture2D,rt);
			Texture2D result=new Texture2D(targetX,targetY);
			result.ReadPixels(new Rect(0,0,targetX,targetY),0,0);
			result.Apply();
			return result;
		}

		#if UNITY_EDITOR

        public override VisualElement BuildInspector(Configuration _asset)
        {
            var _root = new VisualElement();

			var _so = new SerializedObject(this);

			var _originalTexture = new PropertyField();
			_originalTexture.label = "Height Texture";
			_originalTexture.BindProperty(_so.FindProperty(nameof(originalTexture)));

			if (originalTexture != null)
			{
				var _preview = new VisualElement();
				_preview.style.backgroundImage = originalTexture;
				_preview.style.width = 100;
				_preview.style.height = 100;
			
				_root.Add(_preview);
			}

			var _grayscaleRange = new MinMaxSlider();
			_grayscaleRange.lowLimit = 0f;
			_grayscaleRange.highLimit = 1f;
			_grayscaleRange.BindProperty(_so.FindProperty(nameof(grayscaleRange)));

			_root.Add(_originalTexture);
			_root.Add(_grayscaleRange);

			return _root;
        }

		#endif

    }
}
