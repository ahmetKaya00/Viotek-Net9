using System;

namespace ViotekErp.Models
{
    public class GiderHareket
    {
        // msg_S_0998 (smallint)
        public short MsgS0998 { get; set; }

        // #msg_S_0088 (uniqueidentifier) => gerçek benzersiz kayıt
        public Guid MsgS0088 { get; set; }

        // msg_S_0089 (datetime)
        public DateTime? MsgS0089 { get; set; }

        public string? MsgS0223 { get; set; }
        public string? MsgS0137 { get; set; }
        public string? MsgS1158 { get; set; }
        public string? MsgS0203 { get; set; }
        public string? MsgS0433 { get; set; }

        public string? MsgS1167 { get; set; }
        public string? MsgS1168 { get; set; }

        public string? MsgS0471 { get; set; }
        public string? MsgS0472 { get; set; }
        public string? MsgS0473 { get; set; }

        public string? MsgS1162 { get; set; }
        public string? MsgS1035 { get; set; }

        public string? MsgS1163 { get; set; } // nvarchar (ör: "194,6")
        public double? MsgS1164 { get; set; } // float
        public string? MsgS1160 { get; set; } // nvarchar
        public double? MsgS1165 { get; set; } // float
        public double? MsgS1166 { get; set; } // float
    }
}