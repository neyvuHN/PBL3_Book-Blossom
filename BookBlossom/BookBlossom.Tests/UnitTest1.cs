using Xunit;
using Microsoft.Data.SqlClient;
using System;
using System.Text;

namespace BookBlossom.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            string connString = "Server=sql1004.site4now.net;Database=db_ac99f8_pbl3;User Id=db_ac99f8_pbl3_admin;Password=abc1234@;TrustServerCertificate=True;MultipleActiveResultSets=true;";
            var sb = new StringBuilder();
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        COLUMN_NAME, 
                        DATA_TYPE, 
                        IS_NULLABLE, 
                        CHARACTER_MAXIMUM_LENGTH
                    FROM 
                        INFORMATION_SCHEMA.COLUMNS
                    WHERE 
                        TABLE_SCHEMA = 'OrderRequest' AND TABLE_NAME = 'ReturnRequest'
                    ORDER BY 
                        ORDINAL_POSITION
                ", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sb.AppendLine($"Col: {reader["COLUMN_NAME"]} | Type: {reader["DATA_TYPE"]} | Nullable: {reader["IS_NULLABLE"]} | MaxLen: {reader["CHARACTER_MAXIMUM_LENGTH"]}");
                        }
                    }
                }
            }
            throw new Exception("COLUMNS IN ReturnRequest TABLE:\n" + sb.ToString());
        }
    }
}
