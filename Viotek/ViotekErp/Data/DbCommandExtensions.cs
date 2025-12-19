using System.Data.Common;

namespace ViotekErp.Data
{
    public static class DbCommandExtensions
    {
        public static void AddParameter(this DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}