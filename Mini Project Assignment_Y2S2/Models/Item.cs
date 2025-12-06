using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mini_Project_Assignment_Y2S2.Models
{
    [FirestoreData]
    public class Item
    {
        [FirestoreProperty]
        public string IName { get; set; }

        [FirestoreProperty]
        public string IType { get; set; }

        [FirestoreProperty]
        public string Idescription { get; set; }

        [FirestoreProperty]
        public string LocationID { get; set; }

        [FirestoreProperty]
        public string Image { get; set; }

        [FirestoreProperty]
        public string Category { get; set; }

        [FirestoreProperty]
        public DateTime Date { get; set; }

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; }


        public IFormFile ImageFile { get; set; }
    }
}