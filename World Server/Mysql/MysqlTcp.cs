using IOCP;
using Message;
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
public class MysqlTcp : IOCPServer<MysqlPCK>
{
    public const int SeedLenght=1000;
    private List<string>Loginc_tokens=new List<string>();
    private const string connectStr = "server=localhost;uid=root;pwd=zch680625;database=acc";
    public MysqlTcp() : base(1024)
    {
    }
    public override void AcceptClient(IOCPToken<MysqlPCK> client)
    {

    }
    public override void OnCloseAccpet(IOCPToken<MysqlPCK> client)
    {

    }
    public void SelectAccount(IOCPToken<MysqlPCK>client, MySqlConnection connection,string uid,string passworld)
    {
        MysqlPCK mysqlPCK = new MysqlPCK();
        mysqlPCK.Body = new MysqlBody();
        mysqlPCK.Head = new Message.Head();
        using (MySqlCommand command = new MySqlCommand("SELECT PassWord FROM acc_table WHERE UID = @uid", connection))
        {
            command.Parameters.AddWithValue("@uid", uid);
            mysqlPCK.Head.Cmd = Cmd.Mysqllogin;
            try
            {
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    if (reader["PassWord"].Equals(passworld))
                    {
                        logaction.Invoke($"账户认证成功 [{uid}:登录成功]");
                        mysqlPCK.Body.Successful = true;
                        mysqlPCK.Body.Token = RandomToken();
                    }
                    else
                    {
                        mysqlPCK.Body.Successful = false;
                        logaction.Invoke($"账户认证失败 [{uid}:登录失败 密码错误]");
                    }
                }
                else
                {
                    mysqlPCK.Body.Successful = false;
                    logaction.Invoke("账户认证失败 账号错误");
                }
                if (reader != null)
                {
                    reader.Close();
                }
            }
            catch (Exception ex) 
            {
                logaction.Invoke($"数据库查询异常 {ex.Message}");
            }
        }
        client.Send(mysqlPCK);
    }
    public bool SqlConnect(out MySqlConnection sqlConnection)
    {
        try
        {
            sqlConnection = new MySqlConnection(connectStr);
            sqlConnection.Open();
            return true;
        }
        catch
        {
            sqlConnection = null;
            return false;
        }
    }
    public void SqlClose(MySqlConnection sqlConnection)
    {
        sqlConnection.Close();
        sqlConnection.Dispose();
    }
    public bool IsToken(string token)
    {
        if(Loginc_tokens.Contains(token))
        {
            Loginc_tokens.Remove(token);
            return true;
        }
        else
        {
            return false;
        }
    }
    public override void OnReceiveMessage(IOCPToken<MysqlPCK> client, MysqlPCK message)
    {
        switch (message.Head.Cmd)
        {
            case Cmd.Mysqllogin:
                if(SqlConnect(out MySqlConnection connect))
                {
                    logaction.Invoke("数据库连接成功");
                    SelectAccount(client, connect, message.Body.Uid, message.Body.Password);
                    SqlClose(connect);
                }
                else
                {
                    logaction.Invoke("数据库连接失败");
                }
                break;
            case Cmd.MysqlQuit:
                
                break;
        }
    }
    private string RandomToken()
    {
        var random = new Random();
        string token=(random.NextDouble() * SeedLenght).ToString();
        while (Loginc_tokens.Contains(token))
        {
             token = (random.NextDouble() * SeedLenght).ToString();
        }
        Loginc_tokens.Add(token);
        return token;
    }
}
