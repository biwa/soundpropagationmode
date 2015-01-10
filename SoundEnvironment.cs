using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	public class SoundEnvironment
	{
		#region ================== Variables

		private List<Sector> sectors;
		private List<Thing> things;
		private List<Linedef> linedefs;
		private PixelColor color;
		private int id;

		#endregion

		#region ================== Properties

		public List<Sector> Sectors { get { return sectors; } set { sectors = value; } }
		public List<Thing> Things { get { return things; } set { things = value; } }
		public List<Linedef> Linedefs { get { return linedefs; } set { linedefs = value; } }
		public PixelColor Color { get { return color; } set { color = value; } }
		public int ID { get { return id; } set { id = value; } }

		#endregion

		public SoundEnvironment()
		{
			sectors = new List<Sector>();
			things = new List<Thing>();
			linedefs = new List<Linedef>();
			color = General.Colors.Background;
			this.id = -1;
		}
	}
}
