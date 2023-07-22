using System;
using System.Data.SQLite;

namespace DDictionary.DAL
{
    /// <summary>
    /// User-defined function "MYUPPER" to change string into upper case with current UI culture.
    /// </summary>
    [SQLiteFunction(Name = "MYUPPER", FuncType = FunctionType.Scalar)]
    internal class MyUpperSQLiteFunction : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            if(args?.Length != 1)
                throw new ArgumentException("The function takes one string parameter.");

            if(args[0] is null)
                return null;

            if(!(args[0] is string))
                throw new InvalidCastException("The function takes one string parameter.");

            return ((string)args[0]).ToUpper();
        }
    }
}
