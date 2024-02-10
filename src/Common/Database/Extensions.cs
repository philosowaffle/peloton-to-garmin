using JsonFlatFileDataStore;
using System.Collections.Generic;

namespace Common.Database;

public static class Extensions
{
	public static bool TryGetItem<T>(this DataStore db, int id, out T item) where T : class
	{
		item = null;
		try
		{
			item = db.GetItem<T>(id.ToString());
			return true;

		}
		catch (KeyNotFoundException)
		{
			return false;
		}
	}
}
