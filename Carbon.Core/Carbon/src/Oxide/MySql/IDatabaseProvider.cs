﻿///
/// Copyright (c) 2022 Carbon Community 
/// All rights reserved
/// 

using System;
using System.Collections.Generic;
using Oxide.Ext.MySql;
using Oxide.Plugins;

namespace Oxide.Core.Database
{
	public interface IDatabaseProvider
	{
		Connection OpenDb(string file, Plugin plugin, bool persistent = false);
		void CloseDb(Connection db);
		Sql NewSql();
		void Query(Sql sql, Connection db, Action<List<Dictionary<string, object>>> callback);
		void ExecuteNonQuery(Sql sql, Connection db, Action<int> callback = null);
		void Insert(Sql sql, Connection db, Action<int> callback = null);
		void Update(Sql sql, Connection db, Action<int> callback = null);
		void Delete(Sql sql, Connection db, Action<int> callback = null);
	}
}
