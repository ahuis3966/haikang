using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace anfang
{
    public class MySQLHelper
    {
        private string connectionString;

        public MySQLHelper(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // 执行不返回结果集的SQL语句
        public int ExecuteNonQuery(string sql, params MySqlParameter[] parameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    // 添加参数
                    command.Parameters.AddRange(parameters);
                    // 打开连接
                    connection.Open();
                    // 执行SQL语句并返回影响行数
                    return command.ExecuteNonQuery();
                }
            }
        }

        // 执行一个查询，并返回结果集中第一行的第一列
        public object ExecuteScalar(string sql, params MySqlParameter[] parameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    // 添加参数
                    command.Parameters.AddRange(parameters);
                    // 打开连接
                    connection.Open();
                    // 执行SQL查询并返回第一行第一列的值
                    return command.ExecuteScalar();
                }
            }
        }

        // 执行一个查询，并返回结果集
        public DataTable ExecuteQuery(string sql, params MySqlParameter[] parameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    // 添加参数
                    command.Parameters.AddRange(parameters);
                    // 打开连接
                    connection.Open();
                    // 创建DataAdapter和DataTable对象，并填充数据
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
        }

        // 执行一个查询，并将结果集映射到一个对象列表
        public List<T> ExecuteQuery<T>(string sql, Func<IDataRecord, T> selector, params MySqlParameter[] parameters)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    // 添加参数
                    command.Parameters.AddRange(parameters);
                    // 打开连接
                    connection.Open();
                    // 创建DataReader对象并读取数据，将每行数据映射到对象并添加到列表中
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        List<T> list = new List<T>();
                        while (reader.Read())
                        {
                            list.Add(selector(reader));
                        }
                        return list;
                    }
                }
            }
        }

        // 向数据库中插入数据
        public int Insert(string tableName, Dictionary<string, object> data)
        {
            string[] columns = new string[data.Count];
            object[] values = new object[data.Count];

            int i = 0;
            foreach (KeyValuePair<string, object> item in data)
            {
                // 获取列名和值
                columns[i] = item.Key;
                values[i] = item.Value;
                i++;
            }

            string sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", tableName, string.Join(",", columns), "@" + string.Join(",@", columns));

            // 将Dictionary转换为MySqlParameter数组，并执行SQL语句
            return ExecuteNonQuery(sql, ToMySqlParameters(data));
        }

        // 更新数据库中的数据
        public int Update(string tableName, Dictionary<string, object> data, string whereClause = "")
        {
            string[] setValues = new string[data.Count];
            int i = 0;
            foreach (KeyValuePair<string, object> item in data)
            {
                // 获取列名和值
                setValues[i] = string.Format("{0}=@{0}", item.Key);
                i++;
            }

            string sql = string.Format("UPDATE {0} SET {1}", tableName, string.Join(",", setValues));

            if (!string.IsNullOrEmpty(whereClause))
            {
                sql += " WHERE " + whereClause;
            }

            // 将Dictionary转换为MySqlParameter数组，并执行SQL语句
            return ExecuteNonQuery(sql, ToMySqlParameters(data));
        }

        // 删除数据库中的数据
        public int Delete(string tableName, string whereClause = "")
        {
            string sql = string.Format("DELETE FROM {0}", tableName);

            if (!string.IsNullOrEmpty(whereClause))
            {
                sql += " WHERE " + whereClause;
            }

            // 执行SQL语句并返回影响

            return ExecuteNonQuery(sql);
        }
        // 将Dictionary转换为MySqlParameter数组
        private MySqlParameter[] ToMySqlParameters(Dictionary<string, object> data)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            foreach (KeyValuePair<string, object> item in data)
            {
                parameters.Add(new MySqlParameter("@" + item.Key, item.Value));
            }

            return parameters.ToArray();
        }

        // 是否存在数据
        public bool IsExist(string sql)
        {
            bool result = false;
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    object queryResult = command.ExecuteScalar();

                    if (queryResult != null && queryResult != DBNull.Value)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        /*
// 查询所有数据
DataTable dataTable = mySQLHelper.ExecuteQuery("SELECT * FROM myTable");
foreach (DataRow row in dataTable.Rows)
{
    Console.WriteLine(row["column1"].ToString());
}
 
 
// 查询单个值
object value = mySQLHelper.ExecuteScalar("SELECT COUNT(*) FROM myTable");
Console.WriteLine(value.ToString());
 
 
// 查询并映射到对象列表
List<MyClass> list = mySQLHelper.ExecuteQuery("SELECT * FROM myTable", r => new MyClass
{
    Column1 = r["column1"].ToString(),
    Column2 = int.Parse(r["column2"].ToString())
});
 
 
// 插入数据
Dictionary<string, object> data = new Dictionary<string, object>();
data.Add("column1", "value1");
data.Add("column2", 123);
int result = mySQLHelper.Insert("myTable", data);
 
 
// 更新数据
Dictionary<string, object> data = new Dictionary<string, object>();
data.Add("column1", "value2");
data.Add("column2", 456);
int result = mySQLHelper.Update("myTable", data, "id=1");
 
 
// 删除数据
int result = mySQLHelper.Delete("myTable", "id=1"); 
        


        */

    }
}
