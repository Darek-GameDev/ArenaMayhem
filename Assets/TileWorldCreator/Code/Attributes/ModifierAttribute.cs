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
using System;

namespace GiantGrey.TileWorldCreator.Attributes
{
  [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false)]
  public class ModifierAttribute : Attribute
  {
      public enum Category
      {
        Generators,
        Modifiers
      }
      
      public Category category;
      public string name;
      public string iconPath;

      public ModifierAttribute(Category _category, string _name, string _iconPath)
      {
          category = _category;
          name = _name;
          iconPath = _iconPath;
      }
  }
}