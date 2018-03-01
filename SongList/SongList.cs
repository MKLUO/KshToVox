﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Xml.Linq;

using IfsParse;

namespace SongList
{
	public class SongList
	{
		public readonly static string[] DIFS = { "novice", "advanced", "exhaust", "infinite" };
		public readonly static string cachePath = System.IO.Path.GetDirectoryName(
			System.Reflection.Assembly.GetExecutingAssembly().Location
			) + "\\cache\\";

		public SongList()
		{
			Directory.CreateDirectory(cachePath);
		}

		~SongList()
		{
			Directory.Delete(cachePath, true);
		}

		// From KFC
		public void Load(string kfcPath_)
		{
			songs.Clear();

			kfcPath = kfcPath_;

            // Parse Ifs Files (Charts, Jackets) into Cache
            // Jackets are not parsed for now, will be included in further version
            Ifs vox01 = new Ifs(kfcPath + "\\data\\others\\vox_ifs\\vox_01.ifs", Ifs.IfsParseType.Chart);
            vox01.Cache(cachePath);
            vox01.Close();

            // DB backup?
            string dbPath = kfcPath + "\\data\\others\\music_db.xml";
			if (!File.Exists(dbPath)) throw new FileNotFoundException();
			FileStream stream = new FileStream(dbPath, FileMode.Open);

			XElement root = XElement.Load(stream);
			foreach (XElement songXml in root.Elements("music"))
			{
				// Basic Attributes
				Dictionary<string, string> data = new Dictionary<string, string>();

				int id = int.Parse(songXml.Attribute("id").Value);
				data["label"]	= songXml.Element("info").Element("label").Value;
				data["title"]	= songXml.Element("info").Element("title_name").Value;
				data["artist"]	= songXml.Element("info").Element("artist_name").Value;
				data["ascii"]	= songXml.Element("info").Element("ascii").Value;
				data["version"]	= songXml.Element("info").Element("version").Value;
				data["inf_ver"]	= songXml.Element("info").Element("inf_ver").Value;

				// Difficulties. 0 = dummy.
				Dictionary<string, int> difficulty = new Dictionary<string, int>();

				foreach (string dif in DIFS)
					difficulty[dif]	= int.Parse(songXml.Element("difficulty").Element(dif).Element("difnum").Value);

				Song song = new Song(data, difficulty, kfcPath, false);
				songs[id] = song;
			}

			stream.Close();

			loaded = true;
		}

		public int AddKshSong(string path)
		{
			int newId = 0;
            int listId = 0; // For ListBox Display
            foreach (int id in Enumerable.Range(256, 1024))
            {
                if (songs.ContainsKey(id)) listId++;
                else
                {
                    newId = id;
                    break;
                }
            }
			if (newId == 0) throw new Exception("Song list is full!");

			songs[newId] = new Song(newId.ToString(), path);

			return listId;
		}

		public void DeleteId(int id)
		{
			songs.Remove(id);
		}


		// To KFC
		public void Save()
		{
			string dbPath = kfcPath + "\\data\\others\\music_db.xml";

			XElement root = new XElement("mdb");
			foreach (KeyValuePair<int, Song> songId in songs)
			{
				int id		= songId.Key;
				Song song	= songId.Value;

                // Write .vox and 2dx
                song.Save(kfcPath);

                // Write to db
                XElement music = new XElement("music", new XAttribute("id", id));

				XElement info = new XElement("info");

				info.Add(new XElement("label",				song.Data("label")));
				info.Add(new XElement("title_name",			song.Data("title")));
				info.Add(new XElement("title_yomigana",		""));
				info.Add(new XElement("artist_name",		song.Data("artist")));
				info.Add(new XElement("artist_yomigana",	""));
				info.Add(new XElement("ascii",				song.Data("ascii")));
				info.Add(new XElement("bpm_max",			new XAttribute("__type", "u32"),	99999)); // BPM
				info.Add(new XElement("bpm_min",			new XAttribute("__type", "u32"),	99999)); // BPM
				info.Add(new XElement("distribution_date",	new XAttribute("__type", "u32"),	22222222));
				info.Add(new XElement("volume",				new XAttribute("__type", "u16"),	100)); // Adjust by sox?
				info.Add(new XElement("bg_no",				new XAttribute("__type", "u16"),	0)); // BG here
				info.Add(new XElement("genre",				new XAttribute("__type", "u8"),		32));
				info.Add(new XElement("is_fixed",			new XAttribute("__type", "u8"),		1));
				info.Add(new XElement("version",			new XAttribute("__type", "u8"),		song.Data("version")));
				info.Add(new XElement("demo_pri",			new XAttribute("__type", "s8"),		0));
				info.Add(new XElement("inf_ver",			new XAttribute("__type", "u8"),		song.Data("inf_ver")));

				XElement difficulty = new XElement("difficulty");
				
				foreach (string dif in DIFS)
				{
					XElement difTag = new XElement(dif);
					difTag.Add(new XElement("difnum", new XAttribute("__type", "u8"), song.Difficulty(dif)));
					difTag.Add(new XElement("illustrator", ""));
					difTag.Add(new XElement("effected_by", ""));
					difTag.Add(new XElement("price", new XAttribute("__type", "s32"), 9999));
					difTag.Add(new XElement("limited", new XAttribute("__type", "u8"), 3));

					difficulty.Add(difTag);
				}

				music.Add(info);
				music.Add(difficulty);

				root.Add(music);
			}

			root.Save(dbPath);
		}

		// Utils

		public Song Song(int id) { return songs[id]; }

		public List<KeyValuePair<int, Song>> List()
		{
			List<KeyValuePair<int, Song>> list = new List<KeyValuePair<int, Song>>();
			foreach (int id in Enumerable.Range(1, 1024))
				if (songs.ContainsKey(id))
					list.Add(new KeyValuePair<int, Song>(id, songs[id]));
			return list;
		}

		public bool Loaded() { return loaded; }

		// Datas

		private Dictionary<int, Song> songs = new Dictionary<int, Song>();
		private string kfcPath;
		private bool loaded = false;
	}
}
