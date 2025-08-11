// SyndicateData.cs - GÜNCELLENDİ (Geliştirme Seviyeleri Eklendi)
using Firebase.Firestore;
using System.Collections.Generic;

[FirestoreData]
public class SyndicateData
{
    [FirestoreProperty] public string SyndicateName { get; set; }
    [FirestoreProperty] public string Tag { get; set; }
    [FirestoreProperty] public string Description { get; set; }
    [FirestoreProperty] public string LeaderID { get; set; }
    [FirestoreProperty] public List<string> MemberIDs { get; set; }
    [FirestoreProperty] public int Treasury { get; set; }

    // --- YENİ EKLENEN ALANLAR ---
    // Bu alan, sendikanın Ticaret Bonusu'nun kaçıncı seviyede olduğunu saklayacak.
    [FirestoreProperty] public int TradeBuffLevel { get; set; }
    
    // --- YENİ EKLENEN ALAN ---
    [FirestoreProperty] public string EmblemURL { get; set; }

    // Gelecekte eklenebilecek diğer buff'lar için buraya yeni alanlar ekleyebiliriz:
    // [FirestoreProperty] public int CombatBuffLevel { get; set; }
    // [FirestoreProperty] public int MiningBuffLevel { get; set; }
    // -------------------------

    // Firestore'un bu sınıfı okuyabilmesi için boş bir yapıcı metot (constructor) gereklidir.
    public SyndicateData() 
    {
        // Yeni bir sendika oluşturulduğunda, tüm buff seviyelerinin varsayılan olarak
        // 0 olduğundan emin oluyoruz. Bu, hataları önler.
        this.TradeBuffLevel = 0;
    }
}