using System;
using System.Reflection;

namespace Word_Sever
{
    /// <summary>
    /// 服务端协议版本检查工具
    /// </summary>
    public static class ProtocolVersionCheck
    {
        public static void CheckAndPrintVersion()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("服务端协议版本检查");
            Console.WriteLine("========================================");

            // 检查 ServerMessage 是否有 JsonDoubleKey 属性
            var serverMsgType = typeof(ServerMessage);
            var jsonDoubleKeyProperty = serverMsgType.GetProperty("JsonDoubleKey");

            if (jsonDoubleKeyProperty != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ ServerMessage 包含 JsonDoubleKey 字段");
                Console.WriteLine($"   字段类型: {jsonDoubleKeyProperty.PropertyType.Name}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ ServerMessage 缺少 JsonDoubleKey 字段！");
                Console.WriteLine("   请重新编译服务端！");
                Console.ResetColor();
            }

            // 检查 ClientMessage 是否有 JsonDoubleKey 属性
            var clientMsgType = typeof(ClientMessage);
            var clientJsonDoubleKeyProperty = clientMsgType.GetProperty("JsonDoubleKey");

            if (clientJsonDoubleKeyProperty != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ ClientMessage 包含 JsonDoubleKey 字段");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ ClientMessage 缺少 JsonDoubleKey 字段！");
                Console.ResetColor();
            }

            // 打印所有字段
            Console.WriteLine("\nServerMessage 所有字段:");
            foreach (var prop in serverMsgType.GetProperties())
            {
                var attrs = prop.GetCustomAttributes(typeof(ProtoBuf.ProtoMemberAttribute), false);
                if (attrs.Length > 0)
                {
                    var attr = (ProtoBuf.ProtoMemberAttribute)attrs[0];
                    Console.WriteLine($"   [{attr.Tag}] {prop.Name} ({prop.PropertyType.Name})");
                }
            }

            Console.WriteLine("========================================");
            Console.WriteLine();

            // 如果缺少字段，延迟退出让用户看到错误
            if (jsonDoubleKeyProperty == null || clientJsonDoubleKeyProperty == null)
            {
                Console.WriteLine("警告：协议版本不正确，可能导致客户端连接失败！");
                Console.WriteLine("按任意键继续...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 测试 Protobuf 序列化
        /// </summary>
        public static void TestProtobufSerialization()
        {
            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("Protobuf 序列化测试");
                Console.WriteLine("========================================");

                // 创建测试消息
                var testPkg = new Pkg
                {
                    Head = new Head
                    {
                        ClientCmd = ClientCMD.GetJson,
                        ServerCmd = ServerCMD.ReturnJson
                    },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            clientName = "Test",
                            companyName = "TestCompany",
                            Id = "TestID",
                            Message = "Test Message",
                            JsonDicKey = "TestUserID",
                            JsonDoubleKey = "block",
                            JsonValue = "{\"test\":\"data\"}"
                        }
                    }
                };

                // 序列化
                using (var ms = new System.IO.MemoryStream())
                {
                    ProtoBuf.Serializer.Serialize(ms, testPkg);
                    byte[] bytes = ms.ToArray();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ 序列化成功，字节长度: {bytes.Length}");
                    Console.WriteLine($"   前20字节: {BitConverter.ToString(bytes, 0, Math.Min(20, bytes.Length))}");
                    Console.ResetColor();

                    // 反序列化
                    ms.Position = 0;
                    var deserialized = ProtoBuf.Serializer.Deserialize<Pkg>(ms);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ 反序列化成功");
                    Console.WriteLine($"   ServerMessage.JsonDoubleKey: {deserialized.Body.serverMessage.JsonDoubleKey}");
                    Console.ResetColor();
                }

                Console.WriteLine("========================================");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Protobuf 序列化测试失败: {ex.Message}");
                Console.WriteLine($"   堆栈: {ex.StackTrace}");
                Console.ResetColor();
                Console.WriteLine("按任意键继续...");
                Console.ReadKey();
            }
        }
    }
}
