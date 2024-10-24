using System.ComponentModel.DataAnnotations;

public class FriendModel
{
    [Key]
    public int Id { get; set; }
    // 申請者がここに来るようにする
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public FriendStatusEnum Status { get; set; }
}
