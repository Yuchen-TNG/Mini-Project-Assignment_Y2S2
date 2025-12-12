namespace Mini_Project_Assignment_Y2S2.Models
{
    public class CardDetailsViewModel
    {
        public Item Item { get; set; }   // Firestore 里的物品信息
        public User User { get; set; }   // 当前登录用户信息
    }
}
