// SyndicateData.cs - TEK OYUNCULU VERSİYON İÇİN GEÇİCİ YER TUTUCU

using System.Collections.Generic;

[System.Serializable]
public class SyndicateData
{
    // Bu sınıf, SyndicateManager gibi diğer scriptlerin derlenebilmesi için
    // bir yer tutucu olarak görev yapar. Tek oyunculu versiyonda bu sınıfın
    // içi boş olabilir veya temel alanları içerebilir.
    public string SyndicateName { get; set; }
    public string Tag { get; set; }
    public string Description { get; set; }
    public string LeaderID { get; set; }
    public List<string> MemberIDs { get; set; } = new List<string>();
    public int Treasury { get; set; }
    public string EmblemURL { get; set; }
    public int TradeBuffLevel { get; set; } // Hata CS1061 için eklendi
}