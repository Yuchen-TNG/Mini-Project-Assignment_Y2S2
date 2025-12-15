using Google.Cloud.Firestore;
using System;

namespace Mini_Project_Assignment_Y2S2.Models
{
    [FirestoreData]

    // can be changed later if needed, this is just a basic structure for now (for admin post management to run)
    public class Post
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Title { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public string PostType { get; set; } // "LOSTITEM" or "FOUNDITEM"

        [FirestoreProperty]
        public string ImageUrl { get; set; }

        [FirestoreProperty]
        public string UserId { get; set; }

        [FirestoreProperty]
        public string UserName { get; set; }

        [FirestoreProperty]
        public DateTime DatePosted { get; set; }

        [FirestoreProperty]
        public string Status { get; set; } // "Pending Approval", "Approved", "Rejected"

        [FirestoreProperty]
        public DateTime? DateVerified { get; set; }

        [FirestoreProperty]
        public string VerifiedByAdminId { get; set; }
    }
}
