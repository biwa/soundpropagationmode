
#region ================== Copyright (c) 2014 Boris Iwanski

/*
 * Copyright (c) 2014 Boris Iwanski
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Linq;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.BuilderModes;
using CodeImp.DoomBuilder.GZBuilder.Tools;
using CodeImp.DoomBuilder.GZBuilder.Geometry;

#endregion

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	//
	// MANDATORY: The plug!
	// This is an important class to the Doom Builder core. Every plugin must
	// have exactly 1 class that inherits from Plug. When the plugin is loaded,
	// this class is instantiated and used to receive events from the core.
	// Make sure the class is public, because only public classes can be seen
	// by the core.
	//

	public class BuilderPlug : Plug
	{
		#region ================== Variables

		private bool additiveselect;
		private bool autoclearselection;
		private MenusForm menusform;
		private bool usehighlight;
		private bool viewselectionnumbers;
		private float stitchrange;

		// Colors
		private PixelColor highlightcolor;
		private PixelColor level1color;
		private PixelColor level2color;
		private PixelColor blocksoundcolor;
		private PixelColor nosoundcolor;

		#endregion

		#region ================== Properties

		public bool AdditiveSelect { get { return additiveselect; } }
		public bool AutoClearSelection { get { return autoclearselection; } }
		public MenusForm MenusForm { get { return menusform; } }
		public bool ViewSelectionNumbers { get { return viewselectionnumbers; } set { viewselectionnumbers = value; } }
		public bool UseHighlight
		{
			get
			{
				return usehighlight;
			}
			set
			{
				usehighlight = value;
				General.Map.Renderer3D.ShowSelection = usehighlight;
				General.Map.Renderer3D.ShowHighlight = usehighlight;
			}
		}

		public float StitchRange { get { return stitchrange; } }

		// Colors
		public PixelColor HighlightColor { get { return highlightcolor; } set { highlightcolor = value; } }
		public PixelColor Level1Color { get { return level1color; } set { level1color = value; } }
		public PixelColor Level2Color { get { return level2color; } set { level2color = value; } }
		public PixelColor BlockSoundColor { get { return blocksoundcolor; } set { blocksoundcolor = value; } }
		public PixelColor NoSoundColor { get { return nosoundcolor; } set { nosoundcolor = value; } }

		#endregion

 		// Static instance. We can't use a real static class, because BuilderPlug must
		// be instantiated by the core, so we keep a static reference. (this technique
		// should be familiar to object-oriented programmers)
		private static BuilderPlug me;

		// Static property to access the BuilderPlug
		public static BuilderPlug Me { get { return me; } }

      	// This plugin relies on some functionality that wasn't there in older versions
		public override int MinimumRevision { get { return 1310; } }

		// This event is called when the plugin is initialized
		public override void OnInitialize()
		{
			base.OnInitialize();

			usehighlight = true;

			highlightcolor = PixelColor.FromInt(General.Settings.ReadPluginSetting("highlightcolor", new PixelColor(255, 0, 192, 0).ToInt()));
			level1color = PixelColor.FromInt(General.Settings.ReadPluginSetting("level1color", new PixelColor(255, 0, 255, 0).ToInt()));
			level2color = PixelColor.FromInt(General.Settings.ReadPluginSetting("level2color", new PixelColor(255, 255, 255, 0).ToInt()));
			nosoundcolor = PixelColor.FromInt(General.Settings.ReadPluginSetting("nosoundcolor", new PixelColor(255, 160, 160, 160).ToInt()));
			blocksoundcolor = PixelColor.FromInt(General.Settings.ReadPluginSetting("blocksoundcolor", new PixelColor(255, 255, 0, 0).ToInt()));

			//controlsectorarea = new ControlSectorArea(-512, 0, 512, 0, -128, -64, 128, 64, 64, 56);

			// This binds the methods in this class that have the BeginAction
			// and EndAction attributes with their actions. Without this, the
			// attributes are useless. Note that in classes derived from EditMode
			// this is not needed, because they are bound automatically when the
			// editing mode is engaged.
            General.Actions.BindMethods(this);

			menusform = new MenusForm();

  			// TODO: Add DB2 version check so that old DB2 versions won't crash
			// General.ErrorLogger.Add(ErrorType.Error, "zomg!");

			// Keep a static reference
            me = this;
		}

		// This is called when the plugin is terminated
		public override void Dispose()
		{
			base.Dispose();

			// This must be called to remove bound methods for actions.
            General.Actions.UnbindMethods(this);
        }


        #region ================== Actions
		
        #endregion

		#region ================== Methods


		#endregion
	}

	public static class Extensions
	{
		public static PixelColor FromInt(this PixelColor color, int colorInt)
		{
			byte a = (byte)((colorInt & (0xff << 24)) >> 24);
			byte r = (byte)((colorInt & (0xff << 16)) >> 16);
			byte g = (byte)((colorInt & (0xff << 8)) >> 8);
			byte b = (byte)(colorInt & 0xff);

			return new PixelColor(a, r, g, b);
		}
	}
}
