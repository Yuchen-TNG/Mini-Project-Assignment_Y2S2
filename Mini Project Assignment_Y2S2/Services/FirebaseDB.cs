using Google.Cloud.Firestore;
using System.Threading.Tasks;
using System.IO;
using System;   

namespace Mini_Project_Assignment_Y2S2.Services
{
    public class FirebaseDB
    {
        public FirestoreDb Firestore { get; }

        public FirebaseDB()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "firebase_config.json";

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            Firestore = FirestoreDb.Create("miniproject-d280e");
        }

        // --------------------------
        // GET DATA FROM FIRESTORE
        // --------------------------
        public async Task<T?> GetDataAsync<T>(string documentPath) where T : class
        {
            DocumentReference docRef = Firestore.Document(documentPath);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            return snapshot.ConvertTo<T>();
        }

        // --------------------------
        // UPDATE DATA
        // --------------------------
        public async Task UpdateDataAsync<T>(string documentPath, T data)
        {
            DocumentReference docRef = Firestore.Document(documentPath);
            await docRef.SetAsync(data, SetOptions.MergeAll);
        }
    }
}
