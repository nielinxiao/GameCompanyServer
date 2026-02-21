// GameCompany Protobuf Messages
// 与Unity客户端完全兼容的消息定义

#pragma warning disable CS1591, CS0612, CS3021, IDE1006

[global::ProtoBuf.ProtoContract(Name = @"PKG")]
public partial class Pkg : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"head", IsRequired = true)]
        public Head Head { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"body", IsRequired = true)]
        public Body Body { get; set; }
    }

    [global::ProtoBuf.ProtoContract()]
    public partial class Head : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"client_cmd", IsRequired = true)]
        public ClientCMD ClientCmd { get; set; } = ClientCMD.Join;

        [global::ProtoBuf.ProtoMember(2, Name = @"server_cmd", IsRequired = true)]
        public ServerCMD ServerCmd { get; set; } = ServerCMD.ServerMessage;
    }

    [global::ProtoBuf.ProtoContract()]
    public partial class Body : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(3, IsRequired = true)]
        public ClientMessage clientMessage { get; set; }

        [global::ProtoBuf.ProtoMember(4, IsRequired = true)]
        public ServerMessage serverMessage { get; set; }
    }

    [global::ProtoBuf.ProtoContract()]
    public partial class ClientMessage : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, IsRequired = true)]
        public string Name { get; set; }

        [global::ProtoBuf.ProtoMember(2, IsRequired = true)]
        public string companyName { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"message")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Message
        {
            get { return __pbn__Message ?? ""; }
            set { __pbn__Message = value; }
        }
        public bool ShouldSerializeMessage() => __pbn__Message != null;
        public void ResetMessage() => __pbn__Message = null;
        private string __pbn__Message;

        [global::ProtoBuf.ProtoMember(4)]
        [global::System.ComponentModel.DefaultValue("")]
        public string Donat
        {
            get { return __pbn__Donat ?? ""; }
            set { __pbn__Donat = value; }
        }
        public bool ShouldSerializeDonat() => __pbn__Donat != null;
        public void ResetDonat() => __pbn__Donat = null;
        private string __pbn__Donat;

        [global::ProtoBuf.ProtoMember(5)]
        public float DonatMoney
        {
            get { return __pbn__DonatMoney.GetValueOrDefault(); }
            set { __pbn__DonatMoney = value; }
        }
        public bool ShouldSerializeDonatMoney() => __pbn__DonatMoney != null;
        public void ResetDonatMoney() => __pbn__DonatMoney = null;
        private float? __pbn__DonatMoney;

        [global::ProtoBuf.ProtoMember(6)]
        [global::System.ComponentModel.DefaultValue("")]
        public string stockID
        {
            get { return __pbn__stockID ?? ""; }
            set { __pbn__stockID = value; }
        }
        public bool ShouldSerializestockID() => __pbn__stockID != null;
        public void ResetstockID() => __pbn__stockID = null;
        private string __pbn__stockID;

        [global::ProtoBuf.ProtoMember(7)]
        public int StockMuch
        {
            get { return __pbn__StockMuch.GetValueOrDefault(); }
            set { __pbn__StockMuch = value; }
        }
        public bool ShouldSerializeStockMuch() => __pbn__StockMuch != null;
        public void ResetStockMuch() => __pbn__StockMuch = null;
        private int? __pbn__StockMuch;

        [global::ProtoBuf.ProtoMember(8, Name = @"id", IsRequired = true)]
        public string Id { get; set; }

        [global::ProtoBuf.ProtoMember(9, IsRequired = true)]
        public string stockCompany { get; set; }

        [global::ProtoBuf.ProtoMember(10)]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonDicKey
        {
            get { return __pbn__JsonDicKey ?? ""; }
            set { __pbn__JsonDicKey = value; }
        }
        public bool ShouldSerializeJsonDicKey() => __pbn__JsonDicKey != null;
        public void ResetJsonDicKey() => __pbn__JsonDicKey = null;
        private string __pbn__JsonDicKey;

        [global::ProtoBuf.ProtoMember(11)]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonValue
        {
            get { return __pbn__JsonValue ?? ""; }
            set { __pbn__JsonValue = value; }
        }
        public bool ShouldSerializeJsonValue() => __pbn__JsonValue != null;
        public void ResetJsonValue() => __pbn__JsonValue = null;
        private string __pbn__JsonValue;

        [global::ProtoBuf.ProtoMember(12)]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonKey
        {
            get { return __pbn__JsonKey ?? ""; }
            set { __pbn__JsonKey = value; }
        }
        public bool ShouldSerializeJsonKey() => __pbn__JsonKey != null;
        public void ResetJsonKey() => __pbn__JsonKey = null;
        private string __pbn__JsonKey;

        [global::ProtoBuf.ProtoMember(13)]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonDoubleKey
        {
            get { return __pbn__JsonDoubleKey ?? ""; }
            set { __pbn__JsonDoubleKey = value; }
        }
        public bool ShouldSerializeJsonDoubleKey() => __pbn__JsonDoubleKey != null;
        public void ResetJsonDoubleKey() => __pbn__JsonDoubleKey = null;
        private string __pbn__JsonDoubleKey;
    }

    [global::ProtoBuf.ProtoContract()]
    public partial class ServerMessage : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, IsRequired = true)]
        public string clientName { get; set; }

        [global::ProtoBuf.ProtoMember(2, IsRequired = true)]
        public string companyName { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"message", IsRequired = true)]
        public string Message { get; set; }

        [global::ProtoBuf.ProtoMember(4)]
        [global::System.ComponentModel.DefaultValue("")]
        public string Donat
        {
            get { return __pbn__Donat ?? ""; }
            set { __pbn__Donat = value; }
        }
        public bool ShouldSerializeDonat() => __pbn__Donat != null;
        public void ResetDonat() => __pbn__Donat = null;
        private string __pbn__Donat;

        [global::ProtoBuf.ProtoMember(5)]
        public float DonatMoney
        {
            get { return __pbn__DonatMoney.GetValueOrDefault(); }
            set { __pbn__DonatMoney = value; }
        }
        public bool ShouldSerializeDonatMoney() => __pbn__DonatMoney != null;
        public void ResetDonatMoney() => __pbn__DonatMoney = null;
        private float? __pbn__DonatMoney;

        [global::ProtoBuf.ProtoMember(6, Name = @"email")]
        public EmailMessage Email { get; set; }

        [global::ProtoBuf.ProtoMember(7)]
        public float StockMoney
        {
            get { return __pbn__StockMoney.GetValueOrDefault(); }
            set { __pbn__StockMoney = value; }
        }
        public bool ShouldSerializeStockMoney() => __pbn__StockMoney != null;
        public void ResetStockMoney() => __pbn__StockMoney = null;
        private float? __pbn__StockMoney;

        [global::ProtoBuf.ProtoMember(8)]
        public bool AllowBuyStock
        {
            get { return __pbn__AllowBuyStock.GetValueOrDefault(); }
            set { __pbn__AllowBuyStock = value; }
        }
        public bool ShouldSerializeAllowBuyStock() => __pbn__AllowBuyStock != null;
        public void ResetAllowBuyStock() => __pbn__AllowBuyStock = null;
        private bool? __pbn__AllowBuyStock;

        [global::ProtoBuf.ProtoMember(9)]
        [global::System.ComponentModel.DefaultValue("")]
        public string jsonStock
        {
            get { return __pbn__jsonStock ?? ""; }
            set { __pbn__jsonStock = value; }
        }
        public bool ShouldSerializejsonStock() => __pbn__jsonStock != null;
        public void ResetjsonStock() => __pbn__jsonStock = null;
        private string __pbn__jsonStock;

        [global::ProtoBuf.ProtoMember(10)]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonDicKey
        {
            get { return __pbn__JsonDicKey ?? ""; }
            set { __pbn__JsonDicKey = value; }
        }
        public bool ShouldSerializeJsonDicKey() => __pbn__JsonDicKey != null;
        public void ResetJsonDicKey() => __pbn__JsonDicKey = null;
        private string __pbn__JsonDicKey;

        [global::ProtoBuf.ProtoMember(11, Name = @"id", IsRequired = true)]
        public string Id { get; set; }

        [global::ProtoBuf.ProtoMember(12)]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonValue
        {
            get { return __pbn__JsonValue ?? ""; }
            set { __pbn__JsonValue = value; }
        }
        public bool ShouldSerializeJsonValue() => __pbn__JsonValue != null;
        public void ResetJsonValue() => __pbn__JsonValue = null;
        private string __pbn__JsonValue;

        [global::ProtoBuf.ProtoMember(13)]
        public bool FirstCreat
        {
            get { return __pbn__FirstCreat.GetValueOrDefault(); }
            set { __pbn__FirstCreat = value; }
        }
        public bool ShouldSerializeFirstCreat() => __pbn__FirstCreat != null;
        public void ResetFirstCreat() => __pbn__FirstCreat = null;
        private bool? __pbn__FirstCreat;

        [global::ProtoBuf.ProtoMember(14)]
        public float GMMoney
        {
            get { return __pbn__GMMoney.GetValueOrDefault(); }
            set { __pbn__GMMoney = value; }
        }
        public bool ShouldSerializeGMMoney() => __pbn__GMMoney != null;
        public void ResetGMMoney() => __pbn__GMMoney = null;
        private float? __pbn__GMMoney;

        [global::ProtoBuf.ProtoMember(15)]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonDoubleKey
        {
            get { return __pbn__JsonDoubleKey ?? ""; }
            set { __pbn__JsonDoubleKey = value; }
        }
        public bool ShouldSerializeJsonDoubleKey() => __pbn__JsonDoubleKey != null;
        public void ResetJsonDoubleKey() => __pbn__JsonDoubleKey = null;
        private string __pbn__JsonDoubleKey;
    }

    [global::ProtoBuf.ProtoContract()]
    public partial class EmailMessage : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"title", IsRequired = true)]
        public string Title { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"description", IsRequired = true)]
        public string Description { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"datetime", IsRequired = true)]
        public string Datetime { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"objectID")]
        public int[] objectIDs { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"number")]
        public int[] Numbers { get; set; }
    }

    [global::ProtoBuf.ProtoContract(Name = @"Client_CMD")]
    public enum ClientCMD
    {
        Join = 1,
        Remove = 2,
        Donat = 3,
        Message = 4,
        [global::ProtoBuf.ProtoEnum(Name = @"Buy_Stock")]
        BuyStock = 5,
        [global::ProtoBuf.ProtoEnum(Name = @"Sell_Stock")]
        SellStock = 6,
        [global::ProtoBuf.ProtoEnum(Name = @"Search_Stock")]
        SearchStock = 7,
        [global::ProtoBuf.ProtoEnum(Name = @"Get_Stock")]
        GetStock = 8,
        GetJson = 9,
        SetJson = 10,
        CheckPlayerCreatByFirst = 11,
    }

    [global::ProtoBuf.ProtoContract(Name = @"Server_CMD")]
    public enum ServerCMD
    {
        [global::ProtoBuf.ProtoEnum(Name = @"Server_Message")]
        ServerMessage = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"Client_Message")]
        ClientMessage = 2,
        [global::ProtoBuf.ProtoEnum(Name = @"Email_Message")]
        EmailMessage = 3,
        [global::ProtoBuf.ProtoEnum(Name = @"Buy_Stock")]
        BuyStock = 4,
        [global::ProtoBuf.ProtoEnum(Name = @"Get_Stock")]
        GetStock = 5,
        [global::ProtoBuf.ProtoEnum(Name = @"Search_Stock")]
        SearchStock = 6,
        ReturnJson = 7,
        CheckPlayerCreatByFirst = 8,

        // GM命令 (9-20)
        [global::ProtoBuf.ProtoEnum(Name = @"GM_AddMoney")]
        GMAddMoney = 9,
        [global::ProtoBuf.ProtoEnum(Name = @"GM_AwardEmail")]
        GMAwardEmail = 10,
        [global::ProtoBuf.ProtoEnum(Name = @"GM_Broadcast")]
        GMBroadcast = 11,
    }

#pragma warning restore CS1591, CS0612, CS3021, IDE1006
