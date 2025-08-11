// EconomyItemData.cs - YENİ SCRİPT

using Firebase.Firestore;

[FirestoreData]
public class EconomyItemData
{
    [FirestoreProperty] public int Supply { get; set; } // Arz: Gezegende ne kadar var?
    [FirestoreProperty] public int Demand { get; set; } // Talep: Gezegen ne kadar istiyor?

    // Firestore için boş yapıcı metot
    public EconomyItemData() { }
}