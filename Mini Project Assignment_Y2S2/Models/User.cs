using Google.Cloud.Firestore;

namespace Mini_Project_Assignment_Y2S2.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty]
        public string UserID { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string PhoneNumber { get; set; }

        [FirestoreProperty]
        public string PasswordHash { get; set; }

        [FirestoreProperty]
        public string Role { get; set; }
    }

}