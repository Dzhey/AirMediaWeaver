using System;
using SQLite;

namespace AirMedia.Core.Utils
{
    public static class SQLUtils
    {
        /// <summary>
        /// Create and return new function for specified function, wrapping it into transaction.
        /// Returned function returns result of specified function when called.
        /// Return false to rollback transaction, return true to commit.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Func<bool> CreateTransactionProcedure(this SQLiteConnection connection, Func<SQLiteConnection, bool> func)
        {
            return () =>
                {
                    bool isSuccessful = false;
                    connection.BeginTransaction();
                    try
                    {
                        bool success = func(connection);

                        if (success)
                        {
                            connection.Commit();
                        }

                        isSuccessful = success;
                    }
                    finally
                    {
                        if (isSuccessful == false)
                        {
                            connection.Rollback();
                        }
                    }

                    return isSuccessful;
                };
        }
    }
}
