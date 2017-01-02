﻿using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.DB;

namespace OnlineAnnounceV2
{
	public static class DB
	{
		private static IDbConnection db;

		public static void Connect()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "OnlineAnnounce.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;

			}

			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("onlineannounce",
				new SqlColumn("userid", MySqlDbType.Int32) { Primary = true, Unique = true, Length = 6 },
				new SqlColumn("greet", MySqlDbType.Text) { Length = 100 },
				new SqlColumn("leaving", MySqlDbType.Text) { Length = 100 }));
		}

		public static void AddAnnouncement(OAInfo info)
		{
			string query = $"INSERT INTO `onlineannounce` (`userid`, `greet`, `leaving`) VALUES ({info.userid}, '{info.greet}', '{info.leave}');";
			int result = db.Query(query);
			if (result != 1)
			{
				TShock.Log.ConsoleError("Error adding entry to database for user: " + info.userid);
			}
		}

		public static void UpdateAnnouncement(OAInfo info)
		{
			string query = $"UPDATE `onlineannounce` SET `greet` = '{info.greet}', `leaving` = '{info.leave}' WHERE `userid` = {info.userid};";
			int result = db.Query(query);
			if (result != 1)
			{
				TShock.Log.ConsoleError("Error updating entry in database for user: " + info.userid);
			}
		}

		public static void DeleteAnnouncement(int userid)
		{
			string query = $"DELETE FROM `onlineannounce` WHERE `userid` = {userid}";
			int result = db.Query(query);
			if (result != 1)
			{
				TShock.Log.ConsoleError("Error deleting entry in database for user: " + userid);
			}
		}

		public static string SetInfo(TSPlayer plr)
		{
			//Using null to signify that it was not in database
			OAInfo newInfo = new OAInfo(plr.User.ID, false, null, null);

			string query = $"SELECT * FROM `onlineannounce` WHERE `userid` = {plr.User.ID};";
			using (var reader = db.QueryReader(query))
			{
				if (reader.Read())
				{
					newInfo.wasInDatabase = true;
					newInfo.greet = reader.Get<string>("greet");
					newInfo.leave = reader.Get<string>("leaving");
				}
			}

			plr.SetData(OAMain.OAString, newInfo);
			return newInfo.greet;
		}
	}
}
