using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IHaveArrived
{
	internal class Common
	{
		// TODO: Consider turning this into a server-side option
		public static readonly int AnnouncementLengthLimit = 150;

		// Our new file path
		public static readonly string AnnouncementsFilePath = Path.Combine(
			Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ),
			"announcements.txt" );

		// The original file path
		public static readonly string MessagesFilePath = Path.Combine(
			Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ),
			"givenameplz",
			"ValheimIHaveArrived",
			"messages.txt" );

		public static List< string > LoadAnnouncements()
		{
			List< string > announcements = LoadAnnouncements( AnnouncementsFilePath );
			return announcements != null && announcements.Count > 0
				? announcements
				: LoadAnnouncements( MessagesFilePath );
		}

		public static List< string > LoadAnnouncements( string announcementsFilePath )
		{
			if( !File.Exists( announcementsFilePath ) )
				return null;

			List< string > announcements = new List< string >();
			foreach( string announcement in File.ReadAllLines( announcementsFilePath ) )
				if( !announcement.IsNullOrWhiteSpace() && announcement.Length <= AnnouncementLengthLimit )
					announcements.Add( announcement );

			return announcements;
		}
	}
}
